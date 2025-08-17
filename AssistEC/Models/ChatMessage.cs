namespace AssistEC.Models;

public class ChatMessage
{
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.Now;
    public List<string>? Sources { get; set; }
}

public class SharePointDocument
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string WebUrl { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime LastModified { get; set; }
    public string Author { get; set; } = string.Empty;
}