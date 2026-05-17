using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Microsoft.Extensions.DependencyInjection;

namespace ZENITH.Preferences;

public partial class PreferencesWindowView : Window
{
    public PreferencesWindowView()
    {
        InitializeComponent();

        IPreferencesService preferencesService = App.Services.GetRequiredService<IPreferencesService>();
        DataContext = new PreferencesWindowViewModel(preferencesService);
    }

    public void DragBar_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e);
    }

    public void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }

    public void Apply_Click(object sender, RoutedEventArgs e)
    {
        if (DataContext is PreferencesWindowViewModel viewModel)
            viewModel.Apply();
    }
}
