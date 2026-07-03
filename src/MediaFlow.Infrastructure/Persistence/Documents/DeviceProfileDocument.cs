namespace MediaFlow.Infrastructure.Persistence.Documents;

internal sealed class DeviceProfileDocument
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string SourceFolderPath { get; set; } = "";
    public string BackupFolderPath { get; set; } = "";
    public List<NamingTokenDocument> NamingTemplate { get; set; } = [];
    public string TelegramBotToken { get; set; } = "";
    public string TelegramChatId { get; set; } = "";
    public int FilesPerLoad { get; set; }
    public string? ProfilePicturePath { get; set; }
    public string ProfilePictureFitMode { get; set; } = "Crop";
}
