using MediaFlow.Application.Validation;
using MediaFlow.Domain.Entities;
using MediaFlow.Domain.Enums;

namespace MediaFlow.Application.Tests.Fixtures;

internal static class DeviceFixtures
{
    internal static DeviceProfileInput ValidInput(
        string name = "Canon R5",
        string sourcePath = @"C:\Source",
        string backupPath = @"C:\Backup",
        string botToken = "123456:ABC-token",
        string chatId = "987654321",
        int filesPerLoad = 100) =>
        new(name, sourcePath, backupPath, [], botToken, chatId, filesPerLoad);

    internal static DeviceProfile ExistingProfile(string id = "test-id") =>
        new(
            Id: id,
            Name: "Old Name",
            SourceFolderPath: @"C:\OldSource",
            BackupFolderPath: @"C:\OldBackup",
            NamingTemplate: [],
            TelegramBotToken: "old-token",
            TelegramChatId: "111111111",
            FilesPerLoad: 50);

    internal static FileContext ImageFile(
        string name = "photo.jpg",
        MediaAction actions = MediaAction.None,
        string? exifCaptureDate = null) =>
        new(name, $@"C:\Source\{name}", $@"C:\Temp\{name}", FileType.Image, actions, exifCaptureDate);

    internal static FileContext VideoFile(
        string name = "clip.mp4",
        MediaAction actions = MediaAction.None) =>
        new(name, $@"C:\Source\{name}", $@"C:\Temp\{name}", FileType.Video, actions, null);
}
