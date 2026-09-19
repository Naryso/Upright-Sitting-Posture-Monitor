namespace Upright.Core.Tests;

public sealed class PostureEngineTests
{
    private static readonly DateTimeOffset Now =
        new(2026, 7, 25, 12, 0, 0, TimeSpan.Zero);

    private static PostureReading Reading(bool bad, double severity = 0.5) =>
        new(Now, bad, severity);

    [Fact]
    public void GoodReadingResetsBadState()
    {
        var state = new PostureMonitoringState
        {
            ConsecutiveBadFrames = 5,
            BadPostureStartTime = Now.AddSeconds(-1),
            PostureWarningIntensity = 0.8,
        };

        PostureReadingResult result = Process(Reading(false), state);

        Assert.Equal(0, result.NewState.ConsecutiveBadFrames);
        Assert.Equal(1, result.NewState.ConsecutiveGoodFrames);
        Assert.Null(result.NewState.BadPostureStartTime);
        Assert.Equal(0, result.NewState.PostureWarningIntensity);
    }

    [Fact]
    public void BadReadingIncrementsBadAndClearsGood()
    {
        var state = new PostureMonitoringState
        {
            ConsecutiveBadFrames = 3,
            ConsecutiveGoodFrames = 2,
        };

        PostureReadingResult result = Process(Reading(true), state);

        Assert.Equal(4, result.NewState.ConsecutiveBadFrames);
        Assert.Equal(0, result.NewState.ConsecutiveGoodFrames);
    }

    [Fact]
    public void EighthBadReadingEntersWarning()
    {
        var state = new PostureMonitoringState
        {
            ConsecutiveBadFrames = 7,
            BadPostureStartTime = Now.AddSeconds(-1),
        };

        PostureReadingResult result = Process(Reading(true, 0.8), state);

        Assert.True(result.NewState.IsCurrentlySlouching);
        Assert.Equal(0.8, result.NewState.PostureWarningIntensity, 10);
        Assert.Contains(result.Effects, effect => effect is PostureEngineEffect.RecordSlouchEvent);
        Assert.Contains(result.Effects, effect => effect is PostureEngineEffect.UpdateUi);
    }

    [Fact]
    public void WarningTransitionIsNotRecordedTwice()
    {
        var state = new PostureMonitoringState
        {
            ConsecutiveBadFrames = 10,
            IsCurrentlySlouching = true,
            BadPostureStartTime = Now.AddSeconds(-5),
        };

        PostureReadingResult result = Process(Reading(true), state);

        Assert.DoesNotContain(
            result.Effects,
            effect => effect is PostureEngineEffect.RecordSlouchEvent);
        Assert.DoesNotContain(
            result.Effects,
            effect => effect is PostureEngineEffect.UpdateUi);
    }

    [Fact]
    public void OnsetDelayDefersWarning()
    {
        var state = new PostureMonitoringState
        {
            ConsecutiveBadFrames = 7,
            BadPostureStartTime = Now,
        };
        var config = new PostureConfig(WarningOnsetDelay: TimeSpan.FromSeconds(2));

        PostureReadingResult result = Process(Reading(true), state, config, Now);

        Assert.False(result.NewState.IsCurrentlySlouching);
        Assert.DoesNotContain(
            result.Effects,
            effect => effect is PostureEngineEffect.RecordSlouchEvent);
    }

    [Fact]
    public void OnsetDelayAllowsWarningAfterElapsedTime()
    {
        var state = new PostureMonitoringState
        {
            ConsecutiveBadFrames = 7,
            BadPostureStartTime = Now.AddSeconds(-3),
        };
        var config = new PostureConfig(WarningOnsetDelay: TimeSpan.FromSeconds(2));

        PostureReadingResult result = Process(Reading(true), state, config, Now);

        Assert.True(result.NewState.IsCurrentlySlouching);
    }

    [Theory]
    [InlineData(2.0, 1.0)]
    [InlineData(-1.0, 0.0)]
    public void SeverityIsClamped(double severity, double expected)
    {
        var state = new PostureMonitoringState
        {
            ConsecutiveBadFrames = 7,
        };

        PostureReadingResult result = Process(Reading(true, severity), state);

        Assert.Equal(expected, result.NewState.PostureWarningIntensity, 10);
    }

