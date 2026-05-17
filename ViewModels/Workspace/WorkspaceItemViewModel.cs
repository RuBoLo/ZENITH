using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.ObjectModel;

namespace ZENITH.ViewModels.Workspace;

public abstract partial class WorkspaceItemViewModel : ViewModelBase, IDisposable
{
    protected WorkspaceItemViewModel(
        string title,
        double x,
        double y,
        double width,
        double height)
    {
        _title = title;
        _x = x;
        _y = y;
        _width = width;
        _height = height;
        CloseCommand = new RelayCommand(RequestClose);
    }

    public event EventHandler? CloseRequested;
    public event EventHandler? ActivationRequested;

    public ObservableCollection<WorkspacePortViewModel> Ports { get; } = [];
    public IRelayCommand CloseCommand { get; }

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private double _x;

    [ObservableProperty]
    private double _y;

    [ObservableProperty]
    private int _zIndex;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private double _width;

    [ObservableProperty]
    private double _height;

    public void RequestActivation()
    {
        ActivationRequested?.Invoke(this, EventArgs.Empty);
    }

    protected void RequestClose()
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }

    public virtual void Dispose()
    {
    }

    protected virtual void OnWindowSizeChanged()
    {
    }

    partial void OnWidthChanged(double value)
    {
        OnWindowSizeChanged();
    }

    partial void OnHeightChanged(double value)
    {
        OnWindowSizeChanged();
    }
}
