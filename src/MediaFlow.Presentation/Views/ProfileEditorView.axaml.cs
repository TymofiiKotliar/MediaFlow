using System;
using System.IO;
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
        {
            vm.BrowseFolderFunc = BrowseFolderAsync;
            vm.BrowseImageFunc = BrowseImageAsync;
        }
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

    private async Task<(byte[] Bytes, string Extension)?> BrowseImageAsync()
    {
        var topLevel = TopLevel.GetTopLevel(this);
        if (topLevel is null) return null;

        var results = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            AllowMultiple = false,
            FileTypeFilter = [new FilePickerFileType("Images") { Patterns = ["*.png", "*.jpg", "*.jpeg"] }]
        });

        if (results.Count == 0) return null;

        await using var stream = await results[0].OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);

        var extension = Path.GetExtension(results[0].Name);
        return (ms.ToArray(), string.IsNullOrEmpty(extension) ? ".png" : extension);
    }
}
