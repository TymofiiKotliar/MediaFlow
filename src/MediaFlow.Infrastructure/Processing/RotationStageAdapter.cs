using FFMpegCore;
using MediaFlow.Domain.Entities;
using MediaFlow.Domain.Enums;
using MediaFlow.Domain.Interfaces;
using MediaFlow.Domain.ValueObjects;

namespace MediaFlow.Infrastructure.Processing;

public sealed class RotationStageAdapter : IRotationStage
{
    private const MediaAction RotationMask =
        MediaAction.RotateLeft | MediaAction.RotateRight | MediaAction.Flip180;

    public async Task<(PipelineContext Context, PipelineStageResult Result)> ExecuteAsync(
        PipelineContext context, CancellationToken ct)
    {
        var actions = context.File.AssignedActions;

        if ((actions & RotationMask) == MediaAction.None)
            return (context, new Skipped("No rotation action assigned"));

        var inputPath = context.File.TempPath;
        var ext = Path.GetExtension(inputPath);
        var outputPath = inputPath[..^ext.Length] + ".processing" + ext;

        try
        {
            await FFMpegArguments
                .FromFileInput(inputPath)
                .OutputToFile(outputPath, overwrite: true, options =>
                    options.WithCustomArgument($"-vf {BuildFilter(actions)}"))
                .CancellableThrough(ct)
                .ProcessAsynchronously();

            File.Delete(inputPath);
            File.Move(outputPath, inputPath);

            return (context, new Success());
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
            return (context, new Failed($"Rotation failed: {ex.Message}"));
        }
    }

    private static string BuildFilter(MediaAction actions)
    {
        if (actions.HasFlag(MediaAction.RotateLeft))  return "transpose=2";
        if (actions.HasFlag(MediaAction.RotateRight)) return "transpose=1";
        return "hflip,vflip"; // Flip180
    }
}
