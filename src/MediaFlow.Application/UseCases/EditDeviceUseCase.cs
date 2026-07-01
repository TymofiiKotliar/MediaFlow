using MediaFlow.Application.Validation;
using MediaFlow.Domain.Entities;
using MediaFlow.Domain.Interfaces;

namespace MediaFlow.Application.UseCases;

public sealed class EditDeviceUseCase(IDeviceRepository repository)
{
    public async Task<EditDeviceResult> ExecuteAsync(
        string id,
        DeviceProfileInput input,
        CancellationToken ct = default)
    {
        var existing = await repository.GetByIdAsync(id, ct);
        if (existing is null)
            return new EditDeviceResult.NotFound(id);

        var errors = DeviceProfileValidator.Validate(input);
        if (errors.Count > 0)
            return new EditDeviceResult.ValidationFailed(errors);

        var updated = existing with
        {
            Name = input.Name,
            SourceFolderPath = input.SourceFolderPath,
            BackupFolderPath = input.BackupFolderPath,
            NamingTemplate = input.NamingTemplate,
            TelegramBotToken = input.TelegramBotToken,
            TelegramChatId = input.TelegramChatId,
            FilesPerLoad = input.FilesPerLoad,
            ProfilePicturePath = input.ProfilePicturePath,
            ProfilePictureFitMode = input.ProfilePictureFitMode
        };

        await repository.SaveAsync(updated, ct);
        return new EditDeviceResult.Success(updated);
    }
}

public abstract record EditDeviceResult
{
    public sealed record Success(DeviceProfile Profile) : EditDeviceResult;
    public sealed record NotFound(string Id) : EditDeviceResult;
    public sealed record ValidationFailed(IReadOnlyList<string> Errors) : EditDeviceResult;
}
