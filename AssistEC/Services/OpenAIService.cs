using OpenAI;
using AssistEC.Models;
using ChatMessage = AssistEC.Models.ChatMessage;
using AssistEC.Services.Abstractions;

namespace AssistEC.Services;

public class OpenAIService
{
    private readonly OpenAIClient _openAIClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAIService> _logger;
    private readonly IDocumentContextService _documentContextService;
    private const string AIModel = "gpt-5-nano";

    public OpenAIService(IConfiguration configuration, ILogger<OpenAIService> logger, IDocumentContextService documentContextService)
    {
        _configuration = configuration;
        _logger = logger;
        _documentContextService = documentContextService;

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

    public async Task<string> ProcessQueryWithDocumentsAsync(string userQuery, List<SharePointDocument> documents, List<ChatMessage>? conversationHistory = null)
    {
        try
        {
            // Generar consulta expandida considerando el historial de conversación
            var expandedQuery = await GenerateExpandedQueryAsync(userQuery, conversationHistory);
            
            // Usar el nuevo servicio de contexto optimizado con la consulta expandida
            var context = await _documentContextService.GetOptimizedContextAsync(documents, expandedQuery);
            
            // Crear el prompt del sistema
            var systemPrompt = $@"Eres un asistente de inteligencia artificial especializado en ayudar a los usuarios a encontrar información en documentos de SharePoint.

Contexto de documentos disponibles:
{context}

Instrucciones:
1. Mantén el contexto de la conversación - recuerda lo que hemos discutido anteriormente
2. Responde basándote principalmente en la información proporcionada en los documentos
3. Si no encuentras información relevante en los documentos, indícalo claramente
4. Incluye referencias a los documentos específicos cuando sea apropiado
5. Responde en español de manera conversacional y natural
6. Sé conciso pero informativo
7. Si hay múltiples documentos relevantes, menciona todos los que apliquen
8. Si te preguntan sobre líneas de un documento, cuenta las líneas del contenido mostrado
9. Para análisis cuantitativos (como contar líneas, palabras, etc.), realiza el cálculo basándote en el contenido disponible
10. Si el usuario hace preguntas de seguimiento, conecta con el contexto previo de la conversación";

            var messages = new List<ChatMessage>
            {
                new() { Role = "system", Content = systemPrompt }
            };

            // Añadir historial de conversación si existe (últimos 10 mensajes para no exceder límites)
            if (conversationHistory?.Any() == true)
            {
                var recentHistory = conversationHistory.TakeLast(10).ToList();
                messages.AddRange(recentHistory);
            }

            // Añadir el mensaje actual del usuario
            messages.Add(new() { Role = "user", Content = userQuery });

            var chatClient = _openAIClient.GetChatClient(AIModel);
            
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

    public async Task<string> GenerateExpandedQueryAsync(string userQuery, List<ChatMessage>? conversationHistory = null)
    {
        try
        {
            // Si no hay historial, usar la consulta original
            if (conversationHistory?.Any() != true)
            {
                return userQuery;
            }

            // Construir contexto de conversación reciente (últimos 6 mensajes)
            var recentHistory = conversationHistory.TakeLast(6).ToList();
            var conversationContext = string.Join("\n", recentHistory.Select(m => 
                $"{(m.Role == "user" ? "Usuario" : "Asistente")}: {m.Content}"));

            var systemPrompt = @"Basándote en el historial de conversación y la pregunta actual del usuario, genera una consulta de búsqueda expandida que capture el contexto completo de lo que el usuario está buscando.

Instrucciones:
1. Mantén todas las referencias a documentos específicos mencionados previamente
2. Incluye conceptos clave de la conversación anterior
3. Amplía la consulta actual con contexto relevante del historial
4. Responde solo con la consulta expandida, sin explicaciones adicionales
5. Máximo 200 caracteres";

            var userPrompt = $@"Historial de conversación:
{conversationContext}

Pregunta actual del usuario: {userQuery}

Genera una consulta de búsqueda expandida:";

            var chatClient = _openAIClient.GetChatClient(AIModel);
            
            var messages = new List<OpenAI.Chat.ChatMessage>
            {
                OpenAI.Chat.ChatMessage.CreateSystemMessage(systemPrompt),
                OpenAI.Chat.ChatMessage.CreateUserMessage(userPrompt)
            };

            var response = await chatClient.CompleteChatAsync(messages);
            var expandedQuery = response.Value.Content[0].Text.Trim();
            
            _logger.LogInformation($"Consulta original: {userQuery}");
            _logger.LogInformation($"Consulta expandida: {expandedQuery}");
            
            return expandedQuery;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error al generar consulta expandida: {ex.Message}");
            return userQuery; // Fallback a consulta original
        }
    }

    public async Task<string> GenerateSearchQueryAsync(string userInput)
    {
        try
        {
            var systemPrompt = @"Convierte la consulta del usuario en palabras clave optimizadas para búsqueda en SharePoint. 
Extrae los términos más importantes y relevantes. Responde solo con las palabras clave separadas por espacios, sin explicaciones adicionales.";

            var chatClient = _openAIClient.GetChatClient(AIModel);
            
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


    public async Task<string> SummarizeDocumentAsync(SharePointDocument document)
    {
        try
        {
            var systemPrompt = @"Proporciona un resumen conciso del siguiente documento. 
Incluye los puntos principales y la información más relevante. Responde en español.";

            var userPrompt = $@"Documento: {document.Name}
Contenido:
{document.Content}";

            var chatClient = _openAIClient.GetChatClient(AIModel);
            
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