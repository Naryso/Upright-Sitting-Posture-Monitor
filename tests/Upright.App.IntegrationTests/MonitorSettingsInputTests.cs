namespace Upright.App.IntegrationTests;

public sealed class MonitorSettingsInputTests
{
    [Fact]
    public void AcceptsValuesWithinDocumentedRanges()
    {
        bool parsed = MonitorSettingsInput.TryParse(
            "0.04",
            "2.5",
            out MonitorSettingsValues? values,
            out string error);

        Assert.True(parsed, error);
        Assert.NotNull(values);
        Assert.Equal(0.04, values.DeadZone);
        Assert.Equal(2.5, values.WarningDelaySeconds);
    }

    [Theory]
    [InlineData("-0.01")]
    [InlineData("0.21")]
    [InlineData("not-a-number")]
    public void RejectsDeadZoneOutsideDocumentedRange(string input)
    {
        bool parsed = MonitorSettingsInput.TryParse(
            input,
            "0",
            out MonitorSettingsValues? values,
            out string error);

        Assert.False(parsed);
        Assert.Null(values);
        Assert.Contains("0.00 to 0.20", error, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("31")]
    [InlineData("not-a-number")]
    public void RejectsDelayOutsideDocumentedRange(string input)
    {
        bool parsed = MonitorSettingsInput.TryParse(
            "0.03",
            input,
            out MonitorSettingsValues? values,
            out string error);

        Assert.False(parsed);
        Assert.Null(values);
        Assert.Contains("0 to 30 seconds", error, StringComparison.Ordinal);
    }
}
