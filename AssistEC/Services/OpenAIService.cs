using OpenAI;
using OpenAI.Chat;
using AssistEC.Models;
using ChatMessage = AssistEC.Models.ChatMessage;

namespace AssistEC.Services;

public class OpenAIService
{
    private readonly OpenAIClient _openAIClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAIService> _logger;

    public OpenAIService(IConfiguration configuration, ILogger<OpenAIService> logger)
    {
        _configuration = configuration;
        _logger = logger;
        
        // Intentar obtener la API key de variable de entorno primero, luego de configuración
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY_UNITY") 
                     ?? _configuration["OpenAI:ApiKey"];
        
        if (string.IsNullOrEmpty(apiKey))
        {
            throw new InvalidOperationException(
                "La API Key de OpenAI no se encontró. " +
                "Configure la variable de entorno OPENAI_API_KEY_UNITY o agregue la clave en appsettings.json");
        }
        
        _openAIClient = new OpenAIClient(apiKey);
    }

    public async Task<string> ProcessQueryWithDocumentsAsync(string userQuery, List<SharePointDocument> documents)
    {
        try
        {
            // Construir el contexto con la información de los documentos
            var context = BuildDocumentContext(documents);
            
            // Crear el prompt del sistema
            var systemPrompt = $@"Eres un asistente de inteligencia artificial especializado en ayudar a los usuarios a encontrar información en documentos de SharePoint.

Contexto de documentos disponibles:
{context}

Instrucciones:
1. Responde únicamente basándote en la información proporcionada en los documentos
2. Si no encuentras información relevante en los documentos, indícalo claramente
3. Incluye referencias a los documentos específicos cuando sea apropiado
4. Responde en español
5. Sé conciso pero informativo
6. Si hay múltiples documentos relevantes, menciona todos los que apliquen";

            var messages = new List<ChatMessage>
            {
                new() { Role = "system", Content = systemPrompt },
                new() { Role = "user", Content = userQuery }
            };

            var chatClient = _openAIClient.GetChatClient("gpt-5-mini");
            
            var chatMessages = new List<OpenAI.Chat.ChatMessage>();
            foreach (var message in messages)
            {
                if (message.Role == "system")
                    chatMessages.Add(OpenAI.Chat.ChatMessage.CreateSystemMessage(message.Content));
                else if (message.Role == "user")
                    chatMessages.Add(OpenAI.Chat.ChatMessage.CreateUserMessage(message.Content));
                else
                    chatMessages.Add(OpenAI.Chat.ChatMessage.CreateAssistantMessage(message.Content));
            }

            var response = await chatClient.CompleteChatAsync(chatMessages);
            
            return response.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error al procesar consulta con OpenAI: {ex.Message}");
            return "Lo siento, hubo un error al procesar tu consulta. Por favor, inténtalo de nuevo.";
        }
    }

    public async Task<string> GenerateSearchQueryAsync(string userInput)
    {
        try
        {
            var systemPrompt = @"Convierte la consulta del usuario en palabras clave optimizadas para búsqueda en SharePoint. 
Extrae los términos más importantes y relevantes. Responde solo con las palabras clave separadas por espacios, sin explicaciones adicionales.";

            var chatClient = _openAIClient.GetChatClient("gpt-5-mini");
            
            var messages = new List<OpenAI.Chat.ChatMessage>
            {
                OpenAI.Chat.ChatMessage.CreateSystemMessage(systemPrompt),
                OpenAI.Chat.ChatMessage.CreateUserMessage(userInput)
            };

            var response = await chatClient.CompleteChatAsync(messages);
            
            return response.Value.Content[0].Text.Trim();
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error al generar consulta de búsqueda: {ex.Message}");
            return userInput; // Fallback to original input
        }
    }

    private string BuildDocumentContext(List<SharePointDocument> documents)
    {
        if (!documents.Any())
        {
            return "No se encontraron documentos relevantes.";
        }

        var contextBuilder = new System.Text.StringBuilder();
        
        foreach (var doc in documents.Take(5)) // Limitar a 5 documentos para no exceder límites de tokens
        {
            contextBuilder.AppendLine($"=== Documento: {doc.Name} ===");
            contextBuilder.AppendLine($"URL: {doc.WebUrl}");
            contextBuilder.AppendLine($"Última modificación: {doc.LastModified:dd/MM/yyyy}");
            contextBuilder.AppendLine($"Autor: {doc.Author}");
            contextBuilder.AppendLine($"Contenido:");
            
            // Limitar el contenido a los primeros 1000 caracteres para evitar exceder límites
            var content = doc.Content.Length > 1000 ? 
                doc.Content.Substring(0, 1000) + "..." : 
                doc.Content;
            
            contextBuilder.AppendLine(content);
            contextBuilder.AppendLine();
        }

        return contextBuilder.ToString();
    }

    public async Task<string> SummarizeDocumentAsync(SharePointDocument document)
    {
        try
        {
            var systemPrompt = @"Proporciona un resumen conciso del siguiente documento. 
Incluye los puntos principales y la información más relevante. Responde en español.";

            var userPrompt = $@"Documento: {document.Name}
Contenido:
{document.Content}";

            var chatClient = _openAIClient.GetChatClient("gpt-5-mini");
            
            var messages = new List<OpenAI.Chat.ChatMessage>
            {
                OpenAI.Chat.ChatMessage.CreateSystemMessage(systemPrompt),
                OpenAI.Chat.ChatMessage.CreateUserMessage(userPrompt)
            };

            var response = await chatClient.CompleteChatAsync(messages);
            
            return response.Value.Content[0].Text;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error al resumir documento: {ex.Message}");
            return $"No se pudo generar un resumen del documento {document.Name}.";
        }
    }
}