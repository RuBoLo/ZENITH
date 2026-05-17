using Avalonia.Media.Imaging;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Runtime;
using System.Threading;
using System.Threading.Tasks;
using ZENITH.Rendering;
using ZENITH.ViewModels.Workspace;

namespace ZENITH.ViewModels.Documents;

public partial class ImageDocumentViewModel : WorkspaceItemViewModel
{
    private const double DocumentBorderThickness = 2;
    private const double DocumentBorderSize = DocumentBorderThickness * 2;
    private const double TitleBarHeight = 18;
    private const double SidebarWidth = 18;
    private const double ScrollBarSize = 14;
    private const double ScrollBarGap = 2;
    private const double MinWindowWidth = 260;
    private const double MinWindowHeight = 260;
    private const int MinZoomStep = -63;
    private const int MaxZoomStep = 63;

    private readonly IImageDocumentSource _source;
    private bool _disposed;
    private bool _suspendViewportNotifications;
    private bool _viewportMetricsDirty;
    private bool _displayRefreshDirty;
    private bool _isRenderingDisplay;
    private bool _renderAgain;
    private int _viewStateVersion;
    private double _actualViewportWidth;
    private double _actualViewportHeight;

    public ImageDocumentViewModel(
        string title,
        IImageDocumentSource source,
        double x,
        double y)
        : base(title, x, y, 480, 360)
    {
        _source = source;

        ZoomInCommand = new RelayCommand(ZoomIn);
        ZoomOutCommand = new RelayCommand(ZoomOut);
        ShowRgbCommand = new RelayCommand(() => SetChannelView(ImageChannelView.Rgb));
        ShowRedCommand = new RelayCommand(() => SetChannelView(ImageChannelView.Red));
        ShowGreenCommand = new RelayCommand(() => SetChannelView(ImageChannelView.Green));
        ShowBlueCommand = new RelayCommand(() => SetChannelView(ImageChannelView.Blue));
        ShowInvertedCommand = new RelayCommand(() => SetChannelView(ImageChannelView.Inverted));
        ShowLuminanceCommand = new RelayCommand(() => SetChannelView(ImageChannelView.Luminance));
        Ports.Add(WorkspacePortViewModel.Output("image", "Image", "Image"));
    }

    public int PixelWidth => _source.PixelWidth;
    public int PixelHeight => _source.PixelHeight;
    public int Channels => _source.Channels;
    public string ImageInfo => $"{PixelWidth} x {PixelHeight} x {Channels}";
    public IRelayCommand ZoomInCommand { get; }
    public IRelayCommand ZoomOutCommand { get; }
    public IRelayCommand ShowRgbCommand { get; }
    public IRelayCommand ShowRedCommand { get; }
    public IRelayCommand ShowGreenCommand { get; }
    public IRelayCommand ShowBlueCommand { get; }
    public IRelayCommand ShowInvertedCommand { get; }
    public IRelayCommand ShowLuminanceCommand { get; }

    [ObservableProperty]
    private WriteableBitmap? _image;

    [ObservableProperty]
    private ImageChannelView _channelView = ImageChannelView.Rgb;

    [ObservableProperty]
    private int _zoomStep;

    [ObservableProperty]
    private double _renderScaling = 1;

    [ObservableProperty]
    private double _horizontalScrollValue;

    [ObservableProperty]
    private double _verticalScrollValue;

    public double ViewportWidth => _actualViewportWidth > 0
        ? _actualViewportWidth
        : Math.Max(0, Width - DocumentBorderSize - SidebarWidth - ScrollBarGap - ScrollBarSize);

    public double ViewportHeight => _actualViewportHeight > 0
        ? _actualViewportHeight
        : Math.Max(0, Height - DocumentBorderSize - TitleBarHeight - ScrollBarGap - ScrollBarSize);

    public double PhysicalScale => ZoomStep >= 0
        ? ZoomStep + 1
        : 1.0 / (1 - ZoomStep);

    public double DipScale => PhysicalScale / Math.Max(0.01, RenderScaling);

