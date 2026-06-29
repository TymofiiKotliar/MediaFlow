using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using MediaFlow.Presentation.Views;

namespace MediaFlow.Presentation.Services;

public sealed class DialogService : IDialogService
{
    public Task<bool> ConfirmAsync(string message)
    {
        var mainWindow = (Avalonia.Application.Current!.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime)?.MainWindow;
        return new ConfirmDialog(message).ShowDialog<bool>(mainWindow!);
    }
}
