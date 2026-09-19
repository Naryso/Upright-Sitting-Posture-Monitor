using MediaColor = System.Windows.Media.Color;

namespace Upright.App;

public enum MonitorDisplayStatus
{
    ToolInactive,
    Calibrating,
    MonitoringActive,
    PoorPostureDetected,
}

public readonly record struct MonitorDisplayStatusPresentation(
    string Text,
    MediaColor DotColor,
    MediaColor TextColor)
{
    public static MonitorDisplayStatusPresentation From(
        MonitorDisplayStatus status) =>
        status switch
        {
            MonitorDisplayStatus.ToolInactive => new(
                "Tool inactive",
                MediaColor.FromRgb(152, 162, 179),
                MediaColor.FromRgb(71, 84, 103)),
            MonitorDisplayStatus.Calibrating => new(
                "Calibrating",
                MediaColor.FromRgb(23, 111, 229),
                MediaColor.FromRgb(21, 95, 189)),
            MonitorDisplayStatus.MonitoringActive => new(
                "Monitoring active",
                MediaColor.FromRgb(72, 183, 122),
                MediaColor.FromRgb(24, 121, 78)),
            MonitorDisplayStatus.PoorPostureDetected => new(
                "Poor posture detected",
                MediaColor.FromRgb(239, 68, 68),
                MediaColor.FromRgb(180, 35, 24)),
            _ => throw new ArgumentOutOfRangeException(
                nameof(status),
                status,
                "Unsupported monitor display status."),
        };
}
