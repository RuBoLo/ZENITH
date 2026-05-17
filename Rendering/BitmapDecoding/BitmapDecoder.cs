using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using ZENITH.Rendering;

namespace ZENITH.Rendering.BitmapDecoding;

public abstract class BitmapDecoder : IBitmapDecoder
{
    private const int MaxPyramidReduction = 64;

    private static readonly SKSamplingOptions DownsampleSampling = new(SKCubicResampler.Mitchell);
    private static readonly SKSamplingOptions LinearSampling = new(SKFilterMode.Linear, SKMipmapMode.None);
    private static readonly SKSamplingOptions NativeOrUpsampleSampling = new(SKFilterMode.Nearest, SKMipmapMode.None);

    private readonly object _sourceBitmapGate = new();
    private readonly Dictionary<int, SKImage> _pyramidImages = [];
    private SKBitmap? _sourceBitmap;
    private SKImage? _sourceImage;

    protected BitmapDecoder(string path)
    {
        Path = path;

        using SKCodec codec = SKCodec.Create(path)
            ?? throw new InvalidOperationException($"Cannot read image header: {path}");

        PixelWidth = codec.Info.Width;
        PixelHeight = codec.Info.Height;
        Channels = GetSourceChannelCount(codec.Info);
    }

    public string Path { get; }
    public int PixelWidth { get; }
    public int PixelHeight { get; }
    public int Channels { get; }

    public async Task<WriteableBitmap> RenderViewportAsync(
        ImageViewportRenderRequest request,
        CancellationToken cancellationToken = default)
    {
        using DecodedPixelBuffer pixels = await Task.Run(
            () => RenderViewportBgraBuffer(request, cancellationToken),
            cancellationToken);

        return await Dispatcher.UIThread.InvokeAsync(() =>
            CreateBitmap(
                pixels.Width,
                pixels.Height,
                pixels.Pointer,
                pixels.Length));
    }

    public virtual void Dispose()
    {
        lock (_sourceBitmapGate)
        {
            _sourceImage?.Dispose();
            _sourceImage = null;
            _sourceBitmap?.Dispose();
            _sourceBitmap = null;

            foreach (SKImage image in _pyramidImages.Values)
                image.Dispose();

            _pyramidImages.Clear();
        }
    }

    private unsafe DecodedPixelBuffer RenderViewportBgraBuffer(
        ImageViewportRenderRequest request,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        int bitmapWidth = request.BitmapWidth;
        int bitmapHeight = request.BitmapHeight;
        int rowBytes = checked(bitmapWidth * 4);
        int pixelBytes = checked(rowBytes * bitmapHeight);
        byte* pixelBuffer = (byte*)NativeMemory.Alloc((nuint)pixelBytes);

        try
        {
            var info = new SKImageInfo(
                bitmapWidth,
                bitmapHeight,
                SKColorType.Bgra8888,
                SKAlphaType.Opaque);

            using SKSurface surface = SKSurface.Create(info, (IntPtr)pixelBuffer, rowBytes)
                ?? throw new InvalidOperationException("Failed to create image viewport surface.");

            SKCanvas canvas = surface.Canvas;
            canvas.Clear(new SKColor(18, 18, 18));

            using var paint = new SKPaint
            {
                IsAntialias = false,
            };

            DrawSourceBitmap(canvas, paint, request);

            canvas.Flush();
            cancellationToken.ThrowIfCancellationRequested();

            return new DecodedPixelBuffer((IntPtr)pixelBuffer, pixelBytes, bitmapWidth, bitmapHeight);
        }
        catch
        {
            NativeMemory.Free(pixelBuffer);
            throw;
        }
    }

