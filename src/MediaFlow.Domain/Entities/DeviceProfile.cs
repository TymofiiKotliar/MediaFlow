using MediaFlow.Domain.Enums;
using MediaFlow.Domain.ValueObjects;

namespace MediaFlow.Domain.Entities;

public sealed record DeviceProfile(
    string Id,
    string Name,
    string SourceFolderPath,
    string BackupFolderPath,
    IReadOnlyList<NamingToken> NamingTemplate,
    string TelegramBotToken,
    string TelegramChatId,
    int FilesPerLoad,
    string? ProfilePicturePath = null,
    ProfilePictureFitMode ProfilePictureFitMode = ProfilePictureFitMode.Crop
);
