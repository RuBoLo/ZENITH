using CommunityToolkit.Mvvm.ComponentModel;
using ZENITH.Preferences;
using ZENITH.ViewModels;

namespace ZENITH.Preferences.Categories.General;

public partial class GeneralPreferencesViewModel : ViewModelBase, IPreferencesCategoryViewModel
{
    private StartupWindowMode _savedStartupWindowMode;

    [ObservableProperty]
    private StartupWindowMode _startupWindowMode;

    public GeneralPreferencesViewModel(ApplicationPreferences preferences)
    {
        _savedStartupWindowMode = preferences.StartupWindowMode;
        _startupWindowMode = preferences.StartupWindowMode;
    }

    public StartupWindowModeOption[] StartupWindowModeOptions { get; } =
    [
        new(
            "Maximized",
            "Starts the main window maximized.",
            StartupWindowMode.Maximized),
        new(
            "Full screen",
            "Starts the main window in full screen mode.",
            StartupWindowMode.FullScreen)
    ];

    public bool HasChanges => StartupWindowMode != _savedStartupWindowMode;

    public StartupWindowModeOption? SelectedStartupWindowModeOption
    {
        get
        {
            foreach (StartupWindowModeOption option in StartupWindowModeOptions)
            {
                if (option.Value == StartupWindowMode)
                    return option;
            }

            return null;
        }
        set
        {
            if (value is not null)
                StartupWindowMode = value.Value;
        }
    }

    public bool StartMaximized
    {
        get => StartupWindowMode == StartupWindowMode.Maximized;
        set
        {
            if (value)
                StartupWindowMode = StartupWindowMode.Maximized;
        }
    }

    public bool StartFullScreen
    {
        get => StartupWindowMode == StartupWindowMode.FullScreen;
        set
        {
            if (value)
                StartupWindowMode = StartupWindowMode.FullScreen;
        }
    }

    public void ApplyTo(ApplicationPreferences preferences)
    {
        preferences.StartupWindowMode = StartupWindowMode;
    }

    public void AcceptChanges(ApplicationPreferences preferences)
    {
        _savedStartupWindowMode = preferences.StartupWindowMode;
        StartupWindowMode = preferences.StartupWindowMode;
        OnPropertyChanged(nameof(HasChanges));
    }

    partial void OnStartupWindowModeChanged(StartupWindowMode value)
    {
        OnPropertyChanged(nameof(StartMaximized));
        OnPropertyChanged(nameof(StartFullScreen));
        OnPropertyChanged(nameof(SelectedStartupWindowModeOption));
        OnPropertyChanged(nameof(HasChanges));
    }
}

public sealed record StartupWindowModeOption(
    string Label,
    string ToolTip,
    StartupWindowMode Value);
