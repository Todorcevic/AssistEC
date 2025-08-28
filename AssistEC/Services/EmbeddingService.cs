using AssistEC.Services.Abstractions;
using OpenAI;
using OpenAI.Embeddings;

namespace AssistEC.Services;

public class EmbeddingService : IEmbeddingService
{
    private readonly OpenAIClient _openAIClient;
    private readonly ILogger<EmbeddingService> _logger;
    private const string EmbeddingModel = "text-embedding-3-small";

    public EmbeddingService(IConfiguration configuration, ILogger<EmbeddingService> logger)
    {
        _logger = logger;
        
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY_UNITY") 
                     ?? configuration["OpenAI:ApiKey"];
        
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException(
                "La API Key de OpenAI no se encontró. " +
                "Configure la variable de entorno OPENAI_API_KEY_UNITY o agregue la clave en appsettings.json");
        }
        
        _openAIClient = new OpenAIClient(apiKey);
    }

    public async Task<float[]> GetEmbeddingAsync(string text)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(text))
                return Array.Empty<float>();

            var embeddingClient = _openAIClient.GetEmbeddingClient(EmbeddingModel);
            var response = await embeddingClient.GenerateEmbeddingAsync(text);
            
            return response.Value.ToFloats().ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error al generar embedding: {ex.Message}");
            return Array.Empty<float>();
        }
    }

    public async Task<List<float[]>> GetEmbeddingsAsync(List<string> texts)
    {
        try
        {
            if (!texts.Any())
                return new List<float[]>();

            var embeddingClient = _openAIClient.GetEmbeddingClient(EmbeddingModel);
            var response = await embeddingClient.GenerateEmbeddingsAsync(texts);
            
            return response.Value.Select(e => e.ToFloats().ToArray()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error al generar embeddings: {ex.Message}");
            return new List<float[]>();
        }
    }

    public double CosineSimilarity(float[] embedding1, float[] embedding2)
    {
        if (embedding1.Length != embedding2.Length || embedding1.Length == 0)
            return 0;

        double dotProduct = 0;
        double norm1 = 0;
        double norm2 = 0;

        for (int i = 0; i < embedding1.Length; i++)
        {
            dotProduct += embedding1[i] * embedding2[i];
            norm1 += embedding1[i] * embedding1[i];
            norm2 += embedding2[i] * embedding2[i];
        }

        if (norm1 == 0 || norm2 == 0)
            return 0;

        return dotProduct / (Math.Sqrt(norm1) * Math.Sqrt(norm2));
    }
}