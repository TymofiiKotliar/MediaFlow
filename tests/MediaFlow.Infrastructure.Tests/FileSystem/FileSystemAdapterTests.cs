using FluentAssertions;
using MediaFlow.Infrastructure.FileSystem;

namespace MediaFlow.Infrastructure.Tests.FileSystem;

public sealed class FileSystemAdapterTests : IDisposable
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), $"MediaFlow.Tests.{Guid.NewGuid():N}");
    private readonly FileSystemAdapter _sut = new();

    public FileSystemAdapterTests() => Directory.CreateDirectory(_root);
    public void Dispose() => Directory.Delete(_root, recursive: true);

    private string TempFile(string name, string? content = null)
    {
        var path = Path.Combine(_root, name);
        File.WriteAllText(path, content ?? name);
        return path;
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingFile_DeletesIt()
    {
        var path = TempFile("photo.jpg");

        await _sut.DeleteAsync(path);

        File.Exists(path).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteAsync_DoesNotAffectOtherFiles()
    {
        var target = TempFile("delete-me.jpg");
        var other  = TempFile("keep-me.jpg");

        await _sut.DeleteAsync(target);

        File.Exists(other).Should().BeTrue();
    }

    [Fact]
    public async Task DeleteAsync_CancelledToken_ThrowsAndDoesNotDelete()
    {
        var path = TempFile("photo.jpg");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await _sut.Invoking(s => s.DeleteAsync(path, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();

        File.Exists(path).Should().BeTrue();
    }

    // ── CopyAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task CopyAsync_CreatesFileAtDestination()
    {
        var source = TempFile("source.jpg", "data");
        var dest   = Path.Combine(_root, "dest.jpg");

        await _sut.CopyAsync(source, dest);

        File.Exists(dest).Should().BeTrue();
        File.ReadAllText(dest).Should().Be("data");
    }

    [Fact]
    public async Task CopyAsync_LeavesSourceIntact()
    {
        var source = TempFile("source.jpg");
        var dest   = Path.Combine(_root, "dest.jpg");

        await _sut.CopyAsync(source, dest);

        File.Exists(source).Should().BeTrue();
    }

    [Fact]
    public async Task CopyAsync_DestinationAlreadyExists_Throws()
    {
        var source = TempFile("source.jpg");
        var dest   = TempFile("dest.jpg");

        await _sut.Invoking(s => s.CopyAsync(source, dest))
            .Should().ThrowAsync<IOException>();
    }

    // ── FileExists ────────────────────────────────────────────────────────────

    [Fact]
    public void FileExists_ExistingFile_ReturnsTrue()
    {
        var path = TempFile("photo.jpg");

        _sut.FileExists(path).Should().BeTrue();
    }

    [Fact]
    public void FileExists_NonExistingFile_ReturnsFalse()
    {
        _sut.FileExists(Path.Combine(_root, "ghost.jpg")).Should().BeFalse();
    }

    // ── GetLastWriteTime ──────────────────────────────────────────────────────

    [Fact]
    public void GetLastWriteTime_ReturnsReasonableTimestamp()
    {
        var path = TempFile("photo.jpg");

        var ts = _sut.GetLastWriteTime(path);

        ts.Should().BeCloseTo(DateTime.Now, precision: TimeSpan.FromSeconds(5));
    }

    // ── EnsureDirectoryExists ─────────────────────────────────────────────────

    [Fact]
    public void EnsureDirectoryExists_DirectoryAbsent_CreatesIt()
    {
        var dir = Path.Combine(_root, "new-folder");

        _sut.EnsureDirectoryExists(dir);

        Directory.Exists(dir).Should().BeTrue();
    }

    [Fact]
    public void EnsureDirectoryExists_DirectoryAlreadyExists_DoesNotThrow()
    {
        _sut.Invoking(s => s.EnsureDirectoryExists(_root))
            .Should().NotThrow();
    }

    // ── ListMediaFiles ────────────────────────────────────────────────────────

    [Fact]
    public void ListMediaFiles_ReturnsOnlySupportedExtensions()
    {
        TempFile("a.jpg"); TempFile("b.PNG"); TempFile("c.mp4");
        TempFile("d.txt"); TempFile("e.raw");

        var result = _sut.ListMediaFiles(_root, offset: 0, limit: 100);

        result.Should().HaveCount(3);
        result.Select(Path.GetFileName).Should()
            .BeEquivalentTo(["a.jpg", "b.PNG", "c.mp4"]);
    }

    [Fact]
    public void ListMediaFiles_RespectsOffset()
    {
        TempFile("a.jpg"); TempFile("b.jpg"); TempFile("c.jpg");

        var result = _sut.ListMediaFiles(_root, offset: 1, limit: 100);

        result.Should().HaveCount(2);
        Path.GetFileName(result[0]).Should().Be("b.jpg");
    }

    [Fact]
    public void ListMediaFiles_RespectsLimit()
    {
        TempFile("a.jpg"); TempFile("b.jpg"); TempFile("c.jpg");

        var result = _sut.ListMediaFiles(_root, offset: 0, limit: 2);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void ListMediaFiles_SortedAlphabetically()
    {
        TempFile("c.jpg"); TempFile("a.jpg"); TempFile("b.jpg");

        var result = _sut.ListMediaFiles(_root, offset: 0, limit: 100);

        result.Select(Path.GetFileName)
            .Should().ContainInOrder("a.jpg", "b.jpg", "c.jpg");
    }

    [Fact]
    public void ListMediaFiles_NegativeOffset_Throws()
    {
        _sut.Invoking(s => s.ListMediaFiles(_root, offset: -1, limit: 10))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("offset");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ListMediaFiles_NonPositiveLimit_Throws(int limit)
    {
        _sut.Invoking(s => s.ListMediaFiles(_root, offset: 0, limit: limit))
            .Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("limit");
    }

    [Fact]
    public void ListMediaFiles_FolderDoesNotExist_ThrowsDirectoryNotFoundException()
    {
        _sut.Invoking(s => s.ListMediaFiles(Path.Combine(_root, "missing"), 0, 10))
            .Should().Throw<DirectoryNotFoundException>();
    }

    [Fact]
    public void ListMediaFiles_EmptyFolder_ReturnsEmptyList()
    {
        var result = _sut.ListMediaFiles(_root, offset: 0, limit: 100);

        result.Should().BeEmpty();
    }

    // ── ListAllFiles ──────────────────────────────────────────────────────────

    [Fact]
    public void ListAllFiles_ReturnsAllFilesRegardlessOfExtension()
    {
        TempFile("a.jpg"); TempFile("b.txt"); TempFile("c.raw");

        var result = _sut.ListAllFiles(_root);

        result.Should().HaveCount(3);
    }

    [Fact]
    public void ListAllFiles_FolderDoesNotExist_ThrowsDirectoryNotFoundException()
    {
        _sut.Invoking(s => s.ListAllFiles(Path.Combine(_root, "missing")))
            .Should().Throw<DirectoryNotFoundException>();
    }

    // ── ClearDirectory ────────────────────────────────────────────────────────

    [Fact]
    public void ClearDirectory_DeletesAllFiles()
    {
        TempFile("a.jpg"); TempFile("b.jpg");

        _sut.ClearDirectory(_root);

        Directory.GetFiles(_root).Should().BeEmpty();
    }

    [Fact]
    public void ClearDirectory_FolderDoesNotExist_DoesNotThrow()
    {
        _sut.Invoking(s => s.ClearDirectory(Path.Combine(_root, "ghost")))
            .Should().NotThrow();
    }
}
