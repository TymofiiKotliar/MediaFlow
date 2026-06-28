using FluentAssertions;
using MediaFlow.Domain.Enums;
using MediaFlow.Domain.Interfaces;
using MediaFlow.Infrastructure.FileSystem;
using MediaFlow.Infrastructure.Loading;
using MediaFlow.Infrastructure.Metadata;
using NSubstitute;

namespace MediaFlow.Infrastructure.Tests.Loading;

public sealed class MediaLoaderAdapterTests : IDisposable
{
    private readonly string _sourceDir =
        Path.Combine(Path.GetTempPath(), $"MediaFlow.Source.{Guid.NewGuid():N}");
    private readonly string _tempDir =
        Path.Combine(Path.GetTempPath(), $"MediaFlow.Temp.{Guid.NewGuid():N}");

    private readonly MediaLoaderAdapter _sut;

    public MediaLoaderAdapterTests()
    {
        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_tempDir);
        _sut = new MediaLoaderAdapter(new FileSystemAdapter(), new MetadataAdapter(), Substitute.For<IRotationStage>(), _tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_sourceDir)) Directory.Delete(_sourceDir, recursive: true);
        if (Directory.Exists(_tempDir))  Directory.Delete(_tempDir,  recursive: true);
    }

    private string SourceFile(string name)
    {
        var path = Path.Combine(_sourceDir, name);
        File.WriteAllText(path, name);
        return path;
    }

    // ── Count and empty ───────────────────────────────────────────────────────

    [Fact]
    public async Task LoadBatchAsync_EmptyFolder_ReturnsEmptyList()
    {
        var result = await _sut.LoadBatchAsync(_sourceDir, 0, 10, default);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadBatchAsync_ThreeFiles_ReturnsThreeContexts()
    {
        SourceFile("a.jpg"); SourceFile("b.jpg"); SourceFile("c.jpg");

        var result = await _sut.LoadBatchAsync(_sourceDir, 0, 10, default);

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task LoadBatchAsync_RespectsOffsetAndLimit()
    {
        SourceFile("a.jpg"); SourceFile("b.jpg"); SourceFile("c.jpg");

        var result = await _sut.LoadBatchAsync(_sourceDir, offset: 1, limit: 1, default);

        result.Should().HaveCount(1);
        result[0].OriginalName.Should().Be("b.jpg");
    }

    // ── FileContext fields ────────────────────────────────────────────────────

    [Fact]
    public async Task LoadBatchAsync_OriginalName_IsFileName()
    {
        SourceFile("photo.jpg");

        var result = await _sut.LoadBatchAsync(_sourceDir, 0, 10, default);

        result[0].OriginalName.Should().Be("photo.jpg");
    }

    [Fact]
    public async Task LoadBatchAsync_SourcePath_PointsToSourceFolder()
    {
        var path = SourceFile("photo.jpg");

        var result = await _sut.LoadBatchAsync(_sourceDir, 0, 10, default);

        result[0].SourcePath.Should().Be(path);
    }

    [Fact]
    public async Task LoadBatchAsync_TempPath_IsInsideTempFolder()
    {
        SourceFile("photo.jpg");

        var result = await _sut.LoadBatchAsync(_sourceDir, 0, 10, default);

        result[0].TempPath.Should().StartWith(_tempDir);
    }

    [Fact]
    public async Task LoadBatchAsync_TempPath_PreservesExtension()
    {
        SourceFile("clip.mp4");

        var result = await _sut.LoadBatchAsync(_sourceDir, 0, 10, default);

        Path.GetExtension(result[0].TempPath).Should().Be(".mp4");
    }

    [Fact]
    public async Task LoadBatchAsync_AssignedActions_IsNone()
    {
        SourceFile("photo.jpg");

        var result = await _sut.LoadBatchAsync(_sourceDir, 0, 10, default);

        result[0].AssignedActions.Should().Be(MediaAction.None);
    }

    [Fact]
    public async Task LoadBatchAsync_ExifCaptureDate_IsNullForPlainFiles()
    {
        SourceFile("photo.jpg");

        var result = await _sut.LoadBatchAsync(_sourceDir, 0, 10, default);

        result[0].ExifCaptureDate.Should().BeNull();
    }

    // ── FileType resolution ───────────────────────────────────────────────────

    [Theory]
    [InlineData("photo.jpg")]
    [InlineData("photo.jpeg")]
    [InlineData("photo.png")]
    public async Task LoadBatchAsync_ImageExtension_SetsTypeToImage(string name)
    {
        SourceFile(name);

        var result = await _sut.LoadBatchAsync(_sourceDir, 0, 10, default);

        result[0].Type.Should().Be(FileType.Image);
    }

    [Theory]
    [InlineData("clip.mp4")]
    [InlineData("clip.avi")]
    [InlineData("clip.mov")]
    public async Task LoadBatchAsync_VideoExtension_SetsTypeToVideo(string name)
    {
        SourceFile(name);

        var result = await _sut.LoadBatchAsync(_sourceDir, 0, 10, default);

        result[0].Type.Should().Be(FileType.Video);
    }

    // ── File copy ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task LoadBatchAsync_CopiesFileToTempFolder()
    {
        SourceFile("photo.jpg");

        await _sut.LoadBatchAsync(_sourceDir, 0, 10, default);

        Directory.GetFiles(_tempDir).Should().HaveCount(1);
    }

    [Fact]
    public async Task LoadBatchAsync_SourceFileRemainsIntact()
    {
        var path = SourceFile("photo.jpg");

        await _sut.LoadBatchAsync(_sourceDir, 0, 10, default);

        File.Exists(path).Should().BeTrue();
    }

    [Fact]
    public async Task LoadBatchAsync_TempPath_FileActuallyExists()
    {
        SourceFile("photo.jpg");

        var result = await _sut.LoadBatchAsync(_sourceDir, 0, 10, default);

        File.Exists(result[0].TempPath).Should().BeTrue();
    }

    [Fact]
    public async Task LoadBatchAsync_TwoFiles_EachGetDistinctTempPath()
    {
        SourceFile("a.jpg"); SourceFile("b.jpg");

        var result = await _sut.LoadBatchAsync(_sourceDir, 0, 10, default);

        result[0].TempPath.Should().NotBe(result[1].TempPath);
    }

    // ── Cancellation ──────────────────────────────────────────────────────────

    [Fact]
    public async Task LoadBatchAsync_CancelledBeforeStart_Throws()
    {
        SourceFile("photo.jpg");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await _sut.Invoking(s => s.LoadBatchAsync(_sourceDir, 0, 10, cts.Token))
            .Should().ThrowAsync<OperationCanceledException>();
    }
}
