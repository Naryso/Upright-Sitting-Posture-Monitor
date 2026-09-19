namespace Upright.Core.Tests;

public sealed class CameraCalibrationTests
{
    [Fact]
    public void CreateRequiresFourSamples()
    {
        CameraCalibrationData? result = CameraCalibration.Create(
            [
                new(0.6, 0.2),
                new(0.5, 0.2),
                new(0.4, 0.2),
            ],
            "camera");

        Assert.Null(result);
    }

    [Fact]
    public void CreateMatchesSwiftAggregation()
    {
        CameraCalibrationData? result = CameraCalibration.Create(
            [
                new(0.60, 0.20),
                new(0.55, 0.25),
                new(0.50, 0.22),
                new(0.45, 0.24),
            ],
            "camera-1");

        Assert.NotNull(result);
        Assert.Equal(0.60, result.GoodPostureY, 10);
        Assert.Equal(0.45, result.BadPostureY, 10);
        Assert.Equal(0.525, result.NeutralY, 10);
        Assert.Equal(0.15, result.PostureRange, 10);
        Assert.Equal(0.25, result.NeutralFaceWidth, 10);
        Assert.Equal("camera-1", result.CameraId);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateUsesZeroWhenFaceWidthsAreMissing()
    {
        CameraCalibrationData? result = CameraCalibration.Create(
            [
                new(0.60, null),
                new(0.55, null),
                new(0.50, null),
                new(0.45, null),
            ],
            "camera");

        Assert.NotNull(result);
        Assert.Equal(0, result.NeutralFaceWidth);
    }

    [Fact]
    public void EmptyCameraIdCreatesAnInvalidCalibration()
    {
        CameraCalibrationData? result = CameraCalibration.Create(
            [
                new(0.60, null),
                new(0.55, null),
                new(0.50, null),
                new(0.45, null),
            ],
            null);

        Assert.NotNull(result);
        Assert.Equal(string.Empty, result.CameraId);
        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData(0.01, false)]
    [InlineData(0.0100001, true)]
    public void ValidityRequiresRangeStrictlyGreaterThanPointZeroOne(
        double range,
        bool expected)
    {
        var calibration = new CameraCalibrationData(
            0.6,
            0.6 - range,
            0.5,
            range,
            "camera");

        Assert.Equal(expected, calibration.IsValid);
    }
}
