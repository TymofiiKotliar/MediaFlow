namespace MediaFlow.Domain.Interfaces;

public interface IFileService
{
    Task DeleteAsync(string path, CancellationToken ct = default);
}
