using MediaFlow.Domain.Entities;
using ReactiveUI;

namespace MediaFlow.Presentation.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly DeviceListViewModel _deviceList;
    private readonly ProfileEditorViewModel _profileEditor;
    private ViewModelBase? _currentPage;

    public ViewModelBase? CurrentPage
    {
        get => _currentPage;
        set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }

    public MainWindowViewModel(DeviceListViewModel deviceList, ProfileEditorViewModel profileEditor)
    {
        _deviceList = deviceList;
        _profileEditor = profileEditor;

        deviceList.AddDeviceRequested += () => OpenEditor(null);
        deviceList.EditDeviceRequested += profile => OpenEditor(profile);

        profileEditor.SaveCompleted += OnEditorSaved;
        profileEditor.CancelRequested += NavigateToDeviceList;

        NavigateTo(deviceList);
    }

    private void OpenEditor(DeviceProfile? profile)
    {
        _profileEditor.Initialize(profile);
        NavigateTo(_profileEditor);
    }

    private void OnEditorSaved()
    {
        NavigateToDeviceList();
        _ = _deviceList.ReloadAsync();
    }

    public void NavigateTo(ViewModelBase page) => CurrentPage = page;
    public void NavigateToDeviceList() => NavigateTo(_deviceList);
}
