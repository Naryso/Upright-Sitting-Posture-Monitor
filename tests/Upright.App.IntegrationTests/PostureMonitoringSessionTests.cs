using Upright.Core;
using Upright.Vision;

namespace Upright.App.IntegrationTests;

public sealed class PostureMonitoringSessionTests
{
    private static readonly DateTimeOffset Start =
        new(2026, 7, 25, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void EightBadObservationsEnterAndFiveGoodObservationsRecover()
    {
        var session = CreateSession();

        MonitoringUpdate update = default!;
        for (int index = 0; index < 8; index++)
        {
            update = session.Process(Accepted(0.38), Start.AddMilliseconds(index * 100));
        }

        Assert.True(update.State.IsCurrentlySlouching);
        Assert.Equal(PostureEngine.SlouchingFrameInterval, update.NextProcessingInterval);

        for (int index = 0; index < 9; index++)
        {
            update = session.Process(
                Accepted(0.60),
                Start.AddMilliseconds(800 + (index * 100)));
        }

        Assert.False(update.State.IsCurrentlySlouching);
        Assert.Equal(PostureEngine.BaseFrameInterval, update.NextProcessingInterval);
    }

    [Fact]
    public void BodyWithoutNoseDoesNotAdvanceAwayOrPostureCounters()
    {
        var session = CreateSession();
        var ignored = new VisionDecision(
            VisionDecisionKind.BodyWithoutUsableNose,
            0,
            null);

        for (int index = 0; index < 20; index++)
        {
            session.Process(ignored, Start.AddMilliseconds(index));
        }

        Assert.Equal(PostureMonitoringState.Empty, session.State);
    }

    [Fact]
    public void FifteenTrueNoDetectionsEnterAwayState()
    {
        var session = CreateSession();
        var missing = new VisionDecision(VisionDecisionKind.NoDetection, 0, null);

        for (int index = 0; index < 15; index++)
        {
            session.Process(missing, Start.AddMilliseconds(index));
        }

        Assert.True(session.State.IsCurrentlyAway);
    }

    private static PostureMonitoringSession CreateSession() =>
        new(
            new CameraCalibrationData(
                GoodPostureY: 0.60,
                BadPostureY: 0.40,
                NeutralY: 0.50,
                PostureRange: 0.20,
                CameraId: "camera",
                NeutralFaceWidth: 0.20),
            deadZone: 0.03,
            new PostureConfig(),
            awayEnabled: true);

    private static VisionDecision Accepted(double noseY) =>
        new(VisionDecisionKind.AcceptedObservation, noseY, FaceWidth: null);
}
