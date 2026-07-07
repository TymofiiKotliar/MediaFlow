using Telegram.Bot;
using Telegram.Bot.Exceptions;
using MediaFlow.Domain.Interfaces;
using MediaFlow.Domain.ValueObjects;

namespace MediaFlow.Infrastructure.Telegram;

public sealed class TelegramBotVerifierAdapter : ITelegramBotVerifier
{
    private readonly HttpClient _httpClient;

    public TelegramBotVerifierAdapter(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public async Task<TelegramBotVerificationResult> VerifyAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            return new TelegramBotVerificationFailed("Token is empty");

        try
        {
            var bot = new TelegramBotClient(token, _httpClient);
            var me = await bot.GetMe(ct);
            return new TelegramBotVerified(me.Username ?? me.FirstName);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (ArgumentException)
        {
            // TelegramBotClient validates the token shape (digits:hash) in its constructor
            // and throws synchronously — routine while the user is still mid-typing.
            return new TelegramBotVerificationFailed("Invalid token format");
        }
        catch (ApiRequestException ex)
        {
            return new TelegramBotVerificationFailed($"Invalid token: {ex.Message}");
        }
        catch (Exception) when (ct.IsCancellationRequested)
        {
            // A newer keystroke superseded this check mid-request (observed: mid the TLS
            // renegotiation Telegram's server does), which can surface as a raw connection
            // exception instead of a clean OperationCanceledException. Treat it as what it
            // actually is — a superseded check, not a real connectivity failure.
            throw new OperationCanceledException(ct);
        }
        catch (Exception ex)
        {
            return new TelegramBotVerificationFailed($"Couldn't reach Telegram: {ex.Message}");
        }
    }
}