    public double DisplayWidth => PixelWidth * DipScale;
    public double DisplayHeight => PixelHeight * DipScale;

    public double VisibleSourceWidth => Math.Min(PixelWidth, ViewportWidth / Math.Max(0.0001, DipScale));
    public double VisibleSourceHeight => Math.Min(PixelHeight, ViewportHeight / Math.Max(0.0001, DipScale));

    public double HorizontalScrollMaximum => Math.Max(0, PixelWidth - VisibleSourceWidth);
    public double VerticalScrollMaximum => Math.Max(0, PixelHeight - VisibleSourceHeight);

    public double HorizontalViewportSize => Math.Max(0, VisibleSourceWidth);
    public double VerticalViewportSize => Math.Max(0, VisibleSourceHeight);

    public double HorizontalLargeChange => Math.Max(16, VisibleSourceWidth * 0.9);
    public double VerticalLargeChange => Math.Max(16, VisibleSourceHeight * 0.9);

    public string ZoomLabel
    {
        get
        {
            if (ZoomStep == 0)
                return "1:1";

            return ZoomStep > 0
                ? $"{ZoomStep + 1}:1"
                : $"1:{1 - ZoomStep}";
        }
    }

    public bool IsRgbView => ChannelView == ImageChannelView.Rgb;
    public bool IsRedView => ChannelView == ImageChannelView.Red;
    public bool IsGreenView => ChannelView == ImageChannelView.Green;
    public bool IsBlueView => ChannelView == ImageChannelView.Blue;
    public bool IsInvertedView => ChannelView == ImageChannelView.Inverted;
    public bool IsLuminanceView => ChannelView == ImageChannelView.Luminance;

    public async Task LoadPreviewAsync(CancellationToken cancellationToken = default)
    {
        await RefreshDisplayBitmapAsync(cancellationToken);
    }

    public void ApplyInitialLayout(
        double workspaceWidth,
        double workspaceHeight,
        double renderScaling)
    {
        RunViewportUpdate(() =>
        {
            RenderScaling = Math.Max(0.01, renderScaling);

            double availableWidth = Math.Max(MinWindowWidth, workspaceWidth - 96);
            double availableHeight = Math.Max(MinWindowHeight, workspaceHeight - 96);
            double availableImageWidthPhysical = Math.Max(1, (availableWidth - DocumentBorderSize - SidebarWidth - ScrollBarGap - ScrollBarSize) * RenderScaling);
            double availableImageHeightPhysical = Math.Max(1, (availableHeight - DocumentBorderSize - TitleBarHeight - ScrollBarGap - ScrollBarSize) * RenderScaling);

            ZoomStep = ChooseFitZoomStep(availableImageWidthPhysical, availableImageHeightPhysical);

            Width = Math.Clamp(DisplayWidth + DocumentBorderSize + SidebarWidth + ScrollBarGap + ScrollBarSize, MinWindowWidth, availableWidth);
            Height = Math.Clamp(DisplayHeight + DocumentBorderSize + TitleBarHeight + ScrollBarGap + ScrollBarSize, MinWindowHeight, availableHeight);

            SetScrollValues(0, 0);
        });
    }

    public void SetViewportMetrics(
        double viewportWidth,
        double viewportHeight,
        double renderScaling)
    {
        RunViewportUpdate(() =>
        {
            SetActualViewportMetrics(viewportWidth, viewportHeight);
            RenderScaling = Math.Max(0.01, renderScaling);
            ClampScrollValues();
        });

        QueueDisplayRefreshIfReady();
    }

    public void PanFromGesture(
        double startHorizontalScroll,
        double startVerticalScroll,
        double deltaX,
        double deltaY)
    {
        SetScrollValues(
            startHorizontalScroll - (deltaX / Math.Max(0.0001, DipScale)),
            startVerticalScroll - (deltaY / Math.Max(0.0001, DipScale)));
    }

