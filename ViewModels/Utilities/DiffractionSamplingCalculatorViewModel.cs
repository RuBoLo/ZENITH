using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;

namespace ZENITH.ViewModels.Utilities;

public partial class DiffractionSamplingCalculatorViewModel : ObservableObject
{
    private const double RadiansToArcseconds = 206264.80624709636;
    private static readonly SolidColorBrush NeutralSamplingBrush = new(Colors.Transparent);
    private static readonly SolidColorBrush WellSampledBrush = new(Color.FromRgb(38, 166, 91));
    private static readonly SolidColorBrush MildSamplingBrush = new(Color.FromRgb(217, 177, 33));
    private static readonly SolidColorBrush ModerateSamplingBrush = new(Color.FromRgb(242, 117, 24));
    private static readonly SolidColorBrush SevereSamplingBrush = new(Color.FromRgb(220, 53, 69));

    [ObservableProperty]
    private string aperture = string.Empty;

    [ObservableProperty]
    private string wavelength = "550";

    [ObservableProperty]
    private string focalLength = string.Empty;

    [ObservableProperty]
    private string pixelPitch = string.Empty;

    [ObservableProperty]
    private string seeing = "2";

    [ObservableProperty]
    private string rayleighLimitOutput = string.Empty;

    [ObservableProperty]
    private string dawesLimitOutput = string.Empty;

    [ObservableProperty]
    private string focalRatioOutput = string.Empty;

    [ObservableProperty]
    private string pixelScaleOutput = string.Empty;

    [ObservableProperty]
    private string samplingOutput = string.Empty;

    [ObservableProperty]
    private SolidColorBrush samplingBrush = NeutralSamplingBrush;

    [RelayCommand]
    private void Execute()
    {
        bool hasAperture = TryGetPositiveValue(Aperture, out double aperture);
        bool hasWavelength = TryGetPositiveValue(Wavelength, out double wavelength);
        bool hasFocalLength = TryGetPositiveValue(FocalLength, out double focalLength);
        bool hasPixelPitch = TryGetPositiveValue(PixelPitch, out double pixelPitch);
        bool hasSeeing = TryGetPositiveValue(Seeing, out double seeing);

        RayleighLimitOutput = hasAperture && hasWavelength
            ? $"{Format(CalculateRayleighLimitArcseconds(aperture, wavelength))} arcsec"
            : string.Empty;

        DawesLimitOutput = hasAperture
            ? $"{Format(116.0 / aperture)} arcsec"
            : string.Empty;

        FocalRatioOutput = hasAperture && hasFocalLength
            ? $"f/{Format(focalLength / aperture)}"
            : string.Empty;

        double? pixelScale = hasFocalLength && hasPixelPitch
            ? 206.265 * pixelPitch / focalLength
            : null;

        PixelScaleOutput = pixelScale.HasValue
            ? $"{Format(pixelScale.Value)} arcsec/px"
            : string.Empty;

        if (pixelScale.HasValue && hasSeeing)
        {
            double seeingPixels = seeing / pixelScale.Value;
            SamplingEstimate samplingEstimate = GetSamplingEstimate(seeingPixels);

            SamplingOutput = $"{samplingEstimate.Label} ({Format(seeingPixels)} px FWHM)";
            SamplingBrush = samplingEstimate.Brush;
        }
        else
        {
            SamplingOutput = string.Empty;
            SamplingBrush = NeutralSamplingBrush;
        }
    }

    private static double CalculateRayleighLimitArcseconds(double apertureMillimeters, double wavelengthNanometers) =>
        1.22 * (wavelengthNanometers * 1e-9) / (apertureMillimeters * 1e-3) * RadiansToArcseconds;

    private static SamplingEstimate GetSamplingEstimate(double seeingPixels)
    {
        if (seeingPixels < 1.25) return new SamplingEstimate("Severely undersampled", SevereSamplingBrush);
        if (seeingPixels < 1.75) return new SamplingEstimate("Undersampled", ModerateSamplingBrush);
        if (seeingPixels < 2) return new SamplingEstimate("Mildly undersampled", MildSamplingBrush);
        if (seeingPixels <= 3) return new SamplingEstimate("Well sampled", WellSampledBrush);
        if (seeingPixels <= 3.5) return new SamplingEstimate("Mildly oversampled", MildSamplingBrush);
        if (seeingPixels <= 4.25) return new SamplingEstimate("Oversampled", ModerateSamplingBrush);

        return new SamplingEstimate("Severely oversampled", SevereSamplingBrush);
    }

    private static bool TryGetPositiveValue(string value, out double result)
    {
        bool parsed =
            double.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out result) ||
            double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out result);

        return parsed && result > 0;
    }

    private static string Format(double value) =>
        value.ToString("0.##", CultureInfo.InvariantCulture);

    private sealed record SamplingEstimate(string Label, SolidColorBrush Brush);
}
