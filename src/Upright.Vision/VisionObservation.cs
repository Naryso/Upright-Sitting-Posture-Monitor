namespace Upright.Vision;

public sealed record PoseObservation(
    bool HasBodyObservation,
    double NoseX,
    double NoseYFromBottom,
    double NoseConfidence,
    double BodyConfidence,
    TimeSpan InferenceDuration,
    string ExecutionProvider)
{
    public bool HasUsableNose => HasBodyObservation && NoseConfidence > 0.3;
}

public sealed record FaceObservation(
    double MidYFromBottom,
    double NormalizedWidth,
    double Confidence,
    TimeSpan InferenceDuration);

public sealed record VisionFrame(
    byte[] BgraPixels,
    int Width,
    int Height)
{
    public int RequiredByteCount => checked(Width * Height * 4);

    public void Validate()
    {
        ArgumentNullException.ThrowIfNull(BgraPixels);
        if (Width <= 0 || Height <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(Width),
                "Frame dimensions must be positive.");
        }

        if (BgraPixels.Length < RequiredByteCount)
        {
            throw new ArgumentException(
                "The BGRA buffer is smaller than the declared frame.",
                nameof(BgraPixels));
        }
    }
}
