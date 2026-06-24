using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Threading.Tasks;
using MediaFlow.Application.UseCases;
using MediaFlow.Domain.Entities;
using ReactiveUI;

namespace MediaFlow.Presentation.ViewModels;

public sealed class DeviceListViewModel : ViewModelBase
{
    private readonly GetAllDevicesUseCase _getAllDevices;
    private readonly DeleteDeviceUseCase _deleteDevice;

    public ObservableCollection<DeviceCardViewModel> Devices { get; } = [];

    public string DeviceCountText =>
        $"{Devices.Count} device{(Devices.Count == 1 ? "" : "s")} registered";

    public ReactiveCommand<Unit, Unit> AddDeviceCommand { get; }

    public event Action? AddDeviceRequested;
    public event Action<DeviceProfile>? OpenDeviceRequested;
    public event Action<DeviceProfile>? EditDeviceRequested;

    public DeviceListViewModel(GetAllDevicesUseCase getAllDevices, DeleteDeviceUseCase deleteDevice)
    {
        _getAllDevices = getAllDevices;
        _deleteDevice = deleteDevice;

        Devices.CollectionChanged += (_, _) => this.RaisePropertyChanged(nameof(DeviceCountText));
        AddDeviceCommand = ReactiveCommand.Create(() => AddDeviceRequested?.Invoke());

        _ = LoadAsync();
    }

    private async Task LoadAsync()
    {
        var profiles = await _getAllDevices.ExecuteAsync();
        foreach (var p in profiles)
            Devices.Add(CreateCard(p));
    }

    private DeviceCardViewModel CreateCard(DeviceProfile profile) => new(
        profile,
        onOpen:   () => OpenDeviceRequested?.Invoke(profile),
        onEdit:   () => EditDeviceRequested?.Invoke(profile),
        onDelete: () => _ = DeleteAsync(profile));

    private async Task DeleteAsync(DeviceProfile profile)
    {
        var result = await _deleteDevice.ExecuteAsync(profile.Id);
        if (result is DeleteDeviceResult.Success)
        {
            var card = Devices.FirstOrDefault(c => c.Profile.Id == profile.Id);
            if (card is not null)
                Devices.Remove(card);
        }
    }
}
