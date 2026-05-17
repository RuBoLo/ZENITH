using ZENITH.Preferences;
using ZENITH.ViewModels;

namespace ZENITH.Preferences.Categories.Appearance;

public partial class AppearancePreferencesViewModel : ViewModelBase, IPreferencesCategoryViewModel
{
    public bool HasChanges => false;

    public void ApplyTo(ApplicationPreferences preferences)
    {
    }

    public void AcceptChanges(ApplicationPreferences preferences)
    {
    }
}
