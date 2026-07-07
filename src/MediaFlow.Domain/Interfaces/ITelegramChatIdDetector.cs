using MediaFlow.Domain.ValueObjects;

namespace MediaFlow.Domain.Interfaces;

public interface ITelegramChatIdDetector
{
    Task<TelegramChatIdDetectionResult> DetectAsync(string token, CancellationToken ct = default);
}
