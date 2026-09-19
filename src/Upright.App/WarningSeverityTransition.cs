namespace Upright.App;

public static class WarningSeverityTransition
{
    public const double RiseTimeConstantSeconds = 0.45;
    public const double FallTimeConstantSeconds = 0.18;
    public const double SettledTolerance = 0.005;

    public static double Advance(
        double current,
        double target,
        TimeSpan elapsed)
    {
        double clampedCurrent = ClampFinite(current);
        double clampedTarget = ClampFinite(target);
        double elapsedSeconds = Math.Max(0, elapsed.TotalSeconds);
        if (elapsedSeconds == 0 ||
            Math.Abs(clampedTarget - clampedCurrent) <= SettledTolerance)
        {
            return clampedTarget;
        }

        double timeConstant = clampedTarget > clampedCurrent
            ? RiseTimeConstantSeconds
            : FallTimeConstantSeconds;
        double blend = 1 - Math.Exp(-elapsedSeconds / timeConstant);
        double next =
            clampedCurrent + ((clampedTarget - clampedCurrent) * blend);
        return Math.Abs(clampedTarget - next) <= SettledTolerance
            ? clampedTarget
            : next;
    }

    private static double ClampFinite(double value) =>
        double.IsFinite(value) ? Math.Clamp(value, 0, 1) : 0;
}
