using System.ComponentModel;

namespace ZENITH.Preferences;

public interface IPreferencesCategoryViewModel : INotifyPropertyChanged
{
    bool HasChanges { get; }

    void ApplyTo(ApplicationPreferences preferences);

    void AcceptChanges(ApplicationPreferences preferences);
}
