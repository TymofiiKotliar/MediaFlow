using MediaFlow.Application.UseCases;
using MediaFlow.Domain.Entities;
using MediaFlow.Domain.Enums;
using MediaFlow.Domain.Interfaces;
using MediaFlow.Domain.ValueObjects;
using MediaFlow.Infrastructure.FileSystem;

namespace MediaFlow.Infrastructure.Backup;

public sealed class BackupStageAdapter(
    FileSystemAdapter fs,
    BuildNamingTemplateUseCase naming) : IBackupStage
{
    public async Task<(PipelineContext Context, PipelineStageResult Result)> ExecuteAsync(
        PipelineContext context, CancellationToken ct)
    {
        if (!context.File.AssignedActions.HasFlag(MediaAction.SaveToBackup))
            return (context, new Skipped("SaveToBackup not assigned"));

        var backupFolder = context.Device.BackupFolderPath;
        fs.EnsureDirectoryExists(backupFolder);

        var sequenceNumber = fs.ListAllFiles(backupFolder).Count + 1;
        var resolvedName = naming.Execute(
            context.Device.NamingTemplate,
            context.File.OriginalName,
            context.File.ExifCaptureDate,
            sequenceNumber);

        var destPath = Path.Combine(backupFolder, resolvedName);
        if (fs.FileExists(destPath))
            return (context, new Failed($"Backup already exists: {resolvedName}"));

        try
        {
            await fs.CopyAsync(context.File.TempPath, destPath, ct);
            return (context, new Success());
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            return (context, new Failed($"Backup failed: {ex.Message}"));
        }
    }
}
