using System;

namespace ZENITH.Rendering;

public readonly record struct ImageViewportRenderRequest(
    double ViewportWidth,
    double ViewportHeight,
    double RenderScaling,
    int ZoomStep,
    double HorizontalScrollValue,
    double VerticalScrollValue,
    int PixelWidth,
    int PixelHeight,
    ImageChannelView ChannelView)
{
    public int BitmapWidth => Math.Clamp((int)Math.Ceiling(ViewportWidth * RenderScaling), 1, 16384);
    public int BitmapHeight => Math.Clamp((int)Math.Ceiling(ViewportHeight * RenderScaling), 1, 16384);

    public double PhysicalScale => ZoomStep >= 0
        ? ZoomStep + 1
        : 1.0 / (1 - ZoomStep);

    public double DipScale => PhysicalScale / Math.Max(0.01, RenderScaling);

    public double DisplayWidth => PixelWidth * DipScale;
    public double DisplayHeight => PixelHeight * DipScale;

    public double ImageLeftPhysical => DisplayWidth <= ViewportWidth
        ? ((ViewportWidth - DisplayWidth) * RenderScaling) * 0.5
        : -HorizontalScrollValue * PhysicalScale;

    public double ImageTopPhysical => DisplayHeight <= ViewportHeight
        ? ((ViewportHeight - DisplayHeight) * RenderScaling) * 0.5
        : -VerticalScrollValue * PhysicalScale;
}