    public void SetScrollValues(double horizontal, double vertical)
    {
        double clampedHorizontal = ClampHorizontalScroll(horizontal);
        double clampedVertical = ClampVerticalScroll(vertical);

        if (Math.Abs(clampedHorizontal - HorizontalScrollValue) <= 0.001
            && Math.Abs(clampedVertical - VerticalScrollValue) <= 0.001)
        {
            return;
        }

        RunViewportUpdate(() =>
        {
            HorizontalScrollValue = clampedHorizontal;
            VerticalScrollValue = clampedVertical;
        });
    }

    public void ZoomIn()
    {
        ZoomCentered(1);
    }

    public void ZoomOut()
    {
        ZoomCentered(-1);
    }

    private void SetChannelView(ImageChannelView channelView)
    {
        if (ChannelView == channelView)
            return;

        ChannelView = channelView;
    }

    public override void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        WriteableBitmap? image = Image;
        Image = null;
        image?.Dispose();
        _source.Dispose();
        ScheduleMemoryCleanup();
    }

    protected override void OnWindowSizeChanged()
    {
        if (_disposed) return;
        ClampScrollValues();
        NotifyViewportMetricsChanged();
    }

    partial void OnRenderScalingChanged(double value)
    {
        if (_disposed) return;
        ClampScrollValues();
        NotifyViewportMetricsChanged();
        QueueDisplayRefreshIfReady();
    }

    partial void OnZoomStepChanged(int value)
    {
        if (_disposed) return;
        ClampScrollValues();
        NotifyViewportMetricsChanged();
        QueueDisplayRefreshIfReady();
    }

    partial void OnChannelViewChanged(ImageChannelView value)
    {
        if (_disposed) return;

        OnPropertyChanged(nameof(IsRgbView));
        OnPropertyChanged(nameof(IsRedView));
        OnPropertyChanged(nameof(IsGreenView));
        OnPropertyChanged(nameof(IsBlueView));
        OnPropertyChanged(nameof(IsInvertedView));
        QueueDisplayRefreshIfReady();
    }

    partial void OnHorizontalScrollValueChanged(double value)
    {
        double clamped = ClampHorizontalScroll(value);
        if (Math.Abs(clamped - value) > 0.001)
        {
            HorizontalScrollValue = clamped;
            return;
        }

        QueueDisplayRefreshIfReady();
    }

    partial void OnVerticalScrollValueChanged(double value)
    {
        double clamped = ClampVerticalScroll(value);
        if (Math.Abs(clamped - value) > 0.001)
        {
            VerticalScrollValue = clamped;
            return;
        }

        QueueDisplayRefreshIfReady();
    }

    private void ZoomCentered(int direction)
    {
        int newZoomStep = Math.Clamp(ZoomStep + Math.Sign(direction), MinZoomStep, MaxZoomStep);
        if (newZoomStep == ZoomStep)
            return;

        double viewportX = ViewportWidth * 0.5;
        double viewportY = ViewportHeight * 0.5;
        double oldDipScale = GetDipScale(ZoomStep);
        if (oldDipScale <= 0)
            return;

        double imagePixelX = Math.Clamp((viewportX - GetImageLeft(ZoomStep, HorizontalScrollValue)) / oldDipScale, 0, PixelWidth);
        double imagePixelY = Math.Clamp((viewportY - GetImageTop(ZoomStep, VerticalScrollValue)) / oldDipScale, 0, PixelHeight);
        double newDipScale = GetDipScale(newZoomStep);
        double newDisplayWidth = PixelWidth * newDipScale;
        double newDisplayHeight = PixelHeight * newDipScale;

        RunViewportUpdate(() =>
        {
            ZoomStep = newZoomStep;

            SetScrollValues(
                newDisplayWidth <= ViewportWidth ? 0 : imagePixelX - (viewportX / newDipScale),
                newDisplayHeight <= ViewportHeight ? 0 : imagePixelY - (viewportY / newDipScale));
        });
    }

    private void ClampScrollValues()
    {
        double horizontal = ClampHorizontalScroll(HorizontalScrollValue);
        double vertical = ClampVerticalScroll(VerticalScrollValue);

        if (Math.Abs(horizontal - HorizontalScrollValue) > 0.001)
            HorizontalScrollValue = horizontal;

        if (Math.Abs(vertical - VerticalScrollValue) > 0.001)
            VerticalScrollValue = vertical;
    }

    private double ClampHorizontalScroll(double value)
        => Math.Clamp(value, 0, HorizontalScrollMaximum);

    private double ClampVerticalScroll(double value)
        => Math.Clamp(value, 0, VerticalScrollMaximum);

    private void NotifyViewportMetricsChanged()
    {
        if (_suspendViewportNotifications)
        {
            _viewportMetricsDirty = true;
            return;
        }

        OnPropertyChanged(nameof(ViewportWidth));
        OnPropertyChanged(nameof(ViewportHeight));
        OnPropertyChanged(nameof(PhysicalScale));
        OnPropertyChanged(nameof(DipScale));
        OnPropertyChanged(nameof(DisplayWidth));
        OnPropertyChanged(nameof(DisplayHeight));
        OnPropertyChanged(nameof(VisibleSourceWidth));
        OnPropertyChanged(nameof(VisibleSourceHeight));
        OnPropertyChanged(nameof(HorizontalScrollMaximum));
        OnPropertyChanged(nameof(VerticalScrollMaximum));
        OnPropertyChanged(nameof(HorizontalViewportSize));
        OnPropertyChanged(nameof(VerticalViewportSize));
        OnPropertyChanged(nameof(HorizontalLargeChange));
        OnPropertyChanged(nameof(VerticalLargeChange));
        OnPropertyChanged(nameof(ZoomLabel));
    }

    private void RunViewportUpdate(Action action)
    {
        bool wasSuspended = _suspendViewportNotifications;

        if (wasSuspended)
        {
            action();
            return;
        }

        _suspendViewportNotifications = true;

        try
        {
            action();
        }
        finally
        {
            _suspendViewportNotifications = false;

            bool notifyViewport = _viewportMetricsDirty;
            bool refreshDisplay = _displayRefreshDirty;
            _viewportMetricsDirty = false;
            _displayRefreshDirty = false;

            if (notifyViewport)
                NotifyViewportMetricsChanged();

            if (refreshDisplay)
                QueueDisplayRefreshIfReady();
        }
    }

    private void QueueDisplayRefreshIfReady()
    {
        if (_disposed || Image is null)
            return;

        if (_suspendViewportNotifications)
        {
            _displayRefreshDirty = true;
            return;
        }

        Interlocked.Increment(ref _viewStateVersion);
        QueueDisplayRefresh();
    }

    private async void QueueDisplayRefresh()
    {
        try
        {
            if (_isRenderingDisplay)
            {
                _renderAgain = true;
                return;
            }

            await RenderDisplayLoopAsync(CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
        }
        catch
        {
        }
    }

    private async Task RefreshDisplayBitmapAsync(CancellationToken cancellationToken)
    {
        if (_isRenderingDisplay)
        {
            _renderAgain = true;
            return;
        }

        await RenderDisplayLoopAsync(cancellationToken);
    }

    private async Task RenderDisplayLoopAsync(CancellationToken cancellationToken)
    {
        _isRenderingDisplay = true;

        try
        {
            do
            {
                _renderAgain = false;
                await RenderDisplayOnceAsync(cancellationToken);
            }
            while (_renderAgain && !_disposed && !cancellationToken.IsCancellationRequested);
        }
        finally
        {
            _isRenderingDisplay = false;

            if (_renderAgain && !_disposed && !cancellationToken.IsCancellationRequested)
                QueueDisplayRefresh();
        }
    }

    private async Task RenderDisplayOnceAsync(CancellationToken cancellationToken)
    {
        int renderVersion = _viewStateVersion;
        ImageViewportRenderRequest request = CreateViewportRenderRequest();
        WriteableBitmap? bitmap = null;

        try
        {
            bitmap = await _source.CreateViewportBitmapAsync(
                request,
                cancellationToken);

            if (_disposed)
            {
                bitmap.Dispose();
                return;
            }

            if (renderVersion != _viewStateVersion)
                _renderAgain = true;

            if (renderVersion != _viewStateVersion && !CanDisplayRenderedRequest(request))
            {
                bitmap.Dispose();
                return;
            }

            SetDisplayedBitmap(bitmap);
            bitmap = null;
        }
        finally
        {
            bitmap?.Dispose();
        }
    }

    private void SetDisplayedBitmap(WriteableBitmap bitmap)
    {
        WriteableBitmap? previousImage = Image;
        Image = bitmap;
        previousImage?.Dispose();
    }

    private bool CanDisplayRenderedRequest(ImageViewportRenderRequest request)
    {
        return ZoomStep == request.ZoomStep
            && ChannelView == request.ChannelView
            && Math.Abs(RenderScaling - request.RenderScaling) <= 0.001
            && Math.Abs(ViewportWidth - request.ViewportWidth) <= 0.5
            && Math.Abs(ViewportHeight - request.ViewportHeight) <= 0.5;
    }

    private int ChooseFitZoomStep(double availableWidthPhysical, double availableHeightPhysical)
    {
        double maxPhysicalScale = Math.Min(
            availableWidthPhysical / Math.Max(1, PixelWidth),
            availableHeightPhysical / Math.Max(1, PixelHeight));

        if (maxPhysicalScale >= 1)
            return Math.Clamp((int)Math.Floor(maxPhysicalScale) - 1, 0, MaxZoomStep);

        int denominator = Math.Max(1, (int)Math.Ceiling(1 / Math.Max(0.0001, maxPhysicalScale)));
        return Math.Clamp(1 - denominator, MinZoomStep, 0);
    }

    private ImageViewportRenderRequest CreateViewportRenderRequest()
    {
        return new ImageViewportRenderRequest(
            ViewportWidth,
            ViewportHeight,
            RenderScaling,
            ZoomStep,
            HorizontalScrollValue,
            VerticalScrollValue,
            PixelWidth,
            PixelHeight,
            ChannelView);
    }

    private void SetActualViewportMetrics(double viewportWidth, double viewportHeight)
    {
        double clampedWidth = Math.Max(0, viewportWidth);
        double clampedHeight = Math.Max(0, viewportHeight);

        if (Math.Abs(clampedWidth - _actualViewportWidth) > 0.001)
        {
            _actualViewportWidth = clampedWidth;
            _viewportMetricsDirty = true;
        }

        if (Math.Abs(clampedHeight - _actualViewportHeight) > 0.001)
        {
            _actualViewportHeight = clampedHeight;
            _viewportMetricsDirty = true;
        }
    }

    private double GetDipScale(int zoomStep)
    {
        double physicalScale = zoomStep >= 0
            ? zoomStep + 1
            : 1.0 / (1 - zoomStep);

        return physicalScale / Math.Max(0.01, RenderScaling);
    }

    private double GetImageLeft(int zoomStep, double horizontalScroll)
    {
        double displayWidth = PixelWidth * GetDipScale(zoomStep);
        return displayWidth <= ViewportWidth
            ? (ViewportWidth - displayWidth) * 0.5
            : -horizontalScroll * GetDipScale(zoomStep);
    }

    private double GetImageTop(int zoomStep, double verticalScroll)
    {
        double displayHeight = PixelHeight * GetDipScale(zoomStep);
        return displayHeight <= ViewportHeight
            ? (ViewportHeight - displayHeight) * 0.5
            : -verticalScroll * GetDipScale(zoomStep);
    }

    private static void ScheduleMemoryCleanup()
    {
        Dispatcher.UIThread.Post(() =>
        {
            _ = Task.Run(() =>
            {
                GCSettings.LargeObjectHeapCompactionMode = GCLargeObjectHeapCompactionMode.CompactOnce;
                GC.Collect(2, GCCollectionMode.Aggressive, blocking: false, compacting: true);
            });
        }, DispatcherPriority.Background);
    }

    public static string CreateTitle(string path)
        => Path.GetFileName(path);
}
