namespace MediaFlow.Domain.ValueObjects;

public abstract record TelegramChatIdDetectionResult;

public sealed record TelegramChatIdDetected(string ChatId, string? ChatLabel) : TelegramChatIdDetectionResult;
public sealed record TelegramChatIdDetectionFailed(string Reason) : TelegramChatIdDetectionResult;
