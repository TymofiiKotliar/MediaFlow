using System;
using System.IO;
using System.Reactive;
using MediaFlow.Domain.Entities;
using ReactiveUI;

namespace MediaFlow.Presentation.ViewModels;

public sealed class DeviceCardViewModel : ViewModelBase
{
    public DeviceProfile Profile { get; }
    public bool IsSourceAccessible { get; }
    public bool IsBackupAccessible { get; }
    public bool IsStatusOk => IsSourceAccessible && IsBackupAccessible;
    public bool IsStatusWarn => IsSourceAccessible && !IsBackupAccessible;
    public string FilesPerLoadText => $"{Profile.FilesPerLoad} files / load";

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
    }
}
