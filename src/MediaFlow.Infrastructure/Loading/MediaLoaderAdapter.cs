using MediaFlow.Domain.Entities;
using MediaFlow.Domain.Enums;
using MediaFlow.Domain.Interfaces;
using MediaFlow.Infrastructure.FileSystem;
using MediaFlow.Infrastructure.Metadata;

namespace MediaFlow.Infrastructure.Loading;

public sealed class MediaLoaderAdapter(
    FileSystemAdapter fs,
    MetadataAdapter metadata,
    string tempFolderPath) : IMediaLoader
{
    private static readonly HashSet<string> ImageExtensions = [".jpg", ".jpeg", ".png"];

    public async Task<IReadOnlyList<FileContext>> LoadBatchAsync(
        string sourceFolderPath, int offset, int limit, CancellationToken ct)
    {
        var sourcePaths = fs.ListMediaFiles(sourceFolderPath, offset, limit);
        var result = new List<FileContext>(sourcePaths.Count);

        foreach (var sourcePath in sourcePaths)
        {
            ct.ThrowIfCancellationRequested();

            var originalName = Path.GetFileName(sourcePath);
            var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
            var tempPath = Path.Combine(tempFolderPath, $"{Guid.NewGuid():N}_{originalName}");

            await fs.CopyAsync(sourcePath, tempPath, ct);

            var meta = metadata.Read(sourcePath);

            result.Add(new FileContext(
                OriginalName: originalName,
                SourcePath: sourcePath,
                TempPath: tempPath,
                Type: ImageExtensions.Contains(ext) ? FileType.Image : FileType.Video,
                AssignedActions: MediaAction.None,
                ExifCaptureDate: meta.CaptureDate));
        }

        return result;
    }
}