    [Fact]
    public void NonPositiveIntensityUsesOne()
    {
        var state = new PostureMonitoringState
        {
            ConsecutiveBadFrames = 7,
        };
        var config = new PostureConfig(Intensity: 0);

        PostureReadingResult result = Process(Reading(true, 0.8), state, config);

        Assert.Equal(0.8, result.NewState.PostureWarningIntensity, 10);
    }

    [Fact]
    public void PositiveIntensityAppliesPowerTransform()
    {
        var state = new PostureMonitoringState
        {
            ConsecutiveBadFrames = 7,
        };
        var config = new PostureConfig(Intensity: 2);

        PostureReadingResult result = Process(Reading(true, 0.25), state, config);

        Assert.Equal(0.5, result.NewState.PostureWarningIntensity, 10);
    }

    [Fact]
    public void FifthGoodReadingRecovers()
    {
        var state = new PostureMonitoringState
        {
            ConsecutiveGoodFrames = 4,
            IsCurrentlySlouching = true,
            PostureWarningIntensity = 0.8,
        };

        PostureReadingResult result = Process(Reading(false), state);

        Assert.False(result.NewState.IsCurrentlySlouching);
        Assert.Contains(result.Effects, effect => effect is PostureEngineEffect.UpdateUi);
    }

    [Fact]
    public void FourthGoodReadingDoesNotRecover()
    {
        var state = new PostureMonitoringState
        {
            ConsecutiveGoodFrames = 3,
            IsCurrentlySlouching = true,
        };

        PostureReadingResult result = Process(Reading(false), state);

        Assert.True(result.NewState.IsCurrentlySlouching);
        Assert.DoesNotContain(
            result.Effects,
            effect => effect is PostureEngineEffect.UpdateUi);
    }

    [Fact]
    public void EveryReadingRequestsAnalyticsAndWarningUpdate()
    {
        var state = new PostureMonitoringState();

        PostureReadingResult result = Process(Reading(false), state);

        var analytics = Assert.Single(
            result.Effects.OfType<PostureEngineEffect.TrackAnalytics>());
        Assert.Equal(PostureEngine.BaseFrameInterval, analytics.Interval);
        Assert.False(analytics.IsSlouching);
        Assert.Contains(result.Effects, effect => effect is PostureEngineEffect.UpdateWarning);
    }

    [Fact]
    public void AnalyticsUsesStateBeforeCurrentReading()
    {
        var state = new PostureMonitoringState
        {
            ConsecutiveBadFrames = 7,
            BadPostureStartTime = Now.AddSeconds(-1),
        };

        PostureReadingResult result = Process(Reading(true), state);

        var analytics = Assert.Single(
            result.Effects.OfType<PostureEngineEffect.TrackAnalytics>());
        Assert.False(analytics.IsSlouching);
        Assert.True(result.NewState.IsCurrentlySlouching);
    }

    [Fact]
    public void AwayChangeOnlyRequestsUiOnTransition()
    {
        var state = new PostureMonitoringState();

        AwayChangeResult away = PostureEngine.ProcessAwayChange(true, state);
        AwayChangeResult unchanged = PostureEngine.ProcessAwayChange(true, away.NewState);

        Assert.True(away.NewState.IsCurrentlyAway);
        Assert.True(away.ShouldUpdateUi);
        Assert.False(unchanged.ShouldUpdateUi);
    }

    [Fact]
    public void ResetReturnsAnEmptyState()
    {
        var state = new PostureMonitoringState
        {
            ConsecutiveBadFrames = 10,
            ConsecutiveGoodFrames = 5,
            IsCurrentlySlouching = true,
            IsCurrentlyAway = true,
            BadPostureStartTime = Now,
            PostureWarningIntensity = 0.9,
        };

        Assert.Equal(PostureMonitoringState.Empty, PostureEngine.Reset(state));
    }

    private static PostureReadingResult Process(
        PostureReading reading,
        PostureMonitoringState state,
        PostureConfig? config = null,
        DateTimeOffset? currentTime = null) =>
        PostureEngine.ProcessReading(
            reading,
            state,
            config ?? new PostureConfig(),
            currentTime ?? Now,
            PostureEngine.BaseFrameInterval);
}
