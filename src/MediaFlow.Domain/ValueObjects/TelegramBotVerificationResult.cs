namespace MediaFlow.Domain.ValueObjects;

public abstract record TelegramBotVerificationResult;

public sealed record TelegramBotVerified(string BotUsername) : TelegramBotVerificationResult;
public sealed record TelegramBotVerificationFailed(string Reason) : TelegramBotVerificationResult;
