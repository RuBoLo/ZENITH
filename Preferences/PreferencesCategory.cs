namespace ZENITH.Preferences;

public class PreferencesCategory(string title, IPreferencesCategoryViewModel viewModel)
{
    public string Title { get; } = title;

    public IPreferencesCategoryViewModel ViewModel { get; } = viewModel;
}
