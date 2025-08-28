namespace AssistEC.Models;

public class DocumentContextSettings
{
    public int MaxTokens { get; set; } = 4000;
    public int MaxDocuments { get; set; } = 10;
    public double RelevanceThreshold { get; set; } = 0.7;
    public int ChunkSize { get; set; } = 500;
    public int CacheExpirationMinutes { get; set; } = 30;
    public int MaxSentencesPerDocument { get; set; } = 5;
}