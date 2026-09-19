namespace Upright.Core;

public readonly record struct PostureReading(
    DateTimeOffset Timestamp,
    bool IsBadPosture,
    double Severity)
{
    public static PostureReading Good(DateTimeOffset timestamp) => new(timestamp, false, 0);
}

/// <summary>
/// Stateful, platform-neutral reproduction of the camera detector's smoothing,
/// hysteresis, vertical-slouch, and forward-head calculations.
/// </summary>
public sealed class PostureEvaluator
{
    public const int SmoothingWindow = 5;
    public const double ExitThresholdRatio = 0.7;

    private readonly Queue<double> _noseYHistory = new(SmoothingWindow);
    private CameraCalibrationData _calibration;
    private double _deadZone;
    private bool _isCurrentlySlouching;

    public PostureEvaluator(CameraCalibrationData calibration, double deadZone)
    {
        _calibration = calibration ?? throw new ArgumentNullException(nameof(calibration));
        _deadZone = deadZone;
    }

    public bool IsCurrentlySlouching => _isCurrentlySlouching;

    public void Reset(CameraCalibrationData calibration, double deadZone)
    {
        _calibration = calibration ?? throw new ArgumentNullException(nameof(calibration));
        _deadZone = deadZone;
        _noseYHistory.Clear();
        _isCurrentlySlouching = false;
    }

    public void UpdateDeadZone(double deadZone)
    {
        _deadZone = deadZone;
    }

    /// <summary>
    /// Evaluates one accepted body or face observation. A null face width is
    /// intentionally evaluated as zero, matching the body-pose path in Swift.
    /// </summary>
    public PostureReading Evaluate(
        double noseY,
        double? faceWidth,
        DateTimeOffset timestamp)
    {
        double smoothedY = SmoothNoseY(noseY);
        double slouchAmount = _calibration.BadPostureY - smoothedY;
        double deadZoneThreshold = _deadZone * _calibration.PostureRange;

        double entryThreshold = deadZoneThreshold;
        double exitThreshold = deadZoneThreshold * ExitThresholdRatio;
        double threshold = _isCurrentlySlouching ? exitThreshold : entryThreshold;

        bool isBadPosture = slouchAmount > threshold;

        double currentFaceWidth = faceWidth ?? 0;
        double forwardHeadThreshold =
            1 + Math.Max(CameraCalibrationData.ForwardHeadBaseThreshold, _deadZone);
        double forwardHeadSeverity = 0;

        if (_calibration.NeutralFaceWidth > 0 && currentFaceWidth > 0)
        {
            double ratio = currentFaceWidth / _calibration.NeutralFaceWidth;
            if (ratio > forwardHeadThreshold)
            {
                isBadPosture = true;
                double sizeExcess = ratio - forwardHeadThreshold;
                forwardHeadSeverity = Math.Clamp(
                    sizeExcess / CameraCalibrationData.ForwardHeadSeverityRange,
                    0,
                    1);
            }
        }

        double severity = 0;
        if (isBadPosture)
        {
            double pastDeadZone = slouchAmount - deadZoneThreshold;
            double remainingRange = Math.Max(0.01, _calibration.PostureRange - deadZoneThreshold);
            double verticalSeverity = Math.Clamp(pastDeadZone / remainingRange, 0, 1);
            severity = Math.Max(verticalSeverity, forwardHeadSeverity);

            if (forwardHeadSeverity > 0 &&
                severity < CameraCalibrationData.ForwardHeadMinimumSeverity)
            {
                severity = CameraCalibrationData.ForwardHeadMinimumSeverity;
            }
        }

        if (isBadPosture)
        {
            _isCurrentlySlouching = true;
        }
        else if (severity == 0)
        {
            _isCurrentlySlouching = false;
        }

        return new PostureReading(timestamp, isBadPosture, severity);
    }

    private double SmoothNoseY(double rawY)
    {
        _noseYHistory.Enqueue(rawY);
        if (_noseYHistory.Count > SmoothingWindow)
        {
            _noseYHistory.Dequeue();
        }

        return _noseYHistory.Average();
    }
}
