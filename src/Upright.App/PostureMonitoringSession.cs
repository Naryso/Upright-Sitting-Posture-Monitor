using Upright.Core;
using Upright.Vision;

namespace Upright.App;

public sealed record MonitoringUpdate(
    PostureMonitoringState State,
    PostureReading? Reading,
    IReadOnlyList<PostureEngineEffect> Effects,
    TimeSpan NextProcessingInterval,
    bool AwayStateChanged);

public sealed class PostureMonitoringSession
{
    private readonly PostureEvaluator _evaluator;
    private readonly AwayTracker _awayTracker;
    private readonly PostureConfig _config;
    private PostureMonitoringState _state = PostureMonitoringState.Empty;

    public PostureMonitoringSession(
        CameraCalibrationData calibration,
        double deadZone,
        PostureConfig config,
        bool awayEnabled)
    {
        ArgumentNullException.ThrowIfNull(calibration);
        ArgumentNullException.ThrowIfNull(config);
        _evaluator = new PostureEvaluator(calibration, deadZone);
        _awayTracker = new AwayTracker(awayEnabled);
        _config = config;
    }

    public PostureMonitoringState State => _state;

    public TimeSpan ProcessingInterval =>
        _evaluator.IsCurrentlySlouching
            ? PostureEngine.SlouchingFrameInterval
            : PostureEngine.BaseFrameInterval;

    public MonitoringUpdate Process(
        VisionDecision decision,
        DateTimeOffset currentTime)
    {
        if (decision.Kind == VisionDecisionKind.BodyWithoutUsableNose)
        {
            return Unchanged();
        }

        if (decision.Kind == VisionDecisionKind.NoDetection)
        {
            AwayTransition transition = _awayTracker.HandleNoDetection();
            if (transition.StateChanged)
            {
                _state = PostureEngine
                    .ProcessAwayChange(transition.IsAway, _state)
                    .NewState;
            }

            return new MonitoringUpdate(
                _state,
                Reading: null,
                Effects: [],
                ProcessingInterval,
                AwayStateChanged: transition.StateChanged);
        }

        AwayTransition detection = _awayTracker.HandleDetection();
        if (detection.StateChanged)
        {
            _state = PostureEngine
                .ProcessAwayChange(detection.IsAway, _state)
                .NewState;
        }

        PostureReading reading = _evaluator.Evaluate(
            decision.NoseYFromBottom,
            decision.FaceWidth,
            currentTime);
        PostureReadingResult result = PostureEngine.ProcessReading(
            reading,
            _state,
            _config,
            currentTime,
            ProcessingInterval);
        _state = result.NewState;

        return new MonitoringUpdate(
            _state,
            reading,
            result.Effects,
            ProcessingInterval,
            AwayStateChanged: detection.StateChanged);
    }

    public void Reset()
    {
        _state = PostureMonitoringState.Empty;
        _awayTracker.Reset();
    }

    private MonitoringUpdate Unchanged() =>
        new(
            _state,
            Reading: null,
            Effects: [],
            ProcessingInterval,
            AwayStateChanged: false);
}
