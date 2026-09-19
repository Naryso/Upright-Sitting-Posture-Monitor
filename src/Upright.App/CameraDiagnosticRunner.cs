using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Upright.Vision;

namespace Upright.App;

public static class CameraDiagnosticRunner
{
    private const int TargetSamples = 5;

    public static async Task<int> RunAsync(string outputPath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        string? outputDirectory = Path.GetDirectoryName(Path.GetFullPath(outputPath));
        if (!string.IsNullOrEmpty(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        IReadOnlyList<CameraDevice> cameras =
            await CameraCaptureService.GetCamerasAsync();
        if (cameras.Count == 0)
        {
            await WriteAsync(
                outputPath,
                new CameraSmokeReport
                {
                    Status = "no_camera",
                    CameraCount = 0,
                });
            return 2;
        }

        (string detectorPath, string posePath) = ModelAssets.Resolve();
        using TwoStagePoseDetector poseDetector = TwoStagePoseDetector.Create(
            detectorPath,
            posePath,
            preferDirectMl: true);
        WindowsFaceDetector faceDetector = await WindowsFaceDetector.CreateAsync();
        await using var camera = new CameraCaptureService();
        var inferenceGate = new SemaphoreSlim(1, 1);
        var samples = new List<CameraSmokeSample>();
        var completion = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource? restartCompletion = null;
        Exception? captureException = null;
        int frameWidth = 0;
        int frameHeight = 0;

        camera.CaptureFailed += (_, exception) =>
        {
            captureException = exception;
            completion.TrySetResult();
        };
        camera.FrameArrived += async (_, frame) =>
        {
            if (!await inferenceGate.WaitAsync(0))
            {
                return;
            }

            try
            {
                frameWidth = frame.Width;
                frameHeight = frame.Height;
                var visionFrame = new VisionFrame(
                    frame.BgraPixels,
                    frame.Width,
                    frame.Height);
                PoseObservation pose =
                    await Task.Run(() => poseDetector.Detect(visionFrame));
                FaceObservation? face = null;
                if (!pose.HasBodyObservation)
                {
                    face = await faceDetector.DetectFirstAsync(visionFrame);
                }

                lock (samples)
                {
                    samples.Add(new CameraSmokeSample
                    {
                        HasBody = pose.HasBodyObservation,
                        HasUsableNose = pose.HasUsableNose,
                        NoseYFromBottom = pose.HasBodyObservation
                            ? pose.NoseYFromBottom
                            : null,
                        NoseConfidence = pose.HasBodyObservation
                            ? pose.NoseConfidence
                            : null,
                        FaceMidYFromBottom = face?.MidYFromBottom,
                        FaceWidth = face?.NormalizedWidth,
                        PipelineMilliseconds =
                            pose.InferenceDuration.TotalMilliseconds +
                            (face?.InferenceDuration.TotalMilliseconds ?? 0),
                    });
                    if (samples.Count >= TargetSamples)
                    {
                        completion.TrySetResult();
                    }

                    restartCompletion?.TrySetResult();
                }
            }
            catch (Exception exception)
            {
                captureException = exception;
                completion.TrySetResult();
            }
            finally
            {
                inferenceGate.Release();
            }
        };

        var initializationStopwatch = Stopwatch.StartNew();
        await camera.StartAsync(cameras[0].Id);
        initializationStopwatch.Stop();

        Task finished = await Task.WhenAny(
            completion.Task,
            Task.Delay(TimeSpan.FromSeconds(15)));
        await camera.StopAsync();

        int sampleCountBeforeRestart;
        lock (samples)
        {
            sampleCountBeforeRestart = samples.Count;
        }

        restartCompletion = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        var restartStopwatch = Stopwatch.StartNew();
        await camera.StartAsync(cameras[0].Id);
        restartStopwatch.Stop();
        Task restartFinished = await Task.WhenAny(
            restartCompletion.Task,
            Task.Delay(TimeSpan.FromSeconds(5)));
        await camera.StopAsync();
        await inferenceGate.WaitAsync();
        inferenceGate.Release();

        CameraSmokeSample[] finalSamples;
        lock (samples)
        {
            finalSamples = samples.ToArray();
        }

        bool restartSucceeded =
            restartFinished == restartCompletion.Task &&
            finalSamples.Length > sampleCountBeforeRestart;
        var report = new CameraSmokeReport
        {
            Status = captureException is not null
                ? "error"
                : finished == completion.Task &&
                    finalSamples.Length >= TargetSamples &&
                    restartSucceeded
                    ? "passed"
                    : "timeout",
            CameraCount = cameras.Count,
            CameraInitializationMilliseconds =
                initializationStopwatch.Elapsed.TotalMilliseconds,
            RestartInitializationMilliseconds =
                restartStopwatch.Elapsed.TotalMilliseconds,
            RestartSucceeded = restartSucceeded,
            FrameWidth = frameWidth,
            FrameHeight = frameHeight,
            ExecutionProvider = poseDetector.ExecutionProvider,
            ProviderFallbackReason = poseDetector.InitializationWarning,
            Samples = finalSamples,
            Error = captureException?.ToString(),
        };
        await WriteAsync(outputPath, report);
        inferenceGate.Dispose();
        return report.Status == "passed" ? 0 : 3;
    }

    private static Task WriteAsync(string outputPath, CameraSmokeReport report) =>
        File.WriteAllTextAsync(
            outputPath,
            JsonSerializer.Serialize(
                report,
                new JsonSerializerOptions { WriteIndented = true }));

    private sealed record CameraSmokeReport
    {
        public string Status { get; init; } = "not_started";
        public int CameraCount { get; init; }
        public double CameraInitializationMilliseconds { get; init; }
        public double RestartInitializationMilliseconds { get; init; }
        public bool RestartSucceeded { get; init; }
        public int FrameWidth { get; init; }
        public int FrameHeight { get; init; }
        public string? ExecutionProvider { get; init; }
        public string? ProviderFallbackReason { get; init; }
        public IReadOnlyList<CameraSmokeSample> Samples { get; init; } = [];
        public string? Error { get; init; }
    }

    private sealed record CameraSmokeSample
    {
        public bool HasBody { get; init; }
        public bool HasUsableNose { get; init; }
        public double? NoseYFromBottom { get; init; }
        public double? NoseConfidence { get; init; }
        public double? FaceMidYFromBottom { get; init; }
        public double? FaceWidth { get; init; }
        public double PipelineMilliseconds { get; init; }
    }
}
