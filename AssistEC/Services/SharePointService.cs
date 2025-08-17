using Microsoft.Graph;
using Microsoft.Graph.Models;
using Azure.Identity;
using AssistEC.Models;

namespace AssistEC.Services;

public class SharePointService : ISharePointService
{
    private readonly GraphServiceClient? _graphServiceClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SharePointService> _logger;
    private readonly bool _isConfigured;

    public SharePointService(IConfiguration configuration, ILogger<SharePointService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        try
        {
            var tenantId = _configuration["SharePoint:TenantId"];
            var clientId = _configuration["SharePoint:ClientId"];
            var clientSecret = _configuration["SharePoint:ClientSecret"];
            var siteUrl = _configuration["SharePoint:SiteUrl"];

            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId) || 
                string.IsNullOrEmpty(clientSecret) || string.IsNullOrEmpty(siteUrl) ||
                tenantId.Contains("YOUR_") || clientId.Contains("YOUR_") || clientSecret.Contains("YOUR_"))
            {
                _logger.LogWarning("SharePoint configuration is incomplete or contains placeholder values. Using fallback behavior.");
                _isConfigured = false;
                return;
            }

            // Configurar credenciales
            var options = new ClientSecretCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud,
            };

            var clientSecretCredential = new ClientSecretCredential(tenantId, clientId, clientSecret, options);
            
            // Crear cliente de Graph
            _graphServiceClient = new GraphServiceClient(clientSecretCredential);
            _isConfigured = true;
            
            _logger.LogInformation("SharePoint service initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error initializing SharePoint service");
            _isConfigured = false;
        }
    }

    public async Task<List<SharePointDocument>> SearchDocumentsAsync(string query)
    {
        if (!_isConfigured || _graphServiceClient == null)
        {
            _logger.LogWarning("SharePoint service not properly configured. Please configure your SharePoint credentials in appsettings.json");
            return CreateFallbackSearchResults(query);
        }

        try
        {
            _logger.LogInformation($"Searching SharePoint for: {query}");

            // Intentar búsqueda básica usando Microsoft Graph
            // Nota: Esta implementación requiere permisos específicos en Azure AD
            var me = await _graphServiceClient.Me.GetAsync();
            
            if (me != null)
            {
                _logger.LogInformation($"Successfully connected to Microsoft Graph as: {me.DisplayName}");
                
                // En una implementación completa, aquí harías la búsqueda real
                // Por ahora, simulamos algunos resultados basados en la conexión exitosa
                return CreateSampleSearchResults(query, me.DisplayName ?? "Unknown User");
            }

            return new List<SharePointDocument>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error searching SharePoint for query: {query}");
            _logger.LogError("This might be due to insufficient permissions or configuration issues.");
            return CreateFallbackSearchResults(query);
        }
    }

    public async Task<List<SharePointDocument>> GetRecentDocumentsAsync(int count = 10)
    {
        if (!_isConfigured || _graphServiceClient == null)
        {
            _logger.LogWarning("SharePoint service not properly configured. Please configure your SharePoint credentials in appsettings.json");
            return CreateFallbackRecentDocuments(count);
        }

        try
        {
            _logger.LogInformation($"Getting {count} recent documents from SharePoint");

            // Intentar conectarse para validar configuración
            var me = await _graphServiceClient.Me.GetAsync();
            
            if (me != null)
            {
                _logger.LogInformation($"Successfully connected to Microsoft Graph as: {me.DisplayName}");
                return CreateSampleRecentDocuments(count, me.DisplayName ?? "Unknown User");
            }

            return new List<SharePointDocument>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting recent documents from SharePoint");
            return CreateFallbackRecentDocuments(count);
        }
    }

    private List<SharePointDocument> CreateFallbackSearchResults(string query)
    {
        return new List<SharePointDocument>
        {
            new SharePointDocument
            {
                Id = "fallback-1",
                Name = $"Resultados de búsqueda para '{query}'",
                WebUrl = "https://sharepoint-not-configured.example.com",
                Content = $@"⚠️ SharePoint no está configurado correctamente.

Para usar la búsqueda real en SharePoint, necesitas:

1. Configurar las credenciales en appsettings.json:
   - TenantId: ID de tu tenant de Azure AD
   - ClientId: ID de la aplicación registrada en Azure AD
   - ClientSecret: Secret de la aplicación
   - SiteUrl: URL de tu sitio de SharePoint

2. Registrar una aplicación en Azure AD con permisos:
   - Sites.Read.All
   - Files.Read.All
   - User.Read

3. Cambiar UseMockService a false en la configuración

Por ahora, estás usando el servicio simulado que funciona sin configuración.",
                LastModified = DateTime.Now,
                Author = "Sistema"
            }
        };
    }

    private List<SharePointDocument> CreateFallbackRecentDocuments(int count)
    {
        return new List<SharePointDocument>
        {
            new SharePointDocument
            {
                Id = "fallback-recent-1",
                Name = "Configuración de SharePoint requerida",
                WebUrl = "https://sharepoint-not-configured.example.com",
                Content = @"📋 Para acceder a documentos reales de SharePoint:

1. Obtén las credenciales de tu administrador de Azure AD
2. Actualiza appsettings.json con los valores reales
3. Cambia 'UseMockService' a false
4. Reinicia la aplicación

Mientras tanto, puedes usar el servicio simulado que tiene documentos de ejemplo.",
                LastModified = DateTime.Now,
                Author = "Sistema"
            }
        };
    }

    private List<SharePointDocument> CreateSampleSearchResults(string query, string userName)
    {
        return new List<SharePointDocument>
        {
            new SharePointDocument
            {
                Id = "real-search-1",
                Name = $"Resultado de SharePoint para '{query}'",
                WebUrl = "https://graph.microsoft.com/sharepoint",
                Content = $@"✅ ¡Conectado exitosamente a Microsoft Graph!

Usuario autenticado: {userName}
Consulta de búsqueda: {query}

Nota: Esta es una implementación básica que confirma la conectividad.
Para implementar búsqueda completa necesitarías:
- Permisos adicionales en Azure AD
- Implementación de Microsoft Search API
- Manejo de diferentes tipos de archivos",
                LastModified = DateTime.Now,
                Author = userName
            }
        };
    }

    private List<SharePointDocument> CreateSampleRecentDocuments(int count, string userName)
    {
        return new List<SharePointDocument>
        {
            new SharePointDocument
            {
                Id = "real-recent-1",
                Name = "Documento de SharePoint real",
                WebUrl = "https://graph.microsoft.com/sharepoint",
                Content = $@"✅ Conexión exitosa a SharePoint

Usuario: {userName}
Documentos solicitados: {count}

Esta implementación básica confirma que:
- Las credenciales son válidas
- La aplicación tiene permisos básicos
- Puede autenticarse con Microsoft Graph

Para acceso completo a documentos necesitarías implementar:
- Navegación de sitios específicos
- Lectura de bibliotecas de documentos
- Descarga y procesamiento de contenido",
                LastModified = DateTime.Now,
                Author = userName
            }
        };
    }
}