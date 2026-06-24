using ReactiveUI;

namespace MediaFlow.Presentation.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly DeviceListViewModel _deviceList;
    private ViewModelBase? _currentPage;

    public ViewModelBase? CurrentPage
    {
        get => _currentPage;
        set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }

    public MainWindowViewModel(DeviceListViewModel deviceList)
    {
        _deviceList = deviceList;
        // Navigation hooks for future screens (media browser, profile editor) wired here
        NavigateTo(deviceList);
    }

    public void NavigateTo(ViewModelBase page) => CurrentPage = page;
    public void NavigateToDeviceList() => NavigateTo(_deviceList);
}
