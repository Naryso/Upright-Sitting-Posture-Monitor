namespace Upright.App.IntegrationTests;

public sealed class CalibrationTargetLayoutTests
{
    [Theory]
    [InlineData(0, 64, 64)]
    [InlineData(1, 1744, 64)]
    [InlineData(2, 1744, 904)]
    [InlineData(3, 64, 904)]
    public void UsesDisplayCornersInClockwiseOrder(
        int step,
        double expectedX,
        double expectedY)
    {
        CalibrationTargetPosition position =
            CalibrationTargetLayout.Calculate(1920, 1080, step);

        Assert.Equal(expectedX, position.X);
        Assert.Equal(expectedY, position.Y);
    }

    [Fact]
    public void KeepsTargetVisibleOnSmallDisplays()
    {
        CalibrationTargetPosition position =
            CalibrationTargetLayout.Calculate(180, 180, 2);

        Assert.InRange(position.X, 24, 44);
        Assert.InRange(position.Y, 24, 44);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public void RejectsUnknownCalibrationStep(int step)
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CalibrationTargetLayout.Calculate(1920, 1080, step));
    }
}
