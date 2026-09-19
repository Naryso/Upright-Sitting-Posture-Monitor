namespace Upright.App;

public readonly record struct CalibrationTargetPosition(double X, double Y);

public static class CalibrationTargetLayout
{
    public const double TargetSize = 112;
    public const double EdgeInset = 120;
    private const double MinimumEdgeMargin = 24;

    public static CalibrationTargetPosition Calculate(
        double width,
        double height,
        int step)
    {
        if (step is < 0 or > 3)
        {
            throw new ArgumentOutOfRangeException(nameof(step));
        }

        double left = Math.Clamp(
            EdgeInset - (TargetSize / 2),
            MinimumEdgeMargin,
            Math.Max(MinimumEdgeMargin, width - TargetSize - MinimumEdgeMargin));
        double right = Math.Clamp(
            width - EdgeInset - (TargetSize / 2),
            MinimumEdgeMargin,
            Math.Max(MinimumEdgeMargin, width - TargetSize - MinimumEdgeMargin));
        double top = Math.Clamp(
            EdgeInset - (TargetSize / 2),
            MinimumEdgeMargin,
            Math.Max(MinimumEdgeMargin, height - TargetSize - MinimumEdgeMargin));
        double bottom = Math.Clamp(
            height - EdgeInset - (TargetSize / 2),
            MinimumEdgeMargin,
            Math.Max(MinimumEdgeMargin, height - TargetSize - MinimumEdgeMargin));

        return step switch
        {
            0 => new CalibrationTargetPosition(left, top),
            1 => new CalibrationTargetPosition(right, top),
            2 => new CalibrationTargetPosition(right, bottom),
            _ => new CalibrationTargetPosition(left, bottom),
        };
    }
}
