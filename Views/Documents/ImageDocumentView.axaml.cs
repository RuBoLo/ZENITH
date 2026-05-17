using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.VisualTree;
using System;
using ZENITH.ViewModels.Documents;

namespace ZENITH.Views.Documents;

public partial class ImageDocumentView : UserControl
{
    private const double DocumentBorderThickness = 2;
    private const double DocumentBorderSize = DocumentBorderThickness * 2;
    private const double TitleBarHeight = 28;
    private const double SidebarWidth = 28;
    private const double ScrollBarGap = 2;
    private const double ScrollBarSize = 14;

    private ImageDocumentViewModel? _currentViewModel;
    private bool _draggingWindow;
    private bool _panningImage;
    private Point _startMouse;
    private double _startX;
    private double _startY;
    private double _startHorizontalScroll;
    private double _startVerticalScroll;
    private double _windowDragDeltaX;
    private double _windowDragDeltaY;
    private readonly TranslateTransform _windowDragTransform = new();
    private Visual? _dragRoot;
    private Border? _titleBar;
    private Border? _viewport;
    private ImageViewportControl? _imageViewport;

    public ImageDocumentView()
    {
        InitializeComponent();

        DataContextChanged += OnDataContextChanged;
        AttachedToVisualTree += (_, _) => UpdateMetrics();

        _titleBar = this.FindControl<Border>("TitleBar");
        _viewport = this.FindControl<Border>("Viewport");
        _imageViewport = this.FindControl<ImageViewportControl>("ImageViewport");

        if (_titleBar is not null)
            _titleBar.PointerPressed += OnTitleBarPointerPressed;

        PointerMoved += OnPointerMoved;
        PointerReleased += OnPointerReleased;
        PointerPressed += OnDocumentPointerPressed;

        if (_viewport is not null)
        {
            _viewport.SizeChanged += (_, _) => UpdateMetrics();
            _viewport.PointerPressed += OnViewportPointerPressed;
            _viewport.PointerWheelChanged += OnViewportPointerWheelChanged;
        }

        if (_imageViewport is not null)
            _imageViewport.SizeChanged += (_, _) => UpdateMetrics();
    }

    private ImageDocumentViewModel? Vm => _currentViewModel;

    private void OnDocumentPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Vm?.RequestActivation();
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        _currentViewModel = DataContext as ImageDocumentViewModel;
        UpdateMetrics();
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

    private void OnViewportPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        Vm?.RequestActivation();

        if (Vm is null || _viewport is null || !e.GetCurrentPoint(_viewport).Properties.IsLeftButtonPressed)
            return;

        _panningImage = true;
        _dragRoot = _viewport;
        _startMouse = e.GetPosition(GetImageViewportVisual());
        _startHorizontalScroll = Vm.HorizontalScrollValue;
        _startVerticalScroll = Vm.VerticalScrollValue;

        e.Pointer.Capture(this);
        e.Handled = true;
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (Vm is null)
            return;

        if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            if (FinishPointerOperation(e.Pointer))
                e.Handled = true;

            return;
        }

        if (_draggingWindow)
        {
            Point position = e.GetPosition(_dragRoot ?? this);
            _windowDragDeltaX = position.X - _startMouse.X;
            _windowDragDeltaY = position.Y - _startMouse.Y;
            _windowDragTransform.X = _windowDragDeltaX;
            _windowDragTransform.Y = _windowDragDeltaY;
            e.Handled = true;
            return;
        }

        if (_panningImage)
        {
            Visual viewport = GetImageViewportVisual();
            Point position = e.GetPosition(viewport);
            Vm.PanFromGesture(
                _startHorizontalScroll,
                _startVerticalScroll,
                position.X - _startMouse.X,
                position.Y - _startMouse.Y);
            e.Handled = true;
        }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (FinishPointerOperation(e.Pointer))
            e.Handled = true;
    }

    private void OnViewportPointerWheelChanged(object? sender, PointerWheelEventArgs e)
    {
        if (Vm is null || e.Delta.Y == 0)
            return;

        if (e.Delta.Y > 0)
            Vm.ZoomIn();
        else
            Vm.ZoomOut();

        e.Handled = true;
    }

    private Visual GetImageViewportVisual()
        => _imageViewport is not null
            ? _imageViewport
            : _viewport ?? (Visual)this;

    private bool FinishPointerOperation(IPointer? pointer)
    {
        if (!_draggingWindow && !_panningImage)
            return false;

        if (_draggingWindow && Vm is not null)
        {
            Vm.X = _startX + _windowDragDeltaX;
            Vm.Y = _startY + _windowDragDeltaY;
        }

        _draggingWindow = false;
        _panningImage = false;
        _dragRoot = null;
        _windowDragDeltaX = 0;
        _windowDragDeltaY = 0;
        _windowDragTransform.X = 0;
        _windowDragTransform.Y = 0;
        RenderTransform = null;

        pointer?.Capture(null);
        return true;
    }

    private void UpdateMetrics()
    {
        if (Vm is null)
            return;

        if (Bounds.Width <= 0 || Bounds.Height <= 0)
            return;

        double renderScaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1;
        double imageViewportWidth = _viewport is not null && _viewport.Bounds.Width > 0
            ? _viewport.Bounds.Width
            : _imageViewport is not null && _imageViewport.Bounds.Width > 0
                ? _imageViewport.Bounds.Width
                : Math.Max(0, Bounds.Width - DocumentBorderSize - SidebarWidth - ScrollBarGap - ScrollBarSize);
        double imageViewportHeight = _viewport is not null && _viewport.Bounds.Height > 0
            ? _viewport.Bounds.Height
            : _imageViewport is not null && _imageViewport.Bounds.Height > 0
                ? _imageViewport.Bounds.Height
                : Math.Max(0, Bounds.Height - DocumentBorderSize - TitleBarHeight - ScrollBarGap - ScrollBarSize);

        Vm.SetViewportMetrics(
            imageViewportWidth,
            imageViewportHeight,
            renderScaling);
    }

    private static bool StartedOnButton(object? source)
    {
        if (source is Button)
            return true;

        return source is Visual visual &&
               visual.FindAncestorOfType<Button>() is not null;
    }
}
