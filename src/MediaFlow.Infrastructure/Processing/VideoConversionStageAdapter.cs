using FFMpegCore;
using FFMpegCore.Enums;
using MediaFlow.Domain.Entities;
using MediaFlow.Domain.Enums;
using MediaFlow.Domain.Interfaces;
using MediaFlow.Domain.ValueObjects;

namespace MediaFlow.Infrastructure.Processing;

public sealed class VideoConversionStageAdapter : IVideoConversionStage
{
    public async Task<(PipelineContext Context, PipelineStageResult Result)> ExecuteAsync(
        PipelineContext context, CancellationToken ct)
    {
        if (context.File.Type != FileType.Video)
            return (context, new Skipped("Not a video file"));

        var inputPath = context.File.TempPath;

        if (Path.GetExtension(inputPath).Equals(".mp4", StringComparison.OrdinalIgnoreCase))
            return (context, new Skipped("Already in MP4 format"));

        var outputPath = Path.ChangeExtension(inputPath, ".mp4");

        try
        {
            await FFMpegArguments
                .FromFileInput(inputPath)
                .OutputToFile(outputPath, overwrite: true, options => options
                    .WithVideoCodec(VideoCodec.LibX264)
                    .WithAudioCodec(AudioCodec.Aac)
                    .WithConstantRateFactor(23))
                .CancellableThrough(ct)
                .ProcessAsynchronously();

            File.Delete(inputPath);

            var newName = Path.ChangeExtension(context.File.OriginalName, ".mp4");
            var updatedFile = context.File with { TempPath = outputPath, OriginalName = newName };
            return (context with { File = updatedFile }, new Success());
        }
        catch (OperationCanceledException)
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
            throw;
        }
        catch (Exception ex)
        {
            if (File.Exists(outputPath)) File.Delete(outputPath);
            ct.ThrowIfCancellationRequested();
            return (context, new Failed($"Video conversion failed: {ex.Message}"));
        }
    }
}
