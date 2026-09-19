namespace Upright.Vision.Tests;

public sealed class VisionDecisionAdapterTests
{
    [Fact]
    public void UsableBodyNoseWinsAndHasNoFaceWidth()
    {
        VisionDecision decision = VisionDecision.From(
            Pose(body: true, noseConfidence: 0.31),
            Face());

        Assert.Equal(VisionDecisionKind.AcceptedObservation, decision.Kind);
        Assert.Equal(0.7, decision.NoseYFromBottom);
        Assert.Null(decision.FaceWidth);
    }

    [Fact]
    public void BodyWithUnusableNoseDoesNotUseFaceFallback()
    {
        VisionDecision decision = VisionDecision.From(
            Pose(body: true, noseConfidence: 0.3),
            Face());

        Assert.Equal(VisionDecisionKind.BodyWithoutUsableNose, decision.Kind);
    }

    [Fact]
    public void NoBodyUsesFaceFallback()
    {
        VisionDecision decision = VisionDecision.From(
            Pose(body: false, noseConfidence: 0),
            Face());

        Assert.Equal(VisionDecisionKind.AcceptedObservation, decision.Kind);
        Assert.Equal(0.4, decision.NoseYFromBottom);
        Assert.Equal(0.2, decision.FaceWidth);
    }

    [Fact]
    public void NoBodyAndNoFaceIsNoDetection()
    {
        VisionDecision decision = VisionDecision.From(
            Pose(body: false, noseConfidence: 0),
            faceFallback: null);

        Assert.Equal(VisionDecisionKind.NoDetection, decision.Kind);
    }

    private static PoseObservation Pose(bool body, double noseConfidence) =>
        new(
            HasBodyObservation: body,
            NoseX: 0.5,
            NoseYFromBottom: 0.7,
            NoseConfidence: noseConfidence,
            BodyConfidence: body ? 0.9 : 0,
            InferenceDuration: TimeSpan.FromMilliseconds(10),
            ExecutionProvider: "test");

    private static FaceObservation Face() =>
        new(
            MidYFromBottom: 0.4,
            NormalizedWidth: 0.2,
            Confidence: 1,
            InferenceDuration: TimeSpan.FromMilliseconds(1));
}
