namespace Upright.Core.Tests;

public sealed class PostureEvaluatorTests
{
    private static readonly DateTimeOffset Timestamp =
        new(2026, 7, 25, 0, 0, 0, TimeSpan.Zero);

    private static CameraCalibrationData Calibration(
        double neutralFaceWidth = 0.20) =>
        new(
            GoodPostureY: 0.60,
            BadPostureY: 0.40,
            NeutralY: 0.50,
            PostureRange: 0.20,
            CameraId: "camera",
            NeutralFaceWidth: neutralFaceWidth);

    [Fact]
    public void VerticalPositionBelowThresholdIsBad()
    {
        var evaluator = new PostureEvaluator(Calibration(), deadZone: 0.03);

        PostureReading reading = evaluator.Evaluate(0.38, null, Timestamp);

        Assert.True(reading.IsBadPosture);
        Assert.Equal((0.02 - 0.006) / (0.20 - 0.006), reading.Severity, 10);
    }

    [Fact]
    public void PositionJustInsideEntryThresholdIsGood()
    {
        var evaluator = new PostureEvaluator(Calibration(), deadZone: 0.03);

        // Avoid treating a decimal-to-binary rounding artifact as detector behavior.
        PostureReading reading = evaluator.Evaluate(0.394001, null, Timestamp);

        Assert.False(reading.IsBadPosture);
        Assert.Equal(0, reading.Severity);
    }

    [Fact]
    public void ExitThresholdIsSeventyPercentOfEntryThreshold()
    {
        var evaluator = new PostureEvaluator(Calibration(), deadZone: 0.03);
        Assert.True(evaluator.Evaluate(0.38, null, Timestamp).IsBadPosture);

        // Reset the five-value average while preserving detector hysteresis.
        for (int index = 0; index < PostureEvaluator.SmoothingWindow; index++)
        {
            evaluator.Evaluate(0.395, null, Timestamp.AddMilliseconds(index + 1));
        }

        // slouchAmount=0.005; entry threshold=0.006 but exit threshold=0.0042.
        PostureReading reading = evaluator.Evaluate(0.395, null, Timestamp.AddSeconds(1));

        Assert.True(reading.IsBadPosture);
    }

    [Fact]
    public void FiveSampleMovingAverageMatchesSwift()
    {
        var evaluator = new PostureEvaluator(Calibration(), deadZone: 0);

        evaluator.Evaluate(0.60, null, Timestamp);
        evaluator.Evaluate(0.55, null, Timestamp);
        evaluator.Evaluate(0.50, null, Timestamp);
        evaluator.Evaluate(0.45, null, Timestamp);
        PostureReading fifth = evaluator.Evaluate(0.30, null, Timestamp);

        // Average is 0.48, still above badPostureY=0.40.
        Assert.False(fifth.IsBadPosture);

        PostureReading sixth = evaluator.Evaluate(0.30, null, Timestamp);
        // Oldest 0.60 is removed. Average becomes 0.42.
        Assert.False(sixth.IsBadPosture);
    }

    [Fact]
    public void ForwardHeadUsesFaceWidthRatio()
    {
        var evaluator = new PostureEvaluator(Calibration(), deadZone: 0.03);

        PostureReading reading = evaluator.Evaluate(
            noseY: 0.60,
            faceWidth: 0.23,
            timestamp: Timestamp);

        Assert.True(reading.IsBadPosture);
        Assert.Equal(2.0 / 3.0, reading.Severity, 10);
    }

    [Fact]
    public void SmallForwardHeadSeverityHasMinimumPointFive()
    {
        var evaluator = new PostureEvaluator(Calibration(), deadZone: 0.03);

        PostureReading reading = evaluator.Evaluate(
            noseY: 0.60,
            faceWidth: 0.211,
            timestamp: Timestamp);

        Assert.True(reading.IsBadPosture);
        Assert.Equal(0.5, reading.Severity);
    }

    [Fact]
    public void MissingFaceWidthDoesNotReuseAWidthForEvaluation()
    {
        var evaluator = new PostureEvaluator(Calibration(), deadZone: 0.03);
        Assert.True(evaluator.Evaluate(0.60, 0.23, Timestamp).IsBadPosture);

        PostureReading bodyReading = evaluator.Evaluate(
            0.60,
            faceWidth: null,
            Timestamp.AddMilliseconds(100));

        Assert.False(bodyReading.IsBadPosture);
    }

    [Fact]
    public void ResetClearsMovingAverageAndHysteresis()
    {
        var evaluator = new PostureEvaluator(Calibration(), deadZone: 0.03);
        Assert.True(evaluator.Evaluate(0.38, null, Timestamp).IsBadPosture);
        Assert.True(evaluator.IsCurrentlySlouching);

        evaluator.Reset(Calibration(), deadZone: 0.03);
        PostureReading reading = evaluator.Evaluate(0.60, null, Timestamp);

        Assert.False(reading.IsBadPosture);
        Assert.False(evaluator.IsCurrentlySlouching);
    }
}
