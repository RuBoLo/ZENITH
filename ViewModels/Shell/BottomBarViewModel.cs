using System.Collections.ObjectModel;
using System.ComponentModel;
using ZENITH.Services.Console;
using ZENITH.ViewModels.Documents;

namespace ZENITH.ViewModels.Shell;

public partial class BottomBarViewModel : ViewModelBase
{
    private readonly WorkspaceViewModel _workspace;

    public BottomBarViewModel(WorkspaceViewModel workspace, IConsoleService console)
    {
        _workspace = workspace;
        Console = console;
        _workspace.PropertyChanged += Workspace_PropertyChanged;
    }

    public IConsoleService Console { get; }

    public ObservableCollection<ImageDocumentViewModel> Documents => _workspace.Documents;

    public ImageDocumentViewModel? SelectedDocument
    {
        get => _workspace.SelectedDocument;
        set => _workspace.SelectDocument(value);
    }

    private void Workspace_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(WorkspaceViewModel.SelectedDocument))
            OnPropertyChanged(nameof(SelectedDocument));
    }
}
