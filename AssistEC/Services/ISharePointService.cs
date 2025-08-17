using AssistEC.Models;

namespace AssistEC.Services;

public interface ISharePointService
{
    Task<List<SharePointDocument>> SearchDocumentsAsync(string query);
    Task<List<SharePointDocument>> GetRecentDocumentsAsync(int count = 10);
}