    private void DrawSourceBitmap(
        SKCanvas canvas,
        SKPaint paint,
        ImageViewportRenderRequest request)
    {
        lock (_sourceBitmapGate)
        {
            SKImage sourceImage = GetPyramidImage(ChoosePyramidReduction(request));

            if (!TryCreateVisibleRects(request, out SKRect sourceRect, out SKRect destinationRect))
                return;

            double sourceToPyramidX = sourceImage.Width / (double)request.PixelWidth;
            double sourceToPyramidY = sourceImage.Height / (double)request.PixelHeight;
            var pyramidSourceRect = new SKRect(
                (float)(sourceRect.Left * sourceToPyramidX),
                (float)(sourceRect.Top * sourceToPyramidY),
                (float)(sourceRect.Right * sourceToPyramidX),
                (float)(sourceRect.Bottom * sourceToPyramidY));

            SKSamplingOptions sampling = ChooseSampling(request, sourceImage, pyramidSourceRect, destinationRect);

            using SKColorFilter? colorFilter = CreateChannelFilter(request.ChannelView);
            paint.ColorFilter = colorFilter;
            canvas.DrawImage(sourceImage, pyramidSourceRect, destinationRect, sampling, paint);
            paint.ColorFilter = null;
        }
    }

    private static SKColorFilter? CreateChannelFilter(ImageChannelView channelView)
    {
        return channelView switch
        {
            ImageChannelView.Red => SKColorFilter.CreateColorMatrix(
                [
                    1, 0, 0, 0, 0,
                    1, 0, 0, 0, 0,
                    1, 0, 0, 0, 0,
                    0, 0, 0, 1, 0
                ]),
            ImageChannelView.Green => SKColorFilter.CreateColorMatrix(
                [
                    0, 1, 0, 0, 0,
                    0, 1, 0, 0, 0,
                    0, 1, 0, 0, 0,
                    0, 0, 0, 1, 0
                ]),
            ImageChannelView.Blue => SKColorFilter.CreateColorMatrix(
                [
                    0, 0, 1, 0, 0,
                    0, 0, 1, 0, 0,
                    0, 0, 1, 0, 0,
                    0, 0, 0, 1, 0
                ]),
            ImageChannelView.Inverted => SKColorFilter.CreateColorMatrix(
                [
                    -1, 0, 0, 0, 1,
                    0, -1, 0, 0, 1,
                    0, 0, -1, 0, 1,
                    0, 0, 0, 1, 0
                ]),
            ImageChannelView.Luminance => SKColorFilter.CreateColorMatrix(
                [
                    0.333f, 0.333f, 0.333f, 0, 0,
                    0.333f, 0.333f, 0.333f, 0, 0,
                    0.333f, 0.333f, 0.333f, 0, 0,
                    0, 0, 0, 1, 0
                ]),
            _ => null
        };
    }

    private SKImage GetPyramidImage(int reduction)
    {
        if (reduction <= 1)
            return GetSourceImage();

        if (_pyramidImages.TryGetValue(reduction, out SKImage? image))
            return image;

        SKImage previousImage = GetPyramidImage(reduction / 2);
        int width = Math.Max(1, (int)Math.Ceiling(PixelWidth / (double)reduction));
        int height = Math.Max(1, (int)Math.Ceiling(PixelHeight / (double)reduction));

        var info = new SKImageInfo(
            width,
            height,
            SKColorType.Bgra8888,
            SKAlphaType.Opaque);

        using SKSurface surface = SKSurface.Create(info)
            ?? throw new InvalidOperationException("Failed to create image pyramid surface.");

        SKCanvas canvas = surface.Canvas;
        canvas.Clear(SKColors.Black);

        using var paint = new SKPaint
        {
            IsAntialias = false,
        };

        canvas.DrawImage(
            previousImage,
            new SKRect(0, 0, previousImage.Width, previousImage.Height),
            new SKRect(0, 0, width, height),
            DownsampleSampling,
            paint);
        canvas.Flush();

        image = surface.Snapshot();
        _pyramidImages[reduction] = image;
        return image;
    }

    private SKImage GetSourceImage()
    {
        if (_sourceImage is not null)
            return _sourceImage;

        _sourceBitmap = SKBitmap.Decode(Path)
            ?? throw new InvalidOperationException($"Cannot decode image data: {Path}");
        _sourceImage = SKImage.FromBitmap(_sourceBitmap);

        return _sourceImage
            ?? throw new InvalidOperationException($"Cannot create image surface: {Path}");
    }

    private static int ChoosePyramidReduction(ImageViewportRenderRequest request)
    {
        if (request.PhysicalScale >= 1)
            return 1;

        double targetReduction = 1 / Math.Max(0.0001, request.PhysicalScale);
        int reduction = 1;

        while (reduction < MaxPyramidReduction)
        {
            int nextReduction = reduction * 2;
            if (nextReduction > targetReduction * 1.25)
                break;

            if (request.PixelWidth / nextReduction < 1 || request.PixelHeight / nextReduction < 1)
                break;

            reduction = nextReduction;
        }

        return reduction;
    }

