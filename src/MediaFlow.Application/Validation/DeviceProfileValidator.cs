namespace MediaFlow.Application.Validation;

internal static class DeviceProfileValidator
{
    private static readonly char[] InvalidPathChars = Path.GetInvalidPathChars();

    internal static IReadOnlyList<string> Validate(DeviceProfileInput input)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(input.Name))
            errors.Add("Device name is required.");

        ValidatePath(input.SourceFolderPath, "Source folder", errors);
        ValidatePath(input.BackupFolderPath, "Backup folder", errors);

        if (string.IsNullOrWhiteSpace(input.TelegramBotToken))
            errors.Add("Telegram bot token is required.");

        if (string.IsNullOrWhiteSpace(input.TelegramChatId))
            errors.Add("Telegram chat ID is required.");
        else if (!input.TelegramChatId.TrimStart('-').All(char.IsDigit))
            errors.Add("Telegram chat ID must contain only digits (and an optional leading minus for group chats).");

        if (input.FilesPerLoad is < 10 or > 1000)
            errors.Add("Files per load must be between 10 and 1000.");

        return errors;
    }

    private static void ValidatePath(string path, string fieldName, List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            errors.Add($"{fieldName} is required.");
            return;
        }

        if (path.IndexOfAny(InvalidPathChars) >= 0)
        {
            errors.Add($"{fieldName} contains invalid characters.");
            return;
        }

        if (!Path.IsPathRooted(path))
            errors.Add($"{fieldName} must be an absolute path.");
    }
}
