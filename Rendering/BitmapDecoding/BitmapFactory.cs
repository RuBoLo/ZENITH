using Avalonia.Media.Imaging;
using System;
using System.Collections.Generic;
using System.IO;

namespace ZENITH.Rendering.BitmapDecoding;

public static class BitmapFactory
{
    private static readonly HashSet<string> PngExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png",
    };
    private static readonly HashSet<string> JpegExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpeg", ".jpg"
    };
    private static readonly HashSet<string> FitsExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".fits", ".fit", ".fts"
    };

    public static bool IsSupported(string path)
    {
        string extension = Path.GetExtension(path);
        return PngExtensions.Contains(extension) ||
               JpegExtensions.Contains(extension);
    }

    public static bool IsKnownImageFormat(string path)
    {
        string extension = Path.GetExtension(path);
        return IsSupported(path) ||
               FitsExtensions.Contains(extension);
    }

    public static IBitmapDecoder Create(string path)
    {
        string extension = Path.GetExtension(path);

        if (PngExtensions.Contains(extension)) return new PngBitmapDecoder(path);

        if (JpegExtensions.Contains(extension)) return new JpegBitmapDecoder(path);

        if (FitsExtensions.Contains(extension)) return new FitsBitmapDecoder(path);

        throw new NotSupportedException($"Unsupported image format: {extension}");
    }
}