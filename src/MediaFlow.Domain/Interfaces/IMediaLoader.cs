using MediaFlow.Domain.Entities;

namespace MediaFlow.Domain.Interfaces;

public interface IMediaLoader
{
    Task<IReadOnlyList<FileContext>> LoadBatchAsync(
        string sourceFolderPath, int offset, int limit, CancellationToken ct);
}
