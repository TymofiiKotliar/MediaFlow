using FluentAssertions;
using MediaFlow.Domain.Entities;
using MediaFlow.Domain.Enums;
using MediaFlow.Domain.ValueObjects;
using MediaFlow.Infrastructure.Processing;

namespace MediaFlow.Infrastructure.Tests.Processing;

public sealed class VideoConversionStageAdapterTests
{
    private static readonly DeviceProfile Device = new(
        "id", "Cam", @"C:\src", @"C:\bak", [], "tok", "chat", 50);

    private static PipelineContext Ctx(FileType type, string tempPath) =>
        new(Device, new FileContext(
            Path.GetFileName(tempPath), @"C:\src\" + Path.GetFileName(tempPath),
            tempPath, type, MediaAction.None, null));

    private readonly VideoConversionStageAdapter _sut = new();

    // ── Skip logic ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_ImageFile_ReturnsSkipped()
    {
        var (_, result) = await _sut.ExecuteAsync(
            Ctx(FileType.Image, @"C:\tmp\photo.jpg"), default);

        result.Should().BeOfType<Skipped>();
    }

    [Fact]
    public async Task ExecuteAsync_VideoAlreadyMp4_ReturnsSkipped()
    {
        var (_, result) = await _sut.ExecuteAsync(
            Ctx(FileType.Video, @"C:\tmp\video.mp4"), default);

        result.Should().BeOfType<Skipped>();
    }

    [Fact]
    public async Task ExecuteAsync_VideoAlreadyMp4_CaseInsensitive()
    {
        var (_, result) = await _sut.ExecuteAsync(
            Ctx(FileType.Video, @"C:\tmp\video.MP4"), default);

        result.Should().BeOfType<Skipped>();
    }

    // ── Context passthrough on skip ───────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_ImageSkipped_ReturnsSameContext()
    {
        var ctx = Ctx(FileType.Image, @"C:\tmp\photo.jpg");

        var (returnedCtx, _) = await _sut.ExecuteAsync(ctx, default);

        returnedCtx.Should().BeSameAs(ctx);
    }

    [Fact]
    public async Task ExecuteAsync_Mp4Skipped_ReturnsSameContext()
    {
        var ctx = Ctx(FileType.Video, @"C:\tmp\video.mp4");

        var (returnedCtx, _) = await _sut.ExecuteAsync(ctx, default);

        returnedCtx.Should().BeSameAs(ctx);
    }
}
