using System.IO;
using System.Text.Json;
using Upright.Vision;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace Upright.App;

public static class ImageDiagnosticRunner
{
    public static async Task<int> RunAsync(string inputPath, string outputPath)
    {
        VisionFrame frame = await LoadFrameAsync(Path.GetFullPath(inputPath));
        (string detectorPath, string posePath) = ModelAssets.Resolve();
        using TwoStagePoseDetector detector = TwoStagePoseDetector.Create(
            detectorPath,
            posePath,
            preferDirectMl: true);

        PoseObservation pose = await Task.Run(() => detector.Detect(frame));
        var report = new
        {
            Status = pose.HasUsableNose ? "passed" : "no_usable_nose",
            frame.Width,
            frame.Height,
            detector.ExecutionProvider,
            detector.InitializationWarning,
            pose.HasBodyObservation,
            pose.HasUsableNose,
            pose.NoseX,
            pose.NoseYFromBottom,
            pose.NoseConfidence,
            pose.BodyConfidence,
            PipelineMilliseconds = pose.InferenceDuration.TotalMilliseconds,
        };

        string fullOutputPath = Path.GetFullPath(outputPath);
        string? outputDirectory = Path.GetDirectoryName(fullOutputPath);
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        await File.WriteAllTextAsync(
            fullOutputPath,
            JsonSerializer.Serialize(
                report,
                new JsonSerializerOptions { WriteIndented = true }));
        return pose.HasUsableNose ? 0 : 4;
    }

    private static async Task<VisionFrame> LoadFrameAsync(string path)
    {
        StorageFile file = await StorageFile.GetFileFromPathAsync(path);
        using IRandomAccessStream stream = await file.OpenAsync(FileAccessMode.Read);
        BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
        using SoftwareBitmap bitmap = await decoder.GetSoftwareBitmapAsync(
            BitmapPixelFormat.Bgra8,
            BitmapAlphaMode.Ignore);

        int byteCount = checked(bitmap.PixelWidth * bitmap.PixelHeight * 4);
        var buffer = new Windows.Storage.Streams.Buffer((uint)byteCount)
        {
            Length = (uint)byteCount,
        };
        bitmap.CopyToBuffer(buffer);
        byte[] pixels = new byte[byteCount];
        using DataReader reader = DataReader.FromBuffer(buffer);
        reader.ReadBytes(pixels);
        return new VisionFrame(pixels, bitmap.PixelWidth, bitmap.PixelHeight);
    }
}
