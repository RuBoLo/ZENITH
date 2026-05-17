using ZENITH.Preferences.Categories.General;

namespace ZENITH.Preferences;

public sealed class ApplicationPreferences
{
    public StartupWindowMode StartupWindowMode { get; set; } = StartupWindowMode.Maximized;

    public ApplicationPreferences Copy()
    {
        return new ApplicationPreferences
        {
            StartupWindowMode = StartupWindowMode
        };
    }
}
