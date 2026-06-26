using System.Reactive;
using MediaFlow.Domain.Entities;
using ReactiveUI;

namespace MediaFlow.Presentation.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private readonly DeviceListViewModel _deviceList;
    private readonly ProfileEditorViewModel _profileEditor;
    private readonly MediaBrowserViewModel _mediaBrowser;
    private ViewModelBase? _currentPage;

    public ViewModelBase? CurrentPage
    {
        get => _currentPage;
        set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }

    public MainWindowViewModel(
        DeviceListViewModel deviceList,
        ProfileEditorViewModel profileEditor,
        MediaBrowserViewModel mediaBrowser)
    {
        _deviceList = deviceList;
        _profileEditor = profileEditor;
        _mediaBrowser = mediaBrowser;

        deviceList.AddDeviceRequested  += () => OpenEditor(null);
        deviceList.EditDeviceRequested += profile => OpenEditor(profile);
        deviceList.OpenDeviceRequested += OpenBrowser;

        profileEditor.SaveCompleted  += OnEditorSaved;
        profileEditor.CancelRequested += NavigateToDeviceList;

        mediaBrowser.BackRequested += NavigateToDeviceList;

        NavigateToDeviceListCommand = ReactiveCommand.Create(NavigateToDeviceList);

        NavigateTo(deviceList);
    }

    private void OpenEditor(DeviceProfile? profile)
    {
        _profileEditor.Initialize(profile);
        NavigateTo(_profileEditor);
    }

    private void OpenBrowser(DeviceProfile profile)
    {
        _mediaBrowser.Initialize(profile);
        NavigateTo(_mediaBrowser);
    }

    private void OnEditorSaved()
    {
        NavigateToDeviceList();
        _ = _deviceList.ReloadAsync();
    }

    public ReactiveCommand<Unit, Unit> NavigateToDeviceListCommand { get; }

    public void NavigateTo(ViewModelBase page) => CurrentPage = page;
    public void NavigateToDeviceList() => NavigateTo(_deviceList);
}
