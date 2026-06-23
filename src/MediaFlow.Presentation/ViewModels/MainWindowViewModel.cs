using ReactiveUI;

namespace MediaFlow.Presentation.ViewModels;

public sealed class MainWindowViewModel : ViewModelBase
{
    private ViewModelBase? _currentPage;

    public ViewModelBase? CurrentPage
    {
        get => _currentPage;
        set => this.RaiseAndSetIfChanged(ref _currentPage, value);
    }

    public void NavigateTo(ViewModelBase page) => CurrentPage = page;
}
