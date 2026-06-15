using MediaFlow.Application.Validation;
using MediaFlow.Domain.Entities;
using MediaFlow.Domain.Interfaces;

namespace MediaFlow.Application.UseCases;

public sealed class RegisterDeviceUseCase(IDeviceRepository repository)
{
    public async Task<RegisterDeviceResult> ExecuteAsync(
        DeviceProfileInput input,
        CancellationToken ct = default)
    {
        var errors = DeviceProfileValidator.Validate(input);
        if (errors.Count > 0)
            return new RegisterDeviceResult.ValidationFailed(errors);

        var profile = new DeviceProfile(
            Id: Guid.NewGuid().ToString(),
            Name: input.Name,
            SourceFolderPath: input.SourceFolderPath,
            BackupFolderPath: input.BackupFolderPath,
            NamingTemplate: input.NamingTemplate,
            TelegramBotToken: input.TelegramBotToken,
            TelegramChatId: input.TelegramChatId,
            FilesPerLoad: input.FilesPerLoad);

        await repository.SaveAsync(profile, ct);
        return new RegisterDeviceResult.Success(profile);
    }
}

public abstract record RegisterDeviceResult
{
    public sealed record Success(DeviceProfile Profile) : RegisterDeviceResult;
    public sealed record ValidationFailed(IReadOnlyList<string> Errors) : RegisterDeviceResult;
}
