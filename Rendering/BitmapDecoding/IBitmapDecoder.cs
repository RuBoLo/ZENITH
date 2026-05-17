using Avalonia.Media.Imaging;
using System;
using System.Threading;
using System.Threading.Tasks;
using ZENITH.Rendering;

namespace ZENITH.Rendering.BitmapDecoding;

public interface IBitmapDecoder : IDisposable
{
    string Path { get; }

    int PixelWidth { get; }
    int PixelHeight { get; }
    int Channels { get; }

    Task<WriteableBitmap> RenderViewportAsync(
        ImageViewportRenderRequest request,
        CancellationToken cancellationToken = default);
}
