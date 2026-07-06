using System;
using System.IO;
using System.Reactive;
using System.Threading.Tasks;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using MediaFlow.Domain.Entities;
using MediaFlow.Domain.Enums;
using ReactiveUI;

namespace MediaFlow.Presentation.ViewModels;

public sealed class DeviceCardViewModel : ViewModelBase
{
    private Bitmap? _profilePicture;

    public DeviceProfile Profile { get; }
    public bool IsSourceAccessible { get; }
    public bool IsBackupAccessible { get; }
    public bool IsStatusOk => IsSourceAccessible && IsBackupAccessible;
    public bool IsStatusWarn => IsSourceAccessible && !IsBackupAccessible;
    public string FilesPerLoadText => $"{Profile.FilesPerLoad} files / load";

    public Stretch PictureStretch => Profile.ProfilePictureFitMode switch
    {
        ProfilePictureFitMode.Fit => Stretch.Uniform,
        ProfilePictureFitMode.Stretch => Stretch.Fill,
        _ => Stretch.UniformToFill
    };

    public Bitmap? ProfilePicture
    {
        get => _profilePicture;
        private set
        {
            this.RaiseAndSetIfChanged(ref _profilePicture, value);
            this.RaisePropertyChanged(nameof(HasProfilePicture));
        }
    }

    public bool HasProfilePicture => _profilePicture is not null;

    public ReactiveCommand<Unit, Unit> OpenCommand { get; }
    public ReactiveCommand<Unit, Unit> EditCommand { get; }
    public ReactiveCommand<Unit, Unit> DeleteCommand { get; }

    public DeviceCardViewModel(DeviceProfile profile, Action onOpen, Action onEdit, Action onDelete)
    {
        Profile = profile;
        IsSourceAccessible = Directory.Exists(profile.SourceFolderPath);
        IsBackupAccessible = Directory.Exists(profile.BackupFolderPath);
        OpenCommand = ReactiveCommand.Create(onOpen);
        EditCommand = ReactiveCommand.Create(onEdit);
        DeleteCommand = ReactiveCommand.Create(onDelete);

        _ = LoadProfilePictureAsync();
    }

    private async Task LoadProfilePictureAsync()
    {
        if (Profile.ProfilePicturePath is null) return;

        try
        {
            var bytes = await File.ReadAllBytesAsync(Profile.ProfilePicturePath);
            using var ms = new MemoryStream(bytes);
            ProfilePicture = new Bitmap(ms);
        }
        catch
        {
            // Picture stays null; the card shows the default placeholder
        }
    }
}
