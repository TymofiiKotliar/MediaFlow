using MediaFlow.Domain.Entities;
using MediaFlow.Domain.Enums;
using MediaFlow.Domain.Interfaces;
using MediaFlow.Domain.ValueObjects;

namespace MediaFlow.Application.UseCases;

public sealed class RunPipelineUseCase(
    IVideoConversionStage conversionStage,
    IRotationStage rotationStage,
    IBackupStage backupStage,
    ITelegramStage telegramStage,
    IFileService fileService)
{
    private static readonly MediaAction AnyRotation =
        MediaAction.RotateLeft | MediaAction.RotateRight | MediaAction.Flip180;

    public async Task<RunSummary> ExecuteAsync(
        IReadOnlyList<FileContext> files,
        DeviceProfile device,
        IProgressObserver observer,
        CancellationToken ct = default)
    {
        int backedUp = 0, sentToTelegram = 0, deleted = 0, duplicatesSkipped = 0;
        var fileFailures = new Dictionary<string, List<string>>();
        var toDelete = new List<string>();
        int total = files.Count;

        void RecordFailure(string fileName, string reason)
        {
            if (!fileFailures.TryGetValue(fileName, out var list))
                fileFailures[fileName] = list = [];
            list.Add(reason);
        }

        for (int i = 0; i < files.Count; i++)
        {
            if (ct.IsCancellationRequested)
            {
                observer.PipelineCancelled();
                break;
            }

            var file = files[i];
            observer.FileStarted(file.OriginalName, i + 1, total);

            var context = new PipelineContext(device, file);
            bool fileHadFailure = false;

            // 1. Convert non-MP4 videos
            if (file.Type == FileType.Video)
            {
                var (updated, result) = await conversionStage.ExecuteAsync(context, ct);
                context = updated;
                if (result is Failed f)
                {
                    RecordFailure(file.OriginalName, $"Conversion: {f.Reason}");
                    fileHadFailure = true;
                }
            }

            // 2. Rotate
            if ((file.AssignedActions & AnyRotation) != MediaAction.None)
            {
                var (updated, result) = await rotationStage.ExecuteAsync(context, ct);
                context = updated;
                if (result is Failed f)
                {
                    RecordFailure(file.OriginalName, $"Rotation: {f.Reason}");
                    fileHadFailure = true;
                }
            }

            // 3. Backup
            if (file.AssignedActions.HasFlag(MediaAction.SaveToBackup))
            {
                var (updated, result) = await backupStage.ExecuteAsync(context, ct);
                context = updated;
                switch (result)
                {
                    case Success:
                        backedUp++;
                        break;
                    case Skipped:
                        duplicatesSkipped++;
                        break;
                    case Failed f:
                        RecordFailure(file.OriginalName, $"Backup: {f.Reason}");
                        fileHadFailure = true;
                        break;
                }
            }

            // 4. Send to Telegram
            if (file.AssignedActions.HasFlag(MediaAction.SendToTelegram))
            {
                var (_, result) = await telegramStage.ExecuteAsync(context, ct);
                switch (result)
                {
                    case Success:
                        sentToTelegram++;
                        break;
                    case Failed f:
                        RecordFailure(file.OriginalName, $"Telegram: {f.Reason}");
                        fileHadFailure = true;
                        break;
                }
            }

            // 5. Mark for deletion (deferred; skipped if this file had any failure)
            if (file.AssignedActions.HasFlag(MediaAction.DeleteAfter) && !fileHadFailure)
                toDelete.Add(file.SourcePath);

            observer.FileCompleted(file.OriginalName);
        }

        // Delete source files after the full pipeline completes
        foreach (var path in toDelete)
        {
            try
            {
                await fileService.DeleteAsync(path, ct);
                deleted++;
            }
            catch (Exception ex)
            {
                RecordFailure(Path.GetFileName(path), $"Delete: {ex.Message}");
            }
        }

        var failedFiles = fileFailures
            .Select(kv => new FailedFile(kv.Key, string.Join("; ", kv.Value)))
            .ToList();

        var summary = new RunSummary(backedUp, sentToTelegram, deleted, duplicatesSkipped, fileFailures.Count, failedFiles);
        observer.PipelineFinished(summary);
        return summary;
    }
}
