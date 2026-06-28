using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using MediaFlow.Presentation.ViewModels;

namespace MediaFlow.Presentation.Views;

public partial class ProfileEditorView : UserControl
{
    public ProfileEditorView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is ProfileEditorViewModel vm)
            vm.BrowseFolderFunc = BrowseFolderAsync;
    }

    private void OnChipAreaPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is ProfileEditorViewModel vm)
            vm.MoveCursorToEnd();
    }

    private async Task<string?> BrowseFolderAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return null;

        var results = await topLevel.StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions { AllowMultiple = false });

        return results.Count > 0 ? results[0].Path.LocalPath : null;
    }
}
