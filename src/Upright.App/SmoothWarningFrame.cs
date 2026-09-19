using System.Windows;
using System.Windows.Media;

namespace Upright.App;

public static class WarningGradientProfile
{
    public static double SampleOpacity(double normalizedDistance)
    {
        double distance = double.IsFinite(normalizedDistance)
            ? Math.Clamp(normalizedDistance, 0, 1)
            : 0;
        double smootherStep =
            distance * distance * distance *
            ((distance * ((distance * 6) - 15)) + 10);
        return 1 - smootherStep;
    }
}

internal sealed class SmoothWarningFrame : FrameworkElement
{
    private static readonly System.Windows.Media.Color WarningColor =
        System.Windows.Media.Color.FromRgb(239, 68, 68);
    private static readonly SolidColorBrush[] WarningBrushes =
        CreateWarningBrushes();
    private double _gradientDepth;

    public double GradientDepth
    {
        get => _gradientDepth;
        set
        {
            double sanitized = double.IsFinite(value)
                ? Math.Max(0, value)
                : 0;
            if (Math.Abs(_gradientDepth - sanitized) < 0.01)
            {
                return;
            }

            _gradientDepth = sanitized;
            InvalidateVisual();
        }
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        double depth = Math.Min(
            GradientDepth,
            Math.Min(ActualWidth, ActualHeight) / 2);
        if (depth <= 0 || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        // Three sub-DIP samples per unit keep the fade smooth even at the
        // minimum 18-DIP warning depth, without relying on separate edge
        // brushes that would overlap visibly at the corners.
        int bandCount = Math.Max(1, (int)Math.Ceiling(depth * 3));
        double bandWidth = depth / bandCount;

        // Draw inner bands first. Adjacent strokes overlap by a fraction of a
        // device-independent pixel so antialiasing cannot expose hairline gaps.
        for (int index = bandCount - 1; index >= 0; index--)
        {
            double normalizedDistance = (index + 0.5) / bandCount;
            double opacity =
                WarningGradientProfile.SampleOpacity(normalizedDistance);
            if (opacity <= 0.001)
            {
                continue;
            }

            byte alpha = (byte)Math.Round(opacity * byte.MaxValue);
            SolidColorBrush brush = WarningBrushes[alpha];

            var pen = new System.Windows.Media.Pen(brush, bandWidth + 0.08)
            {
                LineJoin = System.Windows.Media.PenLineJoin.Round,
            };
            pen.Freeze();

            double inset = (index * bandWidth) + (bandWidth / 2);
            double width = ActualWidth - (2 * inset);
            double height = ActualHeight - (2 * inset);
            if (width <= 0 || height <= 0)
            {
                continue;
            }

            drawingContext.DrawRectangle(
                null,
                pen,
                new Rect(inset, inset, width, height));
        }
    }

    private static SolidColorBrush[] CreateWarningBrushes()
    {
        var brushes = new SolidColorBrush[byte.MaxValue + 1];
        for (int alpha = 0; alpha <= byte.MaxValue; alpha++)
        {
            var brush = new SolidColorBrush(
                System.Windows.Media.Color.FromArgb(
                    (byte)alpha,
                    WarningColor.R,
                    WarningColor.G,
                    WarningColor.B));
            brush.Freeze();
            brushes[alpha] = brush;
        }

        return brushes;
    }
}

public enum WarningEdge
{
    Top,
    Right,
    Bottom,
    Left,
}

internal sealed class EdgeWarningFrame : FrameworkElement
{
    private static readonly System.Windows.Media.Color WarningColor =
        System.Windows.Media.Color.FromRgb(239, 68, 68);
    private static readonly SolidColorBrush[] WarningBrushes =
        CreateWarningBrushes();
    private double _gradientDepth;
    private double _virtualWidth;
    private double _virtualHeight;
    private double _offsetX;
    private double _offsetY;

    public double GradientDepth
    {
        get => _gradientDepth;
        set
        {
            double sanitized = double.IsFinite(value)
                ? Math.Max(0, value)
                : 0;
            if (Math.Abs(_gradientDepth - sanitized) < 0.01)
            {
                return;
            }

            _gradientDepth = sanitized;
            InvalidateVisual();
        }
    }

    public void ConfigureViewport(
        double virtualWidth,
        double virtualHeight,
        double offsetX,
        double offsetY)
    {
        _virtualWidth = virtualWidth;
        _virtualHeight = virtualHeight;
        _offsetX = offsetX;
        _offsetY = offsetY;
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext drawingContext)
    {
        base.OnRender(drawingContext);

        double virtualWidth = _virtualWidth > 0
            ? _virtualWidth
            : ActualWidth;
        double virtualHeight = _virtualHeight > 0
            ? _virtualHeight
            : ActualHeight;
        double depth = Math.Min(
            GradientDepth,
            Math.Min(virtualWidth, virtualHeight) / 2);
        if (depth <= 0 ||
            ActualWidth <= 0 ||
            ActualHeight <= 0 ||
            virtualWidth <= 0 ||
            virtualHeight <= 0)
        {
            return;
        }

        int bandCount = Math.Max(1, (int)Math.Ceiling(depth * 3));
        double bandWidth = depth / bandCount;
        for (int index = bandCount - 1; index >= 0; index--)
        {
            double normalizedDistance = (index + 0.5) / bandCount;
            double opacity =
                WarningGradientProfile.SampleOpacity(normalizedDistance);
            if (opacity <= 0.001)
            {
                continue;
            }

            byte alpha = (byte)Math.Round(opacity * byte.MaxValue);
            var pen = new System.Windows.Media.Pen(
                WarningBrushes[alpha],
                bandWidth + 0.08)
            {
                LineJoin = System.Windows.Media.PenLineJoin.Round,
            };
            pen.Freeze();

            double inset = (index * bandWidth) + (bandWidth / 2);
            double width = virtualWidth - (2 * inset);
            double height = virtualHeight - (2 * inset);
            if (width <= 0 || height <= 0)
            {
                continue;
            }

            drawingContext.DrawRectangle(
                null,
                pen,
                new Rect(
                    inset - _offsetX,
                    inset - _offsetY,
                    width,
                    height));
        }
    }

    private static SolidColorBrush[] CreateWarningBrushes()
    {
        var brushes = new SolidColorBrush[byte.MaxValue + 1];
        for (int alpha = 0; alpha <= byte.MaxValue; alpha++)
        {
            var brush = new SolidColorBrush(
                System.Windows.Media.Color.FromArgb(
                    (byte)alpha,
                    WarningColor.R,
                    WarningColor.G,
                    WarningColor.B));
            brush.Freeze();
            brushes[alpha] = brush;
        }

        return brushes;
    }
}
