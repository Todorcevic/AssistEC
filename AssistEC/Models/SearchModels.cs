using Microsoft.Graph.Models;

namespace AssistEC.Models;

// Modelos auxiliares para Microsoft Graph Search API
public class SearchPostRequestBody
{
    public List<SearchRequest> Requests { get; set; } = new();
}