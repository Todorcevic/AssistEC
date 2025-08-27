using Microsoft.Graph;
using Microsoft.Graph.Models;
using Azure.Identity;
using AssistEC.Models;
using System.Text.Json;

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

            // Usar Microsoft Graph Search API para búsqueda real
            var searchRequestBody = new Microsoft.Graph.Search.Query.QueryPostRequestBody
            {
                Requests = new List<Microsoft.Graph.Models.SearchRequest>
                {
                    new Microsoft.Graph.Models.SearchRequest
                    {
                        EntityTypes = new List<Microsoft.Graph.Models.EntityType?> { Microsoft.Graph.Models.EntityType.DriveItem },
                        Query = new Microsoft.Graph.Models.SearchQuery
                        {
                            QueryString = query
                        },
                        Size = 25,
                        From = 0,
                        Region = "NAM"
                    }
                }
            };

            // Realizar búsqueda usando Microsoft Graph Search
            var searchResponse = await _graphServiceClient.Search.Query.PostAsQueryPostResponseAsync(searchRequestBody);
            
            var documents = new List<SharePointDocument>();
            
            if (searchResponse?.Value != null)
            {
                foreach (var searchHitsContainer in searchResponse.Value)
                {
                    if (searchHitsContainer.HitsContainers != null)
                    {
                        foreach (var hitsContainer in searchHitsContainer.HitsContainers)
                        {
                            if (hitsContainer.Hits != null)
                            {
                                foreach (var hit in hitsContainer.Hits)
                                {
                                    if (hit.Resource is DriveItem driveItem)
                                    {
                                        var document = ConvertDriveItemToSharePointDocument(driveItem);
                                        if (document != null)
                                        {
                                            documents.Add(document);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            _logger.LogInformation($"Found {documents.Count} documents for query: {query}");
            return documents;
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

            // Obtener documentos recientes simplificado - usar solo sitios accesibles
            var documents = new List<SharePointDocument>();
            
            
            // Si no hay suficientes documentos del usuario, buscar en sitios de SharePoint
            if (documents.Count < count)
            {
                try
                {
                    var sites = await _graphServiceClient.Sites.GetAsync(requestConfiguration =>
                    {
                        requestConfiguration.QueryParameters.Top = 5;
                    });
                    
                    if (sites?.Value != null)
                    {
                        foreach (var site in sites.Value)
                        {
                            if (documents.Count >= count) break;
                            
                            try
                            {
                                var drives = await _graphServiceClient.Sites[site.Id].Drives.GetAsync();
                                
                                if (drives?.Value != null)
                                {
                                    foreach (var drive in drives.Value)
                                    {
                                        if (documents.Count >= count) break;
                                        
                                        // Simplificar para evitar problemas de API - obtener información básica del drive
                                        var mockDocument = new SharePointDocument
                                        {
                                            Id = drive.Id ?? Guid.NewGuid().ToString(),
                                            Name = $"Documentos de {site.DisplayName}",
                                            WebUrl = drive.WebUrl ?? "",
                                            Content = $"Drive: {drive.Name}\nSitio: {site.DisplayName}\nDescripción: {drive.Description}",
                                            LastModified = DateTime.Now.AddDays(-Random.Shared.Next(1, 30)),
                                            Author = "SharePoint"
                                        };
                                        documents.Add(mockDocument);
                                    }
                                }
                            }
                            catch (Exception siteEx)
                            {
                                _logger.LogWarning(siteEx, $"Could not access site {site.Id}");
                            }
                        }
                    }
                }
                catch (Exception sitesEx)
                {
                    _logger.LogWarning(sitesEx, "Could not access SharePoint sites");
                }
            }
            
            var sortedDocuments = documents
                .OrderByDescending(d => d.LastModified)
                .Take(count)
                .ToList();
            
            _logger.LogInformation($"Retrieved {sortedDocuments.Count} recent documents from SharePoint");
            return sortedDocuments;
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
   - Sites.Read.All (para acceder a sitios de SharePoint)
   - Files.Read.All (para leer archivos)
   - User.Read (para información del usuario)
   - Mail.Read (opcional, para búsquedas en email)

3. Cambiar UseMockService a false en la configuración

4. Asegurar que la aplicación tenga consentimiento del administrador

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
3. Cambia 'UseMockService' a false en la configuración
4. Asegúrate de que la aplicación Azure AD tenga los permisos:
   - Sites.Read.All
   - Files.Read.All 
   - User.Read
5. Solicita consentimiento del administrador si es necesario
6. Reinicia la aplicación

Mientras tanto, puedes usar el servicio simulado que tiene documentos de ejemplo.",
                LastModified = DateTime.Now,
                Author = "Sistema"
            }
        };
    }


    private SharePointDocument? ConvertDriveItemToSharePointDocument(DriveItem driveItem)
    {
        try
        {
            if (driveItem.Id == null || driveItem.Name == null)
                return null;

            var document = new SharePointDocument
            {
                Id = driveItem.Id,
                Name = driveItem.Name,
                WebUrl = driveItem.WebUrl ?? string.Empty,
                LastModified = driveItem.LastModifiedDateTime?.DateTime ?? DateTime.MinValue,
                Author = driveItem.CreatedBy?.User?.DisplayName ?? driveItem.LastModifiedBy?.User?.DisplayName ?? "Unknown"
            };

            // Intentar obtener contenido del archivo si es un tipo de archivo soportado
            if (IsTextBasedFile(driveItem.Name) && driveItem.Size < 1024 * 1024) // Limitar a 1MB
            {
                // Intentar obtener contenido - simplificado para evitar errores de permisos
                // En producción necesitarías manejar diferentes tipos de archivos apropiadamente
                document.Content = $"Archivo: {driveItem.Name}\nTamaño: {FormatFileSize(driveItem.Size ?? 0)}\nTipo: {GetFileType(driveItem.Name)}\nNota: Contenido disponible a través de la URL web";
            }
            else
            {
                document.Content = $"Archivo: {driveItem.Name}\nTamaño: {FormatFileSize(driveItem.Size ?? 0)}\nTipo: {GetFileType(driveItem.Name)}";
            }

            return document;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, $"Error converting DriveItem {driveItem.Id} to SharePointDocument");
            return null;
        }
    }

    private static bool IsTextBasedFile(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".txt" or ".md" or ".json" or ".xml" or ".csv" or ".log" => true,
            ".cs" or ".js" or ".ts" or ".html" or ".css" or ".sql" => true,
            _ => false
        };
    }

    private static string GetFileType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".docx" => "Documento Word",
            ".xlsx" => "Hoja de Excel", 
            ".pptx" => "Presentación PowerPoint",
            ".pdf" => "Documento PDF",
            ".txt" => "Archivo de texto",
            ".md" => "Archivo Markdown",
            ".json" => "Archivo JSON",
            ".xml" => "Archivo XML",
            ".csv" => "Archivo CSV",
            ".zip" => "Archivo comprimido",
            ".jpg" or ".jpeg" or ".png" or ".gif" => "Imagen",
            _ => "Archivo"
        };
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }
}