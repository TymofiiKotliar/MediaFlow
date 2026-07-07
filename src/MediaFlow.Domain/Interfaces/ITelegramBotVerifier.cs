using MediaFlow.Domain.ValueObjects;

namespace MediaFlow.Domain.Interfaces;

public interface ITelegramBotVerifier
{
    Task<TelegramBotVerificationResult> VerifyAsync(string token, CancellationToken ct = default);
}
