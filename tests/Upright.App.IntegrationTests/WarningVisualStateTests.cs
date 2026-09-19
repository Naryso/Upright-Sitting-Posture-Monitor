namespace Upright.App.IntegrationTests;

public sealed class WarningVisualStateTests
{
    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, 0)]
    [InlineData(0.5, 0.5)]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    public void SeverityIsClamped(double input, double expected)
    {
        WarningVisualState visual = WarningVisualState.FromSeverity(input);

        Assert.Equal(expected, visual.Severity);
    }

    [Fact]
    public void PositiveSeverityProgressivelyRaisesOpacityAndBorder()
    {
        WarningVisualState low = WarningVisualState.FromSeverity(0.2);
        WarningVisualState high = WarningVisualState.FromSeverity(0.8);

        Assert.True(high.WindowOpacity > low.WindowOpacity);
        Assert.True(high.BorderThickness > low.BorderThickness);
    }

    [Fact]
    public void OpacityStartsContinuouslyWithoutAnImmediateJump()
    {
        WarningVisualState zero = WarningVisualState.FromSeverity(0);
        WarningVisualState firstFrame = WarningVisualState.FromSeverity(
            WarningSeverityTransition.Advance(
                0,
                1,
                TimeSpan.FromSeconds(1d / 60)));

        Assert.Equal(0, zero.WindowOpacity);
        Assert.InRange(firstFrame.WindowOpacity, 0, 0.01);
    }

    [Fact]
    public void WarningRisesSmoothlyOverAboutOneSecond()
    {
        double severity = 0;
        for (int frame = 0; frame < 60; frame++)
        {
            double next = WarningSeverityTransition.Advance(
                severity,
                1,
                TimeSpan.FromSeconds(1d / 60));
            Assert.InRange(next, severity, 1);
            severity = next;
        }

        Assert.InRange(severity, 0.88, 0.90);
    }

    [Fact]
    public void WarningFadesOutWithinOneSecond()
    {
        double severity = 1;
        for (int frame = 0; frame < 60; frame++)
        {
            severity = WarningSeverityTransition.Advance(
                severity,
                0,
                TimeSpan.FromSeconds(1d / 60));
        }

        Assert.Equal(0, severity);
    }

    [Fact]
    public void GradientProfileFadesMonotonicallyWithoutHardInnerEdge()
    {
        const int sampleCount = 256;
        double previous = WarningGradientProfile.SampleOpacity(0);
        double largestStep = 0;

        Assert.Equal(1, previous);
        for (int index = 1; index <= sampleCount; index++)
        {
            double current = WarningGradientProfile.SampleOpacity(
                index / (double)sampleCount);
            Assert.InRange(current, 0, previous);
            largestStep = Math.Max(largestStep, previous - current);
            previous = current;
        }

        Assert.Equal(0, previous);
        Assert.True(largestStep < 0.01);
        Assert.True(WarningGradientProfile.SampleOpacity(0.99) < 0.0001);
    }

    [Theory]
    [InlineData(-1, 1)]
    [InlineData(2, 0)]
    [InlineData(double.NaN, 1)]
    public void GradientProfileClampsDistance(double input, double expected)
    {
        double actual = WarningGradientProfile.SampleOpacity(input);

        Assert.Equal(expected, actual);
    }
}
