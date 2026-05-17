using Avalonia.Controls;
using ZENITH.ViewModels.Shell;

namespace ZENITH.Views.Shell;

public partial class WorkspaceView : UserControl
{
    public WorkspaceView()
    {
        InitializeComponent();

        SizeChanged += (_, _) => UpdateViewportSize();
        AttachedToVisualTree += (_, _) => UpdateViewportSize();
    }

    private void UpdateViewportSize()
    {
        if (DataContext is not WorkspaceViewModel viewModel) return;

        double renderScaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        viewModel.SetViewportMetrics(Bounds.Width, Bounds.Height, renderScaling);
    }
}