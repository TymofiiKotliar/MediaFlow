using FluentAssertions;
using MediaFlow.Domain.Entities;
using MediaFlow.Domain.Enums;
using MediaFlow.Domain.ValueObjects;
using MediaFlow.Infrastructure.Processing;

namespace MediaFlow.Infrastructure.Tests.Processing;

public sealed class RotationStageAdapterTests
{
    private static readonly DeviceProfile Device = new(
        "id", "Cam", @"C:\src", @"C:\bak", [], "tok", "chat", 50);

    private static PipelineContext Ctx(MediaAction actions, string tempPath = @"C:\tmp\photo.jpg") =>
        new(Device, new FileContext("photo.jpg", @"C:\src\photo.jpg", tempPath, FileType.Image, actions, null));

    private readonly RotationStageAdapter _sut = new();

    // ── Skip logic ────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_NoRotationFlag_ReturnsSkipped()
    {
        var (_, result) = await _sut.ExecuteAsync(Ctx(MediaAction.None), default);

        result.Should().BeOfType<Skipped>();
    }

    [Fact]
    public async Task ExecuteAsync_NonRotationFlag_ReturnsSkipped()
    {
        var (_, result) = await _sut.ExecuteAsync(Ctx(MediaAction.SaveToBackup), default);

        result.Should().BeOfType<Skipped>();
    }

    [Theory]
    [InlineData(MediaAction.SaveToBackup | MediaAction.DeleteAfter)]
    [InlineData(MediaAction.SendToTelegram)]
    public async Task ExecuteAsync_CombinedNonRotationFlags_ReturnsSkipped(MediaAction actions)
    {
        var (_, result) = await _sut.ExecuteAsync(Ctx(actions), default);

        result.Should().BeOfType<Skipped>();
    }

    // ── Context passthrough on skip ───────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_Skipped_ReturnsSameContext()
    {
        var ctx = Ctx(MediaAction.None);

        var (returnedCtx, _) = await _sut.ExecuteAsync(ctx, default);

        returnedCtx.Should().BeSameAs(ctx);
    }
}
