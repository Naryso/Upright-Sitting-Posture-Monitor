namespace Upright.Core;

public readonly record struct AwayTransition(bool IsAway, bool StateChanged);

/// <summary>
/// Preserves the camera detector's fifteen-frame no-detection threshold.
/// </summary>
public sealed class AwayTracker
{
    public const int DefaultFrameThreshold = 15;

    private readonly int _frameThreshold;
    private int _consecutiveNoDetectionFrames;

    public AwayTracker(bool enabled, int frameThreshold = DefaultFrameThreshold)
    {
        if (frameThreshold <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(frameThreshold),
                "The away frame threshold must be positive.");
        }

        Enabled = enabled;
        _frameThreshold = frameThreshold;
    }

    public bool Enabled { get; set; }

    public bool IsAway { get; private set; }

    public AwayTransition HandleDetection()
    {
        _consecutiveNoDetectionFrames = 0;
        if (Enabled && IsAway)
        {
            IsAway = false;
            return new AwayTransition(IsAway: false, StateChanged: true);
        }

        return new AwayTransition(IsAway, StateChanged: false);
    }

    public AwayTransition HandleNoDetection()
    {
        if (!Enabled)
        {
            return new AwayTransition(IsAway, StateChanged: false);
        }

        _consecutiveNoDetectionFrames++;
        if (_consecutiveNoDetectionFrames >= _frameThreshold && !IsAway)
        {
            IsAway = true;
            return new AwayTransition(IsAway: true, StateChanged: true);
        }

        return new AwayTransition(IsAway, StateChanged: false);
    }

    public void Reset()
    {
        _consecutiveNoDetectionFrames = 0;
        IsAway = false;
    }
}
