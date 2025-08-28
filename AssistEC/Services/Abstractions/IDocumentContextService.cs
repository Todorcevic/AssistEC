using AssistEC.Models;

namespace AssistEC.Services.Abstractions;

public interface IDocumentContextService
{
    Task<string> GetOptimizedContextAsync(List<SharePointDocument> documents, string userQuery);
    Task<List<DocumentChunk>> CreateSmartChunksAsync(SharePointDocument document);
    Task<List<DocumentChunk>> GetOrCreateChunksAsync(SharePointDocument document);
    void ClearCache();
}