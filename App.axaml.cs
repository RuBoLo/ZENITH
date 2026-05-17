using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.DependencyInjection;
using System;
using ZENITH.Preferences;
using ZENITH.Services.Console;
using ZENITH.Services.FilePicker;
using ZENITH.Services.Workspace;
using ZENITH.ViewModels.Shell;
using ZENITH.Views.Shell;

namespace ZENITH;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        ServiceCollection services = new();

        services.AddSingleton<IConsoleService, ConsoleService>();
        services.AddSingleton<IPreferencesService, PreferencesService>();
        services.AddSingleton<IFilePickerService, FilePickerService>();
        services.AddSingleton<IImageWorkspaceService, ImageWorkspaceService>();

        Services = services.BuildServiceProvider();

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindowView
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}
