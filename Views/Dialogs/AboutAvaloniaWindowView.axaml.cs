using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ZENITH.Views.Dialogs;

public partial class AboutAvaloniaWindowView : Window
{
    public AboutAvaloniaWindowView()
    {
        InitializeComponent();
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}