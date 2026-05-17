using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.ObjectModel;
using ZENITH.ViewModels.Documents;
using ZENITH.ViewModels.Workspace;

namespace ZENITH.ViewModels.Shell;

public partial class WorkspaceViewModel : ViewModelBase
{
    public ObservableCollection<WorkspaceItemViewModel> Items { get; } = [];
    public ObservableCollection<ImageDocumentViewModel> Documents { get; } = [];
    private int _nextZIndex;
    private int _nextItemIndex;
    private WorkspaceItemViewModel? _selectedItem;
    private ImageDocumentViewModel? _selectedDocument;

    public WorkspaceItemViewModel? SelectedItem => _selectedItem;
    public ImageDocumentViewModel? SelectedDocument => _selectedDocument;

    [ObservableProperty]
    private double _viewportWidth = 900;

    [ObservableProperty]
    private double _viewportHeight = 500;

    [ObservableProperty]
    private double _renderScaling = 1;

    public void AddItem(WorkspaceItemViewModel item)
    {
        item.CloseRequested += Item_CloseRequested;
        item.ActivationRequested += Item_ActivationRequested;

        Items.Add(item);

        if (item is ImageDocumentViewModel document)
            Documents.Add(document);

        SelectItem(item);
    }

    public void AddDocument(ImageDocumentViewModel document)
    {
        AddItem(document);
    }

    public void RemoveItem(WorkspaceItemViewModel item)
    {
        if (!Items.Contains(item)) return;

        item.CloseRequested -= Item_CloseRequested;
        item.ActivationRequested -= Item_ActivationRequested;
        bool wasSelected = ReferenceEquals(SelectedItem, item);

        Items.Remove(item);

        if (item is ImageDocumentViewModel document)
            Documents.Remove(document);

        item.IsSelected = false;
        item.Dispose();

        if (wasSelected)
            SelectItem(Items.Count > 0 ? Items[^1] : null);
    }

    public void RemoveDocument(ImageDocumentViewModel document)
    {
        RemoveItem(document);
    }


    public void SelectItem(WorkspaceItemViewModel? item)
    {
        if (item is not null && !Items.Contains(item)) return;

        if (ReferenceEquals(SelectedItem, item))
        {
            if (item is not null)
                BringToFront(item);

            return;
        }

        WorkspaceItemViewModel? previous = SelectedItem;

        if (!SetProperty(ref _selectedItem, item, nameof(SelectedItem)))
            return;

        if (previous is not null)
            previous.IsSelected = false;

        if (item is not null)
        {
            item.IsSelected = true;
            BringToFront(item);
        }

        ImageDocumentViewModel? selectedDocument = item as ImageDocumentViewModel;
        SetProperty(ref _selectedDocument, selectedDocument, nameof(SelectedDocument));
    }

    public void SelectDocument(ImageDocumentViewModel? document)
    {
        SelectItem(document);
    }

    public void SetViewportMetrics(double width, double height, double renderScaling)
    {
        ViewportWidth = width;
        ViewportHeight = height;
        RenderScaling = renderScaling;
    }

    public (double X, double Y) GetNextWindowPosition(double width, double height)
    {
        int index = _nextItemIndex++;
        double offset = (index % 12) * 28;
        double x = Math.Min(40 + offset, Math.Max(40, ViewportWidth - width));
        double y = Math.Min(40 + offset, Math.Max(40, ViewportHeight - height));

        return (x, y);
    }

    private void Item_CloseRequested(object? sender, EventArgs e)
    {
        if (sender is WorkspaceItemViewModel item)
            RemoveItem(item);
    }

    private void Item_ActivationRequested(object? sender, EventArgs e)
    {
        if (sender is WorkspaceItemViewModel item)
            SelectItem(item);
    }

    private void BringToFront(WorkspaceItemViewModel item)
    {
        item.ZIndex = ++_nextZIndex;
    }
}
