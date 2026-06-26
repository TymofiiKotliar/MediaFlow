using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using MediaFlow.Domain.Entities;
using MediaFlow.Domain.Enums;
using MediaFlow.Domain.Interfaces;
using ReactiveUI;

namespace MediaFlow.Presentation.ViewModels;

public sealed class FileRowViewModel : ViewModelBase
{
    private Bitmap? _thumbnail;
    private bool _shouldRotateLeft;
    private bool _shouldRotateRight;
    private bool _shouldFlip;
    private bool _shouldBackup;
    private bool _shouldSendToTelegram;
    private bool _shouldDelete;

    public FileContext Context { get; }
    public string FileName { get; }
    public long FileSize { get; }
    public string FileSizeText { get; }
    public string CaptureDate { get; }
    public string MetaText { get; }
    public string FileExtension { get; }

    public Bitmap? Thumbnail
    {
        get => _thumbnail;
        private set
        {
            this.RaiseAndSetIfChanged(ref _thumbnail, value);
            this.RaisePropertyChanged(nameof(HasThumbnail));
        }
    }

    public bool HasThumbnail => _thumbnail is not null;

    public bool ShouldRotateLeft
    {
        get => _shouldRotateLeft;
        set { this.RaiseAndSetIfChanged(ref _shouldRotateLeft, value); NotifyActions(); }
    }

    public bool ShouldRotateRight
    {
        get => _shouldRotateRight;
        set { this.RaiseAndSetIfChanged(ref _shouldRotateRight, value); NotifyActions(); }
    }

    public bool ShouldFlip
    {
        get => _shouldFlip;
        set { this.RaiseAndSetIfChanged(ref _shouldFlip, value); NotifyActions(); }
    }

    public bool ShouldBackup
    {
        get => _shouldBackup;
        set { this.RaiseAndSetIfChanged(ref _shouldBackup, value); NotifyActions(); }
    }

    public bool ShouldSendToTelegram
    {
        get => _shouldSendToTelegram;
        set { this.RaiseAndSetIfChanged(ref _shouldSendToTelegram, value); NotifyActions(); }
    }

    public bool ShouldDelete
    {
        get => _shouldDelete;
        set { this.RaiseAndSetIfChanged(ref _shouldDelete, value); NotifyActions(); }
    }

    public bool HasAnyAction =>
        _shouldRotateLeft || _shouldRotateRight || _shouldFlip ||
        _shouldBackup || _shouldSendToTelegram || _shouldDelete;

    public MediaAction AssignedActions =>
        (_shouldRotateLeft     ? MediaAction.RotateLeft     : MediaAction.None) |
        (_shouldRotateRight    ? MediaAction.RotateRight    : MediaAction.None) |
        (_shouldFlip           ? MediaAction.Flip180        : MediaAction.None) |
        (_shouldBackup         ? MediaAction.SaveToBackup   : MediaAction.None) |
        (_shouldSendToTelegram ? MediaAction.SendToTelegram : MediaAction.None) |
        (_shouldDelete         ? MediaAction.DeleteAfter    : MediaAction.None);

    public FileRowViewModel(FileContext context, IThumbnailService thumbnails, CancellationToken ct)
    {
        Context = context;
        FileName = context.OriginalName;
        FileExtension = Path.GetExtension(context.OriginalName).TrimStart('.').ToUpperInvariant();

        var info = new FileInfo(context.SourcePath);
        FileSize = info.Exists ? info.Length : 0;
        FileSizeText = FormatSize(FileSize);
        CaptureDate = context.ExifCaptureDate is not null
            ? FormatExifDate(context.ExifCaptureDate)
            : "";
        MetaText = CaptureDate.Length > 0
            ? $"{FileSizeText} · {CaptureDate}"
            : FileSizeText;

        _ = LoadThumbnailAsync(thumbnails, ct);
    }

    private void NotifyActions() => this.RaisePropertyChanged(nameof(HasAnyAction));

    private async Task LoadThumbnailAsync(IThumbnailService thumbnails, CancellationToken ct)
    {
        try
        {
            var bytes = await thumbnails.GenerateAsync(Context.TempPath, maxSize: 128, ct);
            using var ms = new MemoryStream(bytes);
            Thumbnail = new Bitmap(ms);
        }
        catch
        {
            // Thumbnail stays null; the view shows a placeholder
        }
    }

    private static string FormatSize(long bytes)
    {
        if (bytes >= 1_048_576) return $"{bytes / 1_048_576.0:F1} MB";
        if (bytes >= 1_024)     return $"{bytes / 1_024.0:F1} KB";
        return $"{bytes} B";
    }

    private static string FormatExifDate(string exif)
    {
        // EXIF: "YYYY:MM:DD HH:MM:SS"
        if (DateTime.TryParseExact(exif, "yyyy:MM:dd HH:mm:ss",
                CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
            return dt.ToString("MMM dd yyyy");
        return exif;
    }
}
