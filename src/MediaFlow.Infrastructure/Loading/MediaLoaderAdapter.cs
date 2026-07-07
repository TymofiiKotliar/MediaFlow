using MediaFlow.Domain.Entities;
using MediaFlow.Domain.Enums;
using MediaFlow.Domain.Interfaces;
using MediaFlow.Infrastructure.FileSystem;
using MediaFlow.Infrastructure.Metadata;

namespace MediaFlow.Infrastructure.Loading;

public sealed class MediaLoaderAdapter(
    FileSystemAdapter fs,
    MetadataAdapter metadata,
    IRotationStage rotation,
    string tempFolderPath) : IMediaLoader
{
    private static readonly HashSet<string> ImageExtensions = [".jpg", ".jpeg", ".png"];

    public Task<IReadOnlyList<FileContext>> LoadBatchAsync(
        string sourceFolderPath, int offset, int limit, ILoadProgressObserver observer, CancellationToken ct) =>
        Task.Run(async () =>
        {
            // Directory enumeration + per-file EXIF/copy work below is synchronous and can take
            // seconds on large folders — this whole batch runs off the UI thread.
            var sourcePaths = fs.ListMediaFiles(sourceFolderPath, offset, limit);
            var result = new List<FileContext>(sourcePaths.Count);
            var total = sourcePaths.Count;

            for (int i = 0; i < sourcePaths.Count; i++)
            {
                ct.ThrowIfCancellationRequested();

                var sourcePath = sourcePaths[i];
                var originalName = Path.GetFileName(sourcePath);
                observer.FileLoading(originalName, i + 1, total);

                var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
                var isImage = ImageExtensions.Contains(ext);
                var tempPath = Path.Combine(tempFolderPath, $"{Guid.NewGuid():N}_{originalName}");

                await fs.CopyAsync(sourcePath, tempPath, ct);

                var meta = metadata.Read(sourcePath);

                // Correct EXIF orientation on the temp copy so thumbnails display upright.
                // RotationStageAdapter never reads PipelineContext.Device, so null is safe here.
                if (meta.AutoRotation is not null && isImage)
                {
                    var rotCtx = new PipelineContext(
                        null!,
                        new FileContext(originalName, sourcePath, tempPath,
                                        FileType.Image, meta.AutoRotation.Value, meta.CaptureDate));
                    await rotation.ExecuteAsync(rotCtx, ct);
                }

                result.Add(new FileContext(
                    OriginalName: originalName,
                    SourcePath: sourcePath,
                    TempPath: tempPath,
                    Type: isImage ? FileType.Image : FileType.Video,
                    AssignedActions: MediaAction.None,
                    ExifCaptureDate: meta.CaptureDate,
                    CreatedDate: fs.GetCreationTime(sourcePath)));
            }

            return (IReadOnlyList<FileContext>)result;
        }, ct);
}
