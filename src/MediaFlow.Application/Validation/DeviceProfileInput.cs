using MediaFlow.Domain.ValueObjects;

namespace MediaFlow.Application.Validation;

public sealed record DeviceProfileInput(
    string Name,
    string SourceFolderPath,
    string BackupFolderPath,
    IReadOnlyList<NamingToken> NamingTemplate,
    string TelegramBotToken,
    string TelegramChatId,
    int FilesPerLoad
);
