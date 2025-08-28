using AssistEC.Models;
using AssistEC.Services.Abstractions;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using System.Text;
using System.Text.RegularExpressions;

namespace AssistEC.Services;

public class DocumentContextService : IDocumentContextService
{
    private readonly IMemoryCache _cache;
    private readonly IEmbeddingService _embeddingService;
    private readonly DocumentContextSettings _settings;
    private readonly ILogger<DocumentContextService> _logger;

    public DocumentContextService(
        IMemoryCache cache, 
        IEmbeddingService embeddingService,
        IOptions<DocumentContextSettings> settings,
        ILogger<DocumentContextService> logger)
    {
        _cache = cache;
        _embeddingService = embeddingService;
        _settings = settings.Value;
        _logger = logger;
    }

    public async Task<string> GetOptimizedContextAsync(List<SharePointDocument> documents, string userQuery)
    {
        try
        {
            var cacheKey = $"context_{string.Join(",", documents.Select(d => d.Id))}_{userQuery.GetHashCode()}";
            
            if (_cache.TryGetValue(cacheKey, out string? cachedContext) && cachedContext != null)
            {
                return cachedContext;
            }

            // Generar embedding para la consulta del usuario
            var queryEmbedding = await _embeddingService.GetEmbeddingAsync(userQuery);
            if (queryEmbedding.Length == 0)
            {
                // Fallback a método simple si falla el embedding
                _logger.LogWarning("Embeddings no disponibles, usando método de búsqueda simple");
                return BuildSimpleContext(documents, userQuery);
            }

            // Obtener chunks de todos los documentos
            var allChunks = new List<DocumentChunk>();
            foreach (var doc in documents.Take(_settings.MaxDocuments))
            {
                var chunks = await GetOrCreateChunksAsync(doc);
                allChunks.AddRange(chunks);
            }

            // Calcular similaridad semántica para cada chunk
            var rankedChunks = new List<(DocumentChunk chunk, double similarity)>();
            
            foreach (var chunk in allChunks)
            {
                if (chunk.Embedding.Length > 0)
                {
                    var similarity = _embeddingService.CosineSimilarity(queryEmbedding, chunk.Embedding);
                    if (similarity >= _settings.RelevanceThreshold)
                    {
                        rankedChunks.Add((chunk, similarity));
                    }
                }
            }

            // Seleccionar mejores chunks respetando límite de tokens
            var selectedChunks = rankedChunks
                .OrderByDescending(x => x.similarity)
                .TakeWhile(x => EstimateTokens(x.chunk.Content) <= _settings.MaxTokens)
                .Select(x => x.chunk)
                .ToList();

            // Si no se encontraron chunks relevantes con embeddings, usar fallback
            if (!selectedChunks.Any())
            {
                _logger.LogWarning("No se encontraron chunks relevantes con embeddings, usando método simple");
                return BuildSimpleContext(documents, userQuery);
            }

            var context = BuildContextFromChunks(selectedChunks, documents);
            
            _cache.Set(cacheKey, context, TimeSpan.FromMinutes(_settings.CacheExpirationMinutes));
            return context;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error al obtener contexto optimizado: {ex.Message}");
            return BuildSimpleContext(documents, userQuery);
        }
    }

    public async Task<List<DocumentChunk>> CreateSmartChunksAsync(SharePointDocument document)
    {
        try
        {
            var chunks = new List<DocumentChunk>();
            
            if (string.IsNullOrWhiteSpace(document.Content))
                return chunks;

            // Dividir en párrafos
            var paragraphs = document.Content
                .Split(new[] { "\n\n", "\r\n\r\n" }, StringSplitOptions.RemoveEmptyEntries)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .ToList();

            var currentChunk = new StringBuilder();
            var currentSize = 0;

            foreach (var paragraph in paragraphs)
            {
                // Si agregar este párrafo excede el tamaño máximo y ya hay contenido, crear chunk
                if (currentSize + paragraph.Length > _settings.ChunkSize && currentChunk.Length > 0)
                {
                    await CreateChunkWithEmbedding(chunks, document.Id, currentChunk.ToString(), chunks.Count);
                    currentChunk.Clear();
                    currentSize = 0;
                }

                currentChunk.AppendLine(paragraph);
                currentSize += paragraph.Length;
            }

            // Crear chunk final si hay contenido restante
            if (currentChunk.Length > 0)
            {
                await CreateChunkWithEmbedding(chunks, document.Id, currentChunk.ToString(), chunks.Count);
            }

            return chunks;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error al crear chunks para documento {document.Id}: {ex.Message}");
            return new List<DocumentChunk>();
        }
    }

    public async Task<List<DocumentChunk>> GetOrCreateChunksAsync(SharePointDocument document)
    {
        var cacheKey = $"chunks_{document.Id}_{document.LastModified.Ticks}";
        
        if (_cache.TryGetValue(cacheKey, out List<DocumentChunk>? cachedChunks) && cachedChunks != null)
        {
            return cachedChunks;
        }

        var chunks = await CreateSmartChunksAsync(document);
        _cache.Set(cacheKey, chunks, TimeSpan.FromMinutes(_settings.CacheExpirationMinutes));
        
        return chunks;
    }

