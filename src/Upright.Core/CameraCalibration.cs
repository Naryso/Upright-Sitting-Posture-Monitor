namespace Upright.Core;

/// <summary>
/// A normalized vertical position and optional normalized face width captured
/// during camera calibration. Coordinates use an origin at the bottom-left,
/// matching Apple Vision.
/// </summary>
public readonly record struct CameraCalibrationSample(double NoseY, double? FaceWidth);

/// <summary>
/// Camera-specific calibration values used by the original Upright detector.
/// </summary>
public sealed record CameraCalibrationData(
    double GoodPostureY,
    double BadPostureY,
    double NeutralY,
    double PostureRange,
    string CameraId,
    double NeutralFaceWidth = 0)
{
    public const double ForwardHeadBaseThreshold = 0.05;
    public const double ForwardHeadSeverityRange = 0.15;
    public const double ForwardHeadMinimumSeverity = 0.5;

    public bool IsValid => PostureRange > 0.01 && !string.IsNullOrEmpty(CameraId);
}

public static class CameraCalibration
{
    public const int MinimumSampleCount = 4;

    /// <summary>
    /// Reproduces CameraPostureDetector.createCalibrationData(from:).
    /// </summary>
    public static CameraCalibrationData? Create(
        IEnumerable<CameraCalibrationSample> samples,
        string? cameraId)
    {
        ArgumentNullException.ThrowIfNull(samples);

        CameraCalibrationSample[] values = samples.ToArray();
        if (values.Length < MinimumSampleCount)
        {
            return null;
        }

        double maximumY = values.Max(sample => sample.NoseY);
        double minimumY = values.Min(sample => sample.NoseY);
        double averageY = values.Average(sample => sample.NoseY);
        double postureRange = Math.Abs(maximumY - minimumY);
        double neutralFaceWidth = values
            .Where(sample => sample.FaceWidth.HasValue)
            .Select(sample => sample.FaceWidth!.Value)
            .DefaultIfEmpty(0)
            .Max();

        return new CameraCalibrationData(
            GoodPostureY: maximumY,
            BadPostureY: minimumY,
            NeutralY: averageY,
            PostureRange: postureRange,
            CameraId: cameraId ?? string.Empty,
            NeutralFaceWidth: neutralFaceWidth);
    }
}
