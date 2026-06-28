namespace MediaFlow.Domain.Interfaces;

public interface IThumbnailService
{
    Task<byte[]> GenerateAsync(string filePath, int maxSize = 256, CancellationToken ct = default);
}
