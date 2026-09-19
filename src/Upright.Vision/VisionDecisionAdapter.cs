namespace Upright.Vision;

public enum VisionDecisionKind
{
    AcceptedObservation,
    NoDetection,
    BodyWithoutUsableNose,
}

public readonly record struct VisionDecision(
    VisionDecisionKind Kind,
    double NoseYFromBottom,
    double? FaceWidth)
{
    public static VisionDecision From(
        PoseObservation pose,
        FaceObservation? faceFallback)
    {
        ArgumentNullException.ThrowIfNull(pose);

        if (pose.HasBodyObservation)
        {
            return pose.HasUsableNose
                ? new VisionDecision(
                    VisionDecisionKind.AcceptedObservation,
                    pose.NoseYFromBottom,
                    FaceWidth: null)
                : new VisionDecision(
                    VisionDecisionKind.BodyWithoutUsableNose,
                    NoseYFromBottom: 0,
                    FaceWidth: null);
        }

        return faceFallback is not null
            ? new VisionDecision(
                VisionDecisionKind.AcceptedObservation,
                faceFallback.MidYFromBottom,
                faceFallback.NormalizedWidth)
            : new VisionDecision(
                VisionDecisionKind.NoDetection,
                NoseYFromBottom: 0,
                FaceWidth: null);
    }
}
