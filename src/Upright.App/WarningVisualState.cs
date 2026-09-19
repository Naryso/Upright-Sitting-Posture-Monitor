namespace Upright.App;

public readonly record struct WarningVisualState(
    double Severity,
    double WindowOpacity,
    double BorderThickness)
{
    public static WarningVisualState FromSeverity(double severity)
    {
        double clamped = Math.Clamp(severity, 0, 1);
        double eased = clamped * clamped * (3 - (2 * clamped));
        return new WarningVisualState(
            clamped,
            WindowOpacity: 0.60 * eased,
            BorderThickness: 18 + (clamped * 54));
    }
}
