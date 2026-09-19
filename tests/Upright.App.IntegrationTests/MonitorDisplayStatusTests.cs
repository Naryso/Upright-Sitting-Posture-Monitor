using Upright.App;
using MediaColor = System.Windows.Media.Color;

namespace Upright.App.IntegrationTests;

public sealed class MonitorDisplayStatusTests
{
    public static TheoryData<
        MonitorDisplayStatus,
        string,
        MediaColor,
        MediaColor> StatusCases =>
        new()
        {
            {
                MonitorDisplayStatus.ToolInactive,
                "Tool inactive",
                MediaColor.FromRgb(152, 162, 179),
                MediaColor.FromRgb(71, 84, 103)
            },
            {
                MonitorDisplayStatus.Calibrating,
                "Calibrating",
                MediaColor.FromRgb(23, 111, 229),
                MediaColor.FromRgb(21, 95, 189)
            },
            {
                MonitorDisplayStatus.MonitoringActive,
                "Monitoring active",
                MediaColor.FromRgb(72, 183, 122),
                MediaColor.FromRgb(24, 121, 78)
            },
            {
                MonitorDisplayStatus.PoorPostureDetected,
                "Poor posture detected",
                MediaColor.FromRgb(239, 68, 68),
                MediaColor.FromRgb(180, 35, 24)
            },
        };

    [Theory]
    [MemberData(nameof(StatusCases))]
    public void Presentation_MapsEachStatusToEnglishTextAndColors(
        MonitorDisplayStatus status,
        string expectedText,
        MediaColor expectedDotColor,
        MediaColor expectedTextColor)
    {
        MonitorDisplayStatusPresentation presentation =
            MonitorDisplayStatusPresentation.From(status);

        Assert.Equal(expectedText, presentation.Text);
        Assert.Equal(expectedDotColor, presentation.DotColor);
        Assert.Equal(expectedTextColor, presentation.TextColor);
    }
}
