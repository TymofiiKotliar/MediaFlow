using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;
using MediaFlow.Domain.Entities;
using MediaFlow.Domain.Enums;
using MediaFlow.Domain.Interfaces;
using MediaFlow.Domain.ValueObjects;

namespace MediaFlow.Infrastructure.Telegram;

public sealed class TelegramAdapter : ITelegramStage
{
    private readonly HttpClient _httpClient;

    public TelegramAdapter(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<(PipelineContext Context, PipelineStageResult Result)> ExecuteAsync(
        PipelineContext context, CancellationToken ct)
    {
        if (!context.File.AssignedActions.HasFlag(MediaAction.SendToTelegram))
            return (context, new Skipped("SendToTelegram not assigned"));

        if (string.IsNullOrWhiteSpace(context.Device.TelegramBotToken))
            return (context, new Failed("Telegram bot token is not configured"));

        if (!long.TryParse(context.Device.TelegramChatId, out var chatId))
            return (context, new Failed($"Invalid Telegram chat ID: '{context.Device.TelegramChatId}'"));

        var bot = new TelegramBotClient(context.Device.TelegramBotToken, _httpClient);

        try
        {
            await using var stream = File.OpenRead(context.File.TempPath);
            var fileName = context.File.OriginalName;

            if (context.File.Type == FileType.Image)
                await bot.SendPhoto(chatId, InputFile.FromStream(stream, fileName), cancellationToken: ct);
            else
                await bot.SendVideo(chatId, InputFile.FromStream(stream, fileName), cancellationToken: ct);

            return (context, new Success());
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ApiRequestException ex)
        {
            return (context, new Failed($"Telegram API error {ex.ErrorCode}: {ex.Message}"));
        }
        catch (Exception ex)
        {
            return (context, new Failed($"Telegram send failed: {ex.Message}"));
        }
    }
}
