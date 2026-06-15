using MediaFlow.Domain.Entities;
using MediaFlow.Domain.Interfaces;

namespace MediaFlow.Application.UseCases;

public sealed class LoadMediaUseCase(IMediaLoader loader)
{
    public async Task<LoadMediaResult> ExecuteAsync(
        DeviceProfile device,
        int offset,
        CancellationToken ct = default)
    {
        try
        {
            var files = await loader.LoadBatchAsync(
                device.SourceFolderPath, offset, device.FilesPerLoad, ct);

            return new LoadMediaResult.Success(files);
        }
        catch (Exception ex)
        {
            return new LoadMediaResult.SourceNotAccessible(device.SourceFolderPath, ex.Message);
        }
    }
}

public abstract record LoadMediaResult
{
    public sealed record Success(IReadOnlyList<FileContext> Files) : LoadMediaResult;
    public sealed record SourceNotAccessible(string Path, string Reason) : LoadMediaResult;
}