    private static SKSamplingOptions ChooseSampling(
        ImageViewportRenderRequest request,
        SKImage sourceImage,
        SKRect sourceRect,
        SKRect destinationRect)
    {
        if (ReferenceEquals(sourceImage, null))
            return DownsampleSampling;

        double horizontalScale = destinationRect.Width / Math.Max(0.0001, sourceRect.Width);
        double verticalScale = destinationRect.Height / Math.Max(0.0001, sourceRect.Height);
        double scale = Math.Min(horizontalScale, verticalScale);

        if (scale < 1)
            return DownsampleSampling;

        return request.PhysicalScale >= 1
            ? NativeOrUpsampleSampling
            : LinearSampling;
    }

    private static bool TryCreateVisibleRects(
        ImageViewportRenderRequest request,
        out SKRect sourceRect,
        out SKRect destinationRect)
    {
        double scale = Math.Max(0.0001, request.PhysicalScale);
        double sourceLeft = Math.Max(0, -request.ImageLeftPhysical / scale);
        double sourceTop = Math.Max(0, -request.ImageTopPhysical / scale);
        double sourceRight = Math.Min(
            request.PixelWidth,
            (request.BitmapWidth - request.ImageLeftPhysical) / scale);
        double sourceBottom = Math.Min(
            request.PixelHeight,
            (request.BitmapHeight - request.ImageTopPhysical) / scale);

        if (sourceRight <= sourceLeft || sourceBottom <= sourceTop)
        {
            sourceRect = default;
            destinationRect = default;
            return false;
        }

        double destinationLeft = request.ImageLeftPhysical + (sourceLeft * scale);
        double destinationTop = request.ImageTopPhysical + (sourceTop * scale);
        double destinationRight = request.ImageLeftPhysical + (sourceRight * scale);
        double destinationBottom = request.ImageTopPhysical + (sourceBottom * scale);

        sourceRect = new SKRect(
            (float)sourceLeft,
            (float)sourceTop,
            (float)sourceRight,
            (float)sourceBottom);
        destinationRect = new SKRect(
            (float)destinationLeft,
            (float)destinationTop,
            (float)destinationRight,
            (float)destinationBottom);
        return true;
    }

    private static unsafe WriteableBitmap CreateBitmap(
        int width,
        int height,
        IntPtr pixels,
        int pixelBytes)
    {
        var bitmap = new WriteableBitmap(
            new PixelSize(width, height),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Opaque);

        using ILockedFramebuffer framebuffer = bitmap.Lock();
        int sourceRowBytes = checked(width * 4);
        byte* sourceBase = (byte*)pixels;

        if (framebuffer.RowBytes == sourceRowBytes)
        {
            Buffer.MemoryCopy(
                sourceBase,
                (void*)framebuffer.Address,
                pixelBytes,
                pixelBytes);
        }
        else
        {
            byte* destinationBase = (byte*)framebuffer.Address;

            for (int y = 0; y < height; y++)
            {
                Buffer.MemoryCopy(
                    sourceBase + ((long)y * sourceRowBytes),
                    destinationBase + ((long)y * framebuffer.RowBytes),
                    framebuffer.RowBytes,
                    sourceRowBytes);
            }
        }

        return bitmap;
    }

    private static int GetSourceChannelCount(SKImageInfo info)
    {
        if (info.ColorType == SKColorType.Gray8)
            return 1;

        return info.AlphaType == SKAlphaType.Opaque ? 3 : 4;
    }

    private sealed class DecodedPixelBuffer : IDisposable
    {
        public DecodedPixelBuffer(IntPtr pointer, int length, int width, int height)
        {
            Pointer = pointer;
            Length = length;
            Width = width;
            Height = height;
        }

        public IntPtr Pointer { get; }
        public int Length { get; }
        public int Width { get; }
        public int Height { get; }

        public void Dispose()
        {
            unsafe
            {
                NativeMemory.Free((void*)Pointer);
            }
        }
    }
}
