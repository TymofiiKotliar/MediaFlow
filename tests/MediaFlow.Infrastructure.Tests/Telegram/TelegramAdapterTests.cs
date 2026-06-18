using FluentAssertions;
using MediaFlow.Domain.Entities;
using MediaFlow.Domain.Enums;
using MediaFlow.Domain.ValueObjects;
using MediaFlow.Infrastructure.Telegram;

namespace MediaFlow.Infrastructure.Tests.Telegram;

public sealed class TelegramAdapterTests
{
    private readonly TelegramAdapter _sut = new(new HttpClient());

    private static PipelineContext Ctx(
        MediaAction actions,
        string botToken = "valid-token",
        string chatId = "-100123456") => new(
            Device: new DeviceProfile(
                "id", "Cam", @"C:\src", @"C:\bak", [], botToken, chatId, 50),
            File: new FileContext(
                "photo.jpg", @"C:\src\photo.jpg", @"C:\tmp\photo.jpg",
                FileType.Image, actions, null));

    // ── Skip ─────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_SendToTelegramNotAssigned_ReturnsSkipped()
    {
        var (_, result) = await _sut.ExecuteAsync(Ctx(MediaAction.None), default);

        result.Should().BeOfType<Skipped>();
    }

    [Fact]
    public async Task ExecuteAsync_OtherActionsButNotTelegram_ReturnsSkipped()
    {
        var (_, result) = await _sut.ExecuteAsync(
            Ctx(MediaAction.SaveToBackup | MediaAction.DeleteAfter), default);

        result.Should().BeOfType<Skipped>();
    }

    // ── Config validation ─────────────────────────────────────────────────────

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task ExecuteAsync_EmptyBotToken_ReturnsFailed(string token)
    {
        var (_, result) = await _sut.ExecuteAsync(
            Ctx(MediaAction.SendToTelegram, botToken: token), default);

        result.Should().BeOfType<Failed>()
            .Which.Reason.Should().Contain("bot token");
    }

    [Theory]
    [InlineData("not-a-number")]
    [InlineData("")]
    [InlineData("12.34")]
    public async Task ExecuteAsync_InvalidChatId_ReturnsFailed(string chatId)
    {
        var (_, result) = await _sut.ExecuteAsync(
            Ctx(MediaAction.SendToTelegram, chatId: chatId), default);

        result.Should().BeOfType<Failed>()
            .Which.Reason.Should().Contain("chat ID");
    }

    // ── Context passthrough ───────────────────────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_Skipped_ReturnsSameContext()
    {
        var ctx = Ctx(MediaAction.None);

        var (returnedCtx, _) = await _sut.ExecuteAsync(ctx, default);

        returnedCtx.Should().BeSameAs(ctx);
    }
}
