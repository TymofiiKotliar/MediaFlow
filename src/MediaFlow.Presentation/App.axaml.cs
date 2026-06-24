using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MediaFlow.Infrastructure;
using MediaFlow.Presentation.ViewModels;
using MediaFlow.Presentation.Views;
using Microsoft.Extensions.DependencyInjection;

namespace MediaFlow.Presentation;

public partial class App : Avalonia.Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        var services = new ServiceCollection();
        services.AddMediaFlowServices();
        services.AddTransient<DeviceListViewModel>();
        services.AddTransient<ProfileEditorViewModel>();
        services.AddTransient<MainWindowViewModel>();
        services.AddTransient<MainWindow>();
        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            desktop.MainWindow = Services.GetRequiredService<MainWindow>();

        base.OnFrameworkInitializationCompleted();
    }
}
