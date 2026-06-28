using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using MediaFlow.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MediaFlow.Presentation;

[RequiresUnreferencedCode("ViewLocator uses reflection and may not work after trimming.")]
public sealed class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null)
            return null;

        var viewTypeName = param.GetType().FullName!
            .Replace("ViewModel", "View", StringComparison.Ordinal);

        var viewType = Type.GetType(viewTypeName);
        if (viewType is null)
            return new TextBlock { Text = $"View not found: {viewTypeName}" };

        return (Control)(App.Services.GetService(viewType)
            ?? Activator.CreateInstance(viewType)!);
    }

    public bool Match(object? data) => data is ViewModelBase;
}
