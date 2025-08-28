using AssistEC.Models;

namespace AssistEC.Services.Abstractions;

public interface ISharePointService
{
    Task<List<SharePointDocument>> SearchDocumentsAsync(string query);
    Task<List<SharePointDocument>> GetRecentDocumentsAsync(int count = 10);
}