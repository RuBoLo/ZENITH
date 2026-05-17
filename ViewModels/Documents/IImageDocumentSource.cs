using Avalonia.Media.Imaging;
using System;
using System.Threading;
using System.Threading.Tasks;
using ZENITH.Rendering;

namespace ZENITH.ViewModels.Documents;

public interface IImageDocumentSource : IDisposable
{
    int PixelWidth { get; }
    int PixelHeight { get; }
    int Channels { get; }

    Task<WriteableBitmap> CreateViewportBitmapAsync(
        ImageViewportRenderRequest request,
        CancellationToken cancellationToken = default);
}