using Avalonia.Media.Imaging;
using System;
using System.Threading;
using System.Threading.Tasks;
using ZENITH.Rendering;

namespace ZENITH.Rendering.BitmapDecoding;

public sealed class FitsBitmapDecoder : IBitmapDecoder
{
    public FitsBitmapDecoder(string path)
    {
        Path = path;
    }

    public string Path { get; }
    public int PixelWidth => throw CreateNotImplementedException();
    public int PixelHeight => throw CreateNotImplementedException();
    public int Channels => throw CreateNotImplementedException();

    public Task<WriteableBitmap> RenderViewportAsync(
        ImageViewportRenderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<WriteableBitmap>(cancellationToken);

        return Task.FromException<WriteableBitmap>(CreateNotImplementedException());
    }

    public void Dispose()
    {
    }

    private static NotSupportedException CreateNotImplementedException()
    {
        return new NotSupportedException("FITS decoding is planned, but it is not implemented yet.");
    }
}