    public void ClearCache()
    {
        if (_cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0);
        }
    }

    private async Task CreateChunkWithEmbedding(List<DocumentChunk> chunks, string documentId, string content, int index)
    {
        var embedding = await _embeddingService.GetEmbeddingAsync(content);
        
        chunks.Add(new DocumentChunk
        {
            DocumentId = documentId,
            Content = content.Trim(),
            ChunkIndex = index,
            Embedding = embedding
        });
    }

    private string BuildContextFromChunks(List<DocumentChunk> chunks, List<SharePointDocument> documents)
    {
        if (!chunks.Any())
            return "No se encontraron fragmentos de documentos relevantes.";

        var contextBuilder = new StringBuilder();
        
        // Agrupar chunks por documento
        var chunksByDocument = chunks.GroupBy(c => c.DocumentId);
        
        foreach (var docGroup in chunksByDocument)
        {
            var document = documents.FirstOrDefault(d => d.Id == docGroup.Key);
            if (document != null)
            {
                contextBuilder.AppendLine($"=== {document.Name} ===");
                contextBuilder.AppendLine($"URL: {document.WebUrl}");
                contextBuilder.AppendLine($"Última modificación: {document.LastModified:dd/MM/yyyy}");
                contextBuilder.AppendLine($"Autor: {document.Author}");
                contextBuilder.AppendLine("Contenido relevante:");
                
                foreach (var chunk in docGroup.OrderBy(c => c.ChunkIndex))
                {
                    contextBuilder.AppendLine(chunk.Content);
                    contextBuilder.AppendLine();
                }
                
                contextBuilder.AppendLine();
            }
        }

        return contextBuilder.ToString();
    }

    private string BuildSimpleContext(List<SharePointDocument> documents, string userQuery)
    {
        var contextBuilder = new StringBuilder();
        var keywords = ExtractKeywords(userQuery);
        
        if (!documents.Any())
        {
            return "No se encontraron documentos relevantes.";
        }
        
        foreach (var doc in documents.Take(_settings.MaxDocuments))
        {
            contextBuilder.AppendLine($"=== {doc.Name} ===");
            contextBuilder.AppendLine($"URL: {doc.WebUrl}");
            contextBuilder.AppendLine($"Última modificación: {doc.LastModified:dd/MM/yyyy}");
            contextBuilder.AppendLine($"Autor: {doc.Author}");
            contextBuilder.AppendLine("Contenido:");
            
            var relevantContent = ExtractRelevantContent(doc.Content, keywords);
            contextBuilder.AppendLine(relevantContent);
            contextBuilder.AppendLine();
        }
        
        return contextBuilder.ToString();
    }

    private string ExtractRelevantContent(string content, List<string> keywords)
    {
        if (string.IsNullOrWhiteSpace(content))
            return "Sin contenido disponible.";

        var sentences = SplitIntoSentences(content);
        
        var scoredSentences = sentences
            .Select(s => new { 
                Sentence = s, 
                Score = CalculateRelevanceScore(s, keywords) 
            })
            .Where(s => s.Score > 0)
            .OrderByDescending(s => s.Score)
            .Take(_settings.MaxSentencesPerDocument)
            .Select(s => s.Sentence);

        var result = string.Join(" ", scoredSentences);
        
        // Si no hay contenido relevante, tomar los primeros 1000 caracteres
        if (string.IsNullOrWhiteSpace(result))
        {
            result = content.Length > 1000 ? content.Substring(0, 1000) + "..." : content;
        }
        
        return result;
    }

    private List<string> SplitIntoSentences(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new List<string>();

        // Expresión regular para dividir en oraciones
        var sentences = Regex.Split(text, @"(?<=[.!?])\s+")
            .Where(s => !string.IsNullOrWhiteSpace(s) && s.Length > 20)
            .ToList();

        return sentences;
    }

    private double CalculateRelevanceScore(string sentence, List<string> keywords)
    {
        if (!keywords.Any())
            return 0;

        var lowerSentence = sentence.ToLower();
        return keywords.Sum(keyword => 
            lowerSentence.Contains(keyword.ToLower()) ? 1.0 + (keyword.Length * 0.1) : 0);
    }

    private List<string> ExtractKeywords(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<string>();

        // Palabras comunes a filtrar
        var stopWords = new HashSet<string> 
        { 
            "el", "la", "de", "que", "y", "en", "un", "es", "se", "no", "te", "lo", "le", "da", "su", "por", "son", "con", "para", "al", "del", "los", "las", "una", "como", "pero", "sus", "fue", "ser", "todo", "está", "muy", "ya", "o", "cuando", "si", "más", "hasta", "sobre", "también", "me", "mi", "yo", "tú", "él", "ella", "nosotros", "ustedes", "ellos", "ellas"
        };

        return query.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(w => w.ToLower().Trim().Trim(',', '.', '!', '?', ';', ':'))
            .Where(w => w.Length > 2 && !stopWords.Contains(w))
            .Distinct()
            .ToList();
    }

    private int EstimateTokens(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;
            
        // Aproximación: 1 token ≈ 4 caracteres para español
        return (int)Math.Ceiling(text.Length / 4.0);
    }
}