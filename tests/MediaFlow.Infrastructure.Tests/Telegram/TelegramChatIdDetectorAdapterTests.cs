using FluentAssertions;
using MediaFlow.Domain.ValueObjects;
using MediaFlow.Infrastructure.Telegram;

namespace MediaFlow.Infrastructure.Tests.Telegram;

public sealed class TelegramChatIdDetectorAdapterTests
{
    private readonly TelegramChatIdDetectorAdapter _sut = new(new HttpClient());

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task DetectAsync_EmptyToken_ReturnsFailed(string token)
    {
        var result = await _sut.DetectAsync(token, default);

        result.Should().BeOfType<TelegramChatIdDetectionFailed>();
    }

    [Theory]
    [InlineData("not-a-real-token-at-all")]
    [InlineData("12345")]
    [InlineData("123456789")]
    public async Task DetectAsync_MalformedTokenShape_ReturnsFailedInsteadOfThrowing(string token)
    {
        var result = await _sut.DetectAsync(token, default);

        result.Should().BeOfType<TelegramChatIdDetectionFailed>();
    }
}
