using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;

namespace ZENITH.Views.Documents;

public sealed class ImageViewportControl : Control
{
    public static readonly StyledProperty<IImage?> SourceProperty =
        AvaloniaProperty.Register<ImageViewportControl, IImage?>(nameof(Source));

    static ImageViewportControl()
    {
        AffectsRender<ImageViewportControl>(SourceProperty);
    }

    public IImage? Source
    {
        get => GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public override void Render(DrawingContext context)
    {
        var viewport = new Rect(Bounds.Size);
        context.FillRectangle(Brushes.Transparent, viewport);

        if (Source is null)
            return;

        using (context.PushClip(viewport))
        {
            context.DrawImage(
                Source,
                new Rect(Source.Size),
                viewport);
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Source = null;
        base.OnDetachedFromVisualTree(e);
    }
}
