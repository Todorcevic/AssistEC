namespace AssistEC.Services.Abstractions;

public interface IEmbeddingService
{
    Task<float[]> GetEmbeddingAsync(string text);
    Task<List<float[]>> GetEmbeddingsAsync(List<string> texts);
    double CosineSimilarity(float[] embedding1, float[] embedding2);
}