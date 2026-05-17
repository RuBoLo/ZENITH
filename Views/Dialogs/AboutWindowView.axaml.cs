using Avalonia.Controls;
using Avalonia.Interactivity;
using ZENITH.Services;

namespace ZENITH.Views.Dialogs;

public partial class AboutWindowView : Window
{
    public AboutWindowView()
    {
        InitializeComponent();
        VersionTextBlock.Text = ApplicationInfo.VersionText;
    }

    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
