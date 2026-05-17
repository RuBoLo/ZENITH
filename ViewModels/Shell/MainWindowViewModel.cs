using Microsoft.Extensions.DependencyInjection;
using ZENITH.Services.Console;
using ZENITH.Services.Workspace;
using ZENITH.ViewModels.Operators;

namespace ZENITH.ViewModels.Shell;

// Root shell view model that owns toolbar, workspace, and bottom bar state
public partial class MainWindowViewModel : ViewModelBase
{
    public ToolBarViewModel ToolBar { get; }
    public BottomBarViewModel BottomBar { get; }

    public WorkspaceViewModel Workspace { get; }
    public IConsoleService Console { get; }

    public MainWindowViewModel()
    {
        Console = App.Services.GetRequiredService<IConsoleService>();
        Workspace = new WorkspaceViewModel();
        ToolBar = new ToolBarViewModel(Workspace);
        BottomBar = new BottomBarViewModel(Workspace, Console);

        App.Services.GetRequiredService<IImageWorkspaceService>().AttachWorkspace(Workspace);
    }

    public void OpenIntegrationOperator()
    {
        var (x, y) = Workspace.GetNextWindowPosition(360, 460);
        Workspace.AddItem(new IntegrationWindowViewModel(x, y));
    }
}
