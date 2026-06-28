using FFMpegCore;
using MediaFlow.Domain.Interfaces;

namespace MediaFlow.Infrastructure.Processing;

public sealed class ThumbnailService : IThumbnailService
{
    private static readonly HashSet<string> ImageExtensions = [".jpg", ".jpeg", ".png"];
    private static readonly HashSet<string> VideoExtensions = [".mp4", ".avi", ".mov"];

    public Task<byte[]> GenerateAsync(string filePath, int maxSize = 256, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();

        if (ImageExtensions.Contains(ext)) return ExtractFrameAsync(filePath, maxSize, seekTo: null, ct);
        if (VideoExtensions.Contains(ext)) return ExtractFrameAsync(filePath, maxSize, TimeSpan.FromSeconds(1), ct);

        throw new NotSupportedException($"Unsupported extension for thumbnail: {ext}");
    }

    private static async Task<byte[]> ExtractFrameAsync(
        string filePath, int maxSize, TimeSpan? seekTo, CancellationToken ct)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"mf_thumb_{Guid.NewGuid():N}.jpg");
        try
        {
            await FFMpegArguments
                .FromFileInput(filePath, false,
                    seekTo.HasValue ? o => o.Seek(seekTo.Value) : null)
                .OutputToFile(tempPath, overwrite: true, options => options
                    // fit within maxSize×maxSize, preserving aspect ratio
                    .WithCustomArgument($"-vf scale={maxSize}:{maxSize}:force_original_aspect_ratio=decrease")
                    .WithFrameOutputCount(1))
                .CancellableThrough(ct)
                .ProcessAsynchronously();

            return await File.ReadAllBytesAsync(tempPath, ct);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }
}
