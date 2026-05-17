using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using ZENITH.Preferences.Categories.Appearance;
using ZENITH.Preferences.Categories.General;
using ZENITH.ViewModels;

namespace ZENITH.Preferences;

public partial class PreferencesWindowViewModel : ViewModelBase
{
    private readonly IPreferencesService _preferencesService;

    public ObservableCollection<PreferencesCategory> Categories { get; }

    [ObservableProperty]
    private PreferencesCategory? _selectedCategory;

    [ObservableProperty]
    private bool _hasChanges;

    public PreferencesWindowViewModel(IPreferencesService preferencesService)
    {
        _preferencesService = preferencesService;

        Categories =
        [
            new("General", new GeneralPreferencesViewModel(_preferencesService.Current)),
            new("Appearance", new AppearancePreferencesViewModel())
        ];

        foreach (PreferencesCategory category in Categories)
            category.ViewModel.PropertyChanged += PreferenceCategory_PropertyChanged;

        SelectedCategory = Categories[0];
        UpdateHasChanges();
    }

    public void Apply()
    {
        if (!HasChanges)
            return;

        ApplicationPreferences preferences = _preferencesService.Current.Copy();

        foreach (PreferencesCategory category in Categories)
            category.ViewModel.ApplyTo(preferences);

        _preferencesService.Save(preferences);

        foreach (PreferencesCategory category in Categories)
            category.ViewModel.AcceptChanges(_preferencesService.Current);

        UpdateHasChanges();
    }

    private void PreferenceCategory_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateHasChanges();
    }

    private void UpdateHasChanges()
    {
        HasChanges = Categories.Any(category => category.ViewModel.HasChanges);
    }
}
