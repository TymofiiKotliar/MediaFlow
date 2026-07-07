using FluentAssertions;
using MediaFlow.Domain.ValueObjects;
using MediaFlow.Infrastructure.Telegram;

namespace MediaFlow.Infrastructure.Tests.Telegram;

public sealed class TelegramBotVerifierAdapterTests
{
    private readonly TelegramBotVerifierAdapter _sut = new(new HttpClient());

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task VerifyAsync_EmptyToken_ReturnsFailed(string token)
    {
        var result = await _sut.VerifyAsync(token, default);

        result.Should().BeOfType<TelegramBotVerificationFailed>();
    }

    [Theory]
    [InlineData("not-a-real-token-at-all")]
    [InlineData("12345")]
    [InlineData("123456789")]
    public async Task VerifyAsync_MalformedTokenShape_ReturnsFailedInsteadOfThrowing(string token)
    {
        var result = await _sut.VerifyAsync(token, default);

        result.Should().BeOfType<TelegramBotVerificationFailed>();
    }
}
