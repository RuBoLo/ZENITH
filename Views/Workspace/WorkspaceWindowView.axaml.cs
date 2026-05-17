using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using System;
using ZENITH.ViewModels.Workspace;

namespace ZENITH.Views.Workspace;

public partial class WorkspaceWindowView : UserControl
{
    private bool _draggingWindow;
    private Point _startMouse;
    private double _startX;
    private double _startY;
    private double _windowDragDeltaX;
    private double _windowDragDeltaY;
    private readonly TranslateTransform _windowDragTransform = new();
    private Visual? _dragRoot;

    public WorkspaceWindowView()
    {
        InitializeComponent();

        PointerPressed += OnWindowPointerPressed;
        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        TitleBar.PointerPressed += OnTitleBarPointerPressed;
    }

    private WorkspaceItemViewModel? Vm => DataContext as WorkspaceItemViewModel;

    private void OnWindowPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Vm?.RequestActivation();
    }

    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Vm?.RequestActivation();

        if (StartedOnButton(e.Source))
            return;

        if (Vm is null || !e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            return;

        _draggingWindow = true;
        _dragRoot = TopLevel.GetTopLevel(this) as Visual ?? this;
        _startMouse = e.GetPosition(_dragRoot);
        _startX = Vm.X;
        _startY = Vm.Y;
        _windowDragDeltaX = 0;
        _windowDragDeltaY = 0;
        RenderTransform = _windowDragTransform;

        e.Pointer.Capture(this);
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_draggingWindow || Vm is null)
            return;

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            if (FinishPointerOperation(e.Pointer))
                e.Handled = true;

            return;
        }

        Point position = e.GetPosition(_dragRoot ?? this);
        _windowDragDeltaX = position.X - _startMouse.X;
        _windowDragDeltaY = position.Y - _startMouse.Y;
        _windowDragTransform.X = _windowDragDeltaX;
        _windowDragTransform.Y = _windowDragDeltaY;
        e.Handled = true;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (FinishPointerOperation(e.Pointer))
            e.Handled = true;
    }

    private bool FinishPointerOperation(IPointer? pointer)
    {
        if (!_draggingWindow)
            return false;

        if (Vm is not null)
        {
            Vm.X = _startX + _windowDragDeltaX;
            Vm.Y = _startY + _windowDragDeltaY;
        }

        _draggingWindow = false;
        _dragRoot = null;
        _windowDragDeltaX = 0;
        _windowDragDeltaY = 0;
        _windowDragTransform.X = 0;
        _windowDragTransform.Y = 0;
        RenderTransform = null;

        pointer?.Capture(null);
        return true;
    }

    private static bool StartedOnButton(object? source)
    {
        if (source is Button)
            return true;

        return source is Visual visual &&
               visual.FindAncestorOfType<Button>() is not null;
    }
}
