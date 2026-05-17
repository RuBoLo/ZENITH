namespace ZENITH.Preferences;

public interface IPreferencesService
{
    ApplicationPreferences Current { get; }

    void Save(ApplicationPreferences preferences);
}
