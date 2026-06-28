using Avalonia.Controls;
using MediaFlow.Presentation.ViewModels;

namespace MediaFlow.Presentation.Views;

public partial class MainWindow : Window
{
    public MainWindow(MainWindowViewModel vm)
    {
        DataContext = vm;
        InitializeComponent();
    }
}
