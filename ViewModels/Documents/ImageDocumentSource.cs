using Avalonia.Media.Imaging;
using System.Threading;
using System.Threading.Tasks;
using ZENITH.Rendering;
using ZENITH.Rendering.BitmapDecoding;

namespace ZENITH.ViewModels.Documents;

public sealed class ImageDocumentSource(IBitmapDecoder decoder) : IImageDocumentSource
{
    public int PixelWidth => decoder.PixelWidth;
    public int PixelHeight => decoder.PixelHeight;
    public int Channels => decoder.Channels;

    public Task<WriteableBitmap> CreateViewportBitmapAsync(
        ImageViewportRenderRequest request,
        CancellationToken cancellationToken = default)
    {
        return decoder.RenderViewportAsync(request, cancellationToken);
    }

    public void Dispose()
    {
        decoder.Dispose();
    }
}
