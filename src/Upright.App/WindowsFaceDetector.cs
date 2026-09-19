using System.Diagnostics;
using Upright.Vision;
using Windows.Graphics.Imaging;
using Windows.Media.FaceAnalysis;
using Windows.Storage.Streams;

namespace Upright.App;

public sealed class WindowsFaceDetector
{
    private readonly FaceDetector _detector;

    private WindowsFaceDetector(FaceDetector detector)
    {
        _detector = detector;
    }

    public static async Task<WindowsFaceDetector> CreateAsync() =>
        new(await FaceDetector.CreateAsync());

    public async Task<FaceObservation?> DetectFirstAsync(VisionFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        frame.Validate();

        using var writer = new DataWriter();
        writer.WriteBytes(frame.BgraPixels.AsSpan(0, frame.RequiredByteCount).ToArray());
        IBuffer buffer = writer.DetachBuffer();

        using var bgra = new SoftwareBitmap(
            BitmapPixelFormat.Bgra8,
            frame.Width,
            frame.Height,
            BitmapAlphaMode.Ignore);
        bgra.CopyFromBuffer(buffer);

        BitmapPixelFormat detectorFormat = FaceDetector.IsBitmapPixelFormatSupported(
            BitmapPixelFormat.Gray8)
            ? BitmapPixelFormat.Gray8
            : FaceDetector.GetSupportedBitmapPixelFormats().First();
        using SoftwareBitmap detectorBitmap = SoftwareBitmap.Convert(
            bgra,
            detectorFormat);

        var stopwatch = Stopwatch.StartNew();
        IList<DetectedFace> faces =
            await _detector.DetectFacesAsync(detectorBitmap);
        stopwatch.Stop();

        DetectedFace? first = faces.FirstOrDefault();
        if (first is null)
        {
            return null;
        }

        BitmapBounds box = first.FaceBox;
        double midYFromTop = (box.Y + (box.Height / 2.0)) / frame.Height;
        return new FaceObservation(
            MidYFromBottom: 1 - Math.Clamp(midYFromTop, 0, 1),
            NormalizedWidth: Math.Clamp((double)box.Width / frame.Width, 0, 1),
            Confidence: 1,
            InferenceDuration: stopwatch.Elapsed);
    }
}
