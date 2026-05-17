using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using ZENITH.Preferences;
using ZENITH.Preferences.Categories.General;
using ZENITH.Rendering.BitmapDecoding;
using ZENITH.Services.Console;
using ZENITH.Services.FilePicker;
using ZENITH.Services.Workspace;
using ZENITH.Views.Dialogs;
using ZENITH.Views.Utilities;
using ZENITH.ViewModels.Shell;

namespace ZENITH.Views.Shell;

public partial class MainWindowView : Window
{
    public MainWindowView()
    {
        InitializeComponent();

        PropertyChanged += OnWindowPropertyChanged;

        ApplyStartupWindowState();
    }

    private void DragBar_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed && WindowState != WindowState.FullScreen) BeginMoveDrag(e);
    }

    private void Minimise_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Maximise_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.Maximized)
        {
            WindowState = WindowState.Normal;
        }
        else
        {
            WindowState = WindowState.Maximized;
        }
    }

    private void FullScreen_Click(object sender, RoutedEventArgs e)
    {
        if (WindowState == WindowState.FullScreen)
        {
            WindowState = WindowState.Maximized;
        }
        else
        {
            // Maximizing before going full screen as it looks smoother
            WindowState = WindowState.Maximized;
            WindowState = WindowState.FullScreen;
        }
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnWindowPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == WindowStateProperty)
        {
            MaximiseMenuItem.Header = (WindowState == WindowState.Maximized)
                ? "Restore"
                : "Maximise";

            RootBorder.BorderThickness = (WindowState == WindowState.Maximized || WindowState == WindowState.FullScreen)
                ? new Thickness(0)
                : new Thickness(1);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);

        switch (e.Key)
        {
            case Key.F11:
                FullScreen_Click(this, e);
                break;
        }
    }

    private void ApplyStartupWindowState()
    {
        IPreferencesService preferencesService = App.Services.GetRequiredService<IPreferencesService>();

        if (preferencesService.Current.StartupWindowMode == StartupWindowMode.FullScreen)
        {
            WindowState = WindowState.Maximized;
            WindowState = WindowState.FullScreen;
            return;
        }

        WindowState = WindowState.Maximized;
    }

    private async void Open_Click(object sender, RoutedEventArgs e)
    {
        await OpenFilesFromPickerAsync();
    }

    private async void Workspace_DoubleTapped(object? sender, TappedEventArgs e)
    {
        await OpenFilesFromPickerAsync();
    }

    private async Task OpenFilesFromPickerAsync()
    {
        IFilePickerService filePicker = App.Services.GetRequiredService<IFilePickerService>();

        System.Collections.Generic.IReadOnlyList<string> paths = await filePicker.PickFilesAsync(
            this,
            "Open Images",
            allowMultiple: true,
            filter: BitmapFactory.IsSupported);

        if (paths.Count == 0) return;

        IImageWorkspaceService imageWorkspace = App.Services.GetRequiredService<IImageWorkspaceService>();

        await imageWorkspace.OpenFilesAsync(paths);
    }

    // Opens the preferences dialog
    private void Preferences_Click(object sender, RoutedEventArgs e)
    {
        PreferencesWindowView preferencesWindow = new();
        preferencesWindow.ShowDialog(this);
    }

    private void About_Click(object sender, RoutedEventArgs e)
    {
        AboutWindowView about = new();
        about.ShowDialog(this);

    }

    private void AboutAvalonia_Click(object sender, RoutedEventArgs e)
    {
        AboutAvaloniaWindowView aboutAvalonia = new();
        aboutAvalonia.ShowDialog(this);
    }


    // Utilities handling

    private void DiffractionSamplingUtility_Click(object sender, RoutedEventArgs e)
    {
        DiffractionSamplingCalculatorView diffractionSampling = new();
        diffractionSampling.Show(this);
    }

    private void IntegrationOperator_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
            viewModel.OpenIntegrationOperator();
    }
}