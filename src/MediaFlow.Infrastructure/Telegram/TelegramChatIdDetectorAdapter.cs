using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using MediaFlow.Domain.Interfaces;
using MediaFlow.Domain.ValueObjects;

namespace MediaFlow.Infrastructure.Telegram;

public sealed class TelegramChatIdDetectorAdapter : ITelegramChatIdDetector
{
    private readonly HttpClient _httpClient;

    public TelegramChatIdDetectorAdapter(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TelegramChatIdDetectionResult> DetectAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return new TelegramChatIdDetectionFailed("Token is empty");

        try
        {
            var bot = new TelegramBotClient(token, _httpClient);
            var updates = await bot.GetUpdates(limit: 100, cancellationToken: ct);

            var chat = updates
                .Reverse()
                .Select(GetChat)
                .FirstOrDefault(c => c is not null);

            if (chat is null)
                return new TelegramChatIdDetectionFailed(
                    "No messages found — send your bot a message, then try again");

            return new TelegramChatIdDetected(chat.Id.ToString(), DescribeChat(chat));
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ArgumentException)
        {
            return new TelegramChatIdDetectionFailed("Invalid token format");
        }
        catch (ApiRequestException ex)
        {
            return new TelegramChatIdDetectionFailed($"Invalid token: {ex.Message}");
        }
        catch (Exception) when (ct.IsCancellationRequested)
        {
            throw new OperationCanceledException(ct);
        }
        catch (Exception ex)
        {
            return new TelegramChatIdDetectionFailed($"Couldn't reach Telegram: {ex.Message}");
        }
    }

    private static Chat? GetChat(Update update) =>
        update.Message?.Chat
        ?? update.EditedMessage?.Chat
        ?? update.ChannelPost?.Chat
        ?? update.MyChatMember?.Chat;

    private static string DescribeChat(Chat chat) =>
        chat.Title ?? chat.Username ?? chat.FirstName ?? chat.Id.ToString();
}
