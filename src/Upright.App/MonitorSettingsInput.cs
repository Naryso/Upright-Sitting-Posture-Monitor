using System.Globalization;

namespace Upright.App;

public sealed record MonitorSettingsValues(
    double DeadZone,
    double WarningDelaySeconds);

public static class MonitorSettingsInput
{
    public const double DefaultDeadZone = 0.03;
    public const double MinimumDeadZone = 0;
    public const double MaximumDeadZone = 0.20;

    public const double DefaultWarningDelaySeconds = 0;
    public const double MinimumWarningDelaySeconds = 0;
    public const double MaximumWarningDelaySeconds = 30;

    public static bool TryParse(
        string deadZoneText,
        string warningDelayText,
        out MonitorSettingsValues? values,
        out string error)
    {
        if (!TryParseNumber(deadZoneText, out double deadZone) ||
            deadZone < MinimumDeadZone ||
            deadZone > MaximumDeadZone)
        {
            values = null;
            error = "Dead zone must be a number from 0.00 to 0.20.";
            return false;
        }

        if (!TryParseNumber(warningDelayText, out double warningDelay) ||
            warningDelay < MinimumWarningDelaySeconds ||
            warningDelay > MaximumWarningDelaySeconds)
        {
            values = null;
            error = "Alert delay must be a number from 0 to 30 seconds.";
            return false;
        }

        values = new MonitorSettingsValues(deadZone, warningDelay);
        error = string.Empty;
        return true;
    }

    private static bool TryParseNumber(string text, out double value)
    {
        const NumberStyles style = NumberStyles.Float;
        bool parsed =
            double.TryParse(text, style, CultureInfo.CurrentCulture, out value) ||
            double.TryParse(text, style, CultureInfo.InvariantCulture, out value);
        return parsed && double.IsFinite(value);
    }
}
