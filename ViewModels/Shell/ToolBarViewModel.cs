using CommunityToolkit.Mvvm.Input;
using System.ComponentModel;
using ZENITH.Rendering;
using ZENITH.ViewModels.Documents;

namespace ZENITH.ViewModels.Shell;

public partial class ToolBarViewModel : ViewModelBase
{
    private readonly WorkspaceViewModel _workspace;
    private ImageDocumentViewModel? _observedDocument;

    public ToolBarViewModel(WorkspaceViewModel workspace)
    {
        _workspace = workspace;
        _workspace.PropertyChanged += Workspace_PropertyChanged;
        ZoomInCommand = new RelayCommand(ZoomIn, HasSelectedDocument);
        ZoomOutCommand = new RelayCommand(ZoomOut, HasSelectedDocument);
        WatchSelectedDocument(_workspace.SelectedDocument);
    }

    public ImageChannelViewOption[] ChannelViewOptions { get; } =
    [
        new("RGB", ImageChannelView.Rgb),
        new("R", ImageChannelView.Red),
        new("G", ImageChannelView.Green),
        new("B", ImageChannelView.Blue),
        new("Inverted", ImageChannelView.Inverted),
        new("Luminance", ImageChannelView.Luminance)
    ];

    public ImageDocumentViewModel? SelectedDocument => _workspace.SelectedDocument;
    public bool HasDocumentSelected => SelectedDocument is not null;
    public IRelayCommand ZoomInCommand { get; }
    public IRelayCommand ZoomOutCommand { get; }

    public ImageChannelViewOption? SelectedChannelViewOption
    {
        get
        {
            ImageChannelView selectedChannel = SelectedDocument?.ChannelView ?? ImageChannelView.Rgb;

            foreach (ImageChannelViewOption option in ChannelViewOptions)
            {
                if (option.Value == selectedChannel)
                    return option;
            }

            return null;
        }
        set
        {
            if (value is null || SelectedDocument is null)
                return;

            SelectedDocument.ChannelView = value.Value;
            OnPropertyChanged();
        }
    }

    private void ZoomIn()
    {
        SelectedDocument?.ZoomIn();
    }

    private void ZoomOut()
    {
        SelectedDocument?.ZoomOut();
    }

    private bool HasSelectedDocument()
        => SelectedDocument is not null;

    private void Workspace_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(WorkspaceViewModel.SelectedDocument))
            return;

        WatchSelectedDocument(_workspace.SelectedDocument);
        NotifySelectedDocumentChanged();
    }

    private void SelectedDocument_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ImageDocumentViewModel.ChannelView))
            OnPropertyChanged(nameof(SelectedChannelViewOption));
    }

    private void WatchSelectedDocument(ImageDocumentViewModel? document)
    {
        if (ReferenceEquals(_observedDocument, document))
            return;

        if (_observedDocument is not null)
            _observedDocument.PropertyChanged -= SelectedDocument_PropertyChanged;

        _observedDocument = document;

        if (_observedDocument is not null)
            _observedDocument.PropertyChanged += SelectedDocument_PropertyChanged;
    }

    private void NotifySelectedDocumentChanged()
    {
        OnPropertyChanged(nameof(SelectedDocument));
        OnPropertyChanged(nameof(HasDocumentSelected));
        OnPropertyChanged(nameof(SelectedChannelViewOption));
        ZoomInCommand.NotifyCanExecuteChanged();
        ZoomOutCommand.NotifyCanExecuteChanged();
    }
}

public sealed record ImageChannelViewOption(string Label, ImageChannelView Value);
