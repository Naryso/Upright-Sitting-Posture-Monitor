namespace Upright.Core;

public sealed record PostureMonitoringState
{
    public int ConsecutiveBadFrames { get; init; }
    public int ConsecutiveGoodFrames { get; init; }
    public bool IsCurrentlySlouching { get; init; }
    public bool IsCurrentlyAway { get; init; }
    public DateTimeOffset? BadPostureStartTime { get; init; }
    public double PostureWarningIntensity { get; init; }

    public static PostureMonitoringState Empty { get; } = new();
}

public sealed record PostureConfig(
    int FrameThreshold = 8,
    int GoodFrameThreshold = 5,
    TimeSpan WarningOnsetDelay = default,
    double Intensity = 1);

public abstract record PostureEngineEffect
{
    private PostureEngineEffect()
    {
    }

    public sealed record UpdateUi : PostureEngineEffect;
    public sealed record UpdateWarning : PostureEngineEffect;
    public sealed record RecordSlouchEvent : PostureEngineEffect;
    public sealed record TrackAnalytics(TimeSpan Interval, bool IsSlouching) : PostureEngineEffect;
}

public sealed record PostureReadingResult(
    PostureMonitoringState NewState,
    IReadOnlyList<PostureEngineEffect> Effects);

public sealed record AwayChangeResult(
    PostureMonitoringState NewState,
    bool ShouldUpdateUi);

public static class PostureEngine
{
    public const int DefaultBadFrameThreshold = 8;
    public const int DefaultGoodFrameThreshold = 5;
    public static readonly TimeSpan BaseFrameInterval = TimeSpan.FromSeconds(0.25);
    public static readonly TimeSpan SlouchingFrameInterval = TimeSpan.FromSeconds(0.1);

    /// <summary>
    /// Pure reproduction of PostureEngine.processReading in the Swift app.
    /// </summary>
    public static PostureReadingResult ProcessReading(
        PostureReading reading,
        PostureMonitoringState state,
        PostureConfig config,
        DateTimeOffset currentTime,
        TimeSpan frameInterval)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(config);

        var effects = new List<PostureEngineEffect>
        {
            new PostureEngineEffect.TrackAnalytics(
                frameInterval,
                state.IsCurrentlySlouching),
        };

        PostureMonitoringState newState;

        if (reading.IsBadPosture)
        {
            newState = state with
            {
                ConsecutiveBadFrames = state.ConsecutiveBadFrames + 1,
                ConsecutiveGoodFrames = 0,
            };

            if (newState.ConsecutiveBadFrames >= config.FrameThreshold)
            {
                DateTimeOffset badStartTime = newState.BadPostureStartTime ?? currentTime;
                newState = newState with { BadPostureStartTime = badStartTime };

                TimeSpan onsetDelay = config.WarningOnsetDelay < TimeSpan.Zero
                    ? TimeSpan.Zero
                    : config.WarningOnsetDelay;
                TimeSpan elapsed = currentTime - badStartTime;

                if (elapsed >= onsetDelay)
                {
                    if (!newState.IsCurrentlySlouching)
                    {
                        newState = newState with { IsCurrentlySlouching = true };
                        effects.Add(new PostureEngineEffect.RecordSlouchEvent());
                        effects.Add(new PostureEngineEffect.UpdateUi());
                    }

                    double intensity = config.Intensity > 0 ? config.Intensity : 1;
                    double clampedSeverity = Math.Clamp(reading.Severity, 0, 1);
                    double adjustedSeverity = Math.Pow(clampedSeverity, 1 / intensity);
                    newState = newState with { PostureWarningIntensity = adjustedSeverity };
                }
            }
        }
        else
        {
            newState = state with
            {
                ConsecutiveGoodFrames = state.ConsecutiveGoodFrames + 1,
                ConsecutiveBadFrames = 0,
                BadPostureStartTime = null,
                PostureWarningIntensity = 0,
            };

            if (newState.ConsecutiveGoodFrames >= config.GoodFrameThreshold &&
                newState.IsCurrentlySlouching)
            {
                newState = newState with { IsCurrentlySlouching = false };
                effects.Add(new PostureEngineEffect.UpdateUi());
            }
        }

        effects.Add(new PostureEngineEffect.UpdateWarning());
        return new PostureReadingResult(newState, effects);
    }

    public static AwayChangeResult ProcessAwayChange(
        bool isAway,
        PostureMonitoringState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        if (isAway == state.IsCurrentlyAway)
        {
            return new AwayChangeResult(state, ShouldUpdateUi: false);
        }

        return new AwayChangeResult(
            state with { IsCurrentlyAway = isAway },
            ShouldUpdateUi: true);
    }

    public static PostureMonitoringState Reset(PostureMonitoringState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return PostureMonitoringState.Empty;
    }
}
