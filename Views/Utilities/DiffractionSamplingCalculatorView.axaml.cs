using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using ZENITH.ViewModels.Utilities;

namespace ZENITH.Views.Utilities;

public partial class DiffractionSamplingCalculatorView : Window
{
    public DiffractionSamplingCalculatorView()
    {
        InitializeComponent();
        DataContext = new DiffractionSamplingCalculatorViewModel();
    }

    private void DragBar_PointerPressed(object sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) BeginMoveDrag(e);
    }

    private void Minimize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
