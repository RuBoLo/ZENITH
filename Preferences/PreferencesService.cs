using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZENITH.Preferences;

public sealed class PreferencesService : IPreferencesService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters =
        {
            new JsonStringEnumConverter()
        }
    };

    private static readonly string PreferencesPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ZENITH",
        "preferences.json");

    public ApplicationPreferences Current { get; private set; } = Load();

    public void Save(ApplicationPreferences preferences)
    {
        Current = preferences.Copy();

        string? directory = Path.GetDirectoryName(PreferencesPath);
        if (!string.IsNullOrWhiteSpace(directory))
            Directory.CreateDirectory(directory);

        string json = JsonSerializer.Serialize(Current, JsonOptions);
        File.WriteAllText(PreferencesPath, json);
    }

    private static ApplicationPreferences Load()
    {
        if (!File.Exists(PreferencesPath)) return new ApplicationPreferences();

        try
        {
            string json = File.ReadAllText(PreferencesPath);
            ApplicationPreferences? preferences = JsonSerializer.Deserialize<ApplicationPreferences>(json, JsonOptions);

            return preferences is not null && Enum.IsDefined(preferences.StartupWindowMode)
                ? preferences
                : new ApplicationPreferences();
        }
        catch
        {
            return new ApplicationPreferences();
        }
    }
}