using MediaFlow.Domain.Interfaces;

namespace MediaFlow.Infrastructure.FileSystem;

public sealed class FileSystemAdapter : IFileService
{
    private static readonly HashSet<string> MediaExtensions =
        [".jpg", ".jpeg", ".png", ".mp4", ".avi", ".mov"];

    // ── IFileService ──────────────────────────────────────────────────────────

    public Task DeleteAsync(string path, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        File.Delete(path);
        return Task.CompletedTask;
    }

    // ── File operations (used by other Infrastructure adapters) ───────────────

    public Task CopyAsync(string source, string destination, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        File.Copy(source, destination, overwrite: false);
        return Task.CompletedTask;
    }

    public bool FileExists(string path) => File.Exists(path);

    public DateTime GetLastWriteTime(string path) => File.GetLastWriteTime(path);

    // ── Directory operations ──────────────────────────────────────────────────

    public void EnsureDirectoryExists(string path) => Directory.CreateDirectory(path);

    public IReadOnlyList<string> ListMediaFiles(string folderPath, int offset, int limit)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(offset);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(limit, 0);

        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Source folder not found: {folderPath}");

        return Directory.EnumerateFiles(folderPath)
            .Where(f => MediaExtensions.Contains(
                Path.GetExtension(f).ToLowerInvariant()))
            .OrderBy(f => Path.GetFileName(f), StringComparer.OrdinalIgnoreCase)
            .Skip(offset)
            .Take(limit)
            .ToList();
    }

    public IReadOnlyList<string> ListAllFiles(string folderPath)
    {
        if (!Directory.Exists(folderPath))
            throw new DirectoryNotFoundException($"Folder not found: {folderPath}");

        return Directory.GetFiles(folderPath);
    }

    public void ClearDirectory(string folderPath)
    {
        if (!Directory.Exists(folderPath)) return;

        foreach (var file in Directory.GetFiles(folderPath))
            File.Delete(file);
    }
}
