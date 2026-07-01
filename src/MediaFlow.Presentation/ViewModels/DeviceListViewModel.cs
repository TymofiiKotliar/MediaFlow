using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using MediaFlow.Application.UseCases;
using MediaFlow.Domain.Entities;
using MediaFlow.Presentation.Services;
using ReactiveUI;

namespace MediaFlow.Presentation.ViewModels;

public sealed class DeviceListViewModel : ViewModelBase
{
    private readonly GetAllDevicesUseCase _getAllDevices;
    private readonly DeleteDeviceUseCase _deleteDevice;
    private readonly IDialogService _dialogService;

    public ObservableCollection<DeviceCardViewModel> Devices { get; } = [];

    public string DeviceCountText =>
        $"{Devices.Count} device{(Devices.Count == 1 ? "" : "s")} registered";

    public ReactiveCommand<Unit, Unit> AddDeviceCommand { get; }

    public event Action? AddDeviceRequested;
    public event Action<DeviceProfile>? OpenDeviceRequested;
    public event Action<DeviceProfile>? EditDeviceRequested;

    public DeviceListViewModel(GetAllDevicesUseCase getAllDevices, DeleteDeviceUseCase deleteDevice, IDialogService dialogService)
    {
        _getAllDevices = getAllDevices;
        _deleteDevice = deleteDevice;
        _dialogService = dialogService;

        Devices.CollectionChanged += (_, _) => this.RaisePropertyChanged(nameof(DeviceCountText));
        AddDeviceCommand = ReactiveCommand.Create(() => AddDeviceRequested?.Invoke());

        _ = LoadAsync();
    }

    public async Task ReloadAsync()
    {
        Devices.Clear();
        var profiles = await _getAllDevices.ExecuteAsync();
        foreach (var p in profiles)
            Devices.Add(CreateCard(p));
    }

    private Task LoadAsync() => ReloadAsync();

    private DeviceCardViewModel CreateCard(DeviceProfile profile) => new(
        profile,
        onOpen:   () => OpenDeviceRequested?.Invoke(profile),
        onEdit:   () => EditDeviceRequested?.Invoke(profile),
        onDelete: () => _ = DeleteAsync(profile));

    private async Task DeleteAsync(DeviceProfile profile)
    {
        var confirmed = await _dialogService.ConfirmAsync($"This will permanently remove \"{profile.Name}\".");
        if (!confirmed) return;

        var result = await _deleteDevice.ExecuteAsync(profile.Id);
        if (result is DeleteDeviceResult.Success)
        {
            var card = Devices.FirstOrDefault(c => c.Profile.Id == profile.Id);
            if (card is not null)
                Devices.Remove(card);
        }
    }
}
