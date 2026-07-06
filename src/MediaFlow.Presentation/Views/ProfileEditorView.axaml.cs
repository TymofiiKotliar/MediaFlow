using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using MediaFlow.Presentation.ViewModels;

namespace MediaFlow.Presentation.Views;

public partial class ProfileEditorView : UserControl
{
    public ProfileEditorView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;

        // TextBox's watermark is only hidden while Text is non-empty; there's no built-in
        // way to also hide it while the box has focus but is still empty, so we toggle
        // Watermark itself and stash the original text on the Tag property.
        AddHandler(InputElement.GotFocusEvent, OnTextBoxGotFocus, RoutingStrategies.Bubble);
        AddHandler(InputElement.LostFocusEvent, OnTextBoxLostFocus, RoutingStrategies.Bubble);
    }

    private void OnTextBoxGotFocus(object? sender, GotFocusEventArgs e)
    {
        if (e.Source is TextBox textBox && !string.IsNullOrEmpty(textBox.Watermark))
        {
            textBox.Tag = textBox.Watermark;
            textBox.Watermark = null;
        }
    }

    private void OnTextBoxLostFocus(object? sender, RoutedEventArgs e)
    {
        if (e.Source is TextBox textBox && textBox.Tag is string watermark)
        {
            textBox.Watermark = watermark;
            textBox.Tag = null;
        }
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
