using System.Diagnostics;
using System.IO;
using System.Text.Json;
using Upright.Vision;

namespace Upright.App;

public static class SoakDiagnosticRunner
{
    public static async Task<int> RunAsync(
        TimeSpan duration,
        string outputPath)
    {
        if (duration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration));
        }

        IReadOnlyList<CameraDevice> cameras =
            await CameraCaptureService.GetCamerasAsync();
        if (cameras.Count == 0)
        {
            throw new InvalidOperationException("No camera was found.");
        }

        (string detectorPath, string posePath) = ModelAssets.Resolve();
        using TwoStagePoseDetector poseDetector = TwoStagePoseDetector.Create(
            detectorPath,
            posePath,
            preferDirectMl: true);
        WindowsFaceDetector faceDetector = await WindowsFaceDetector.CreateAsync();
        await using var camera = new CameraCaptureService();
        var inferenceGate = new SemaphoreSlim(1, 1);
        var clock = Stopwatch.StartNew();
        var synchronization = new object();
        TimeSpan? lastProcessed = null;
        Exception? failure = null;
        int processed = 0;
        int bodyCount = 0;
        int usableNoseCount = 0;
        int faceCount = 0;
        double totalPipelineMilliseconds = 0;
        double maximumPipelineMilliseconds = 0;
        long maximumWorkingSetBytes = Environment.WorkingSet;

        camera.CaptureFailed += (_, exception) => failure ??= exception;
        camera.FrameArrived += async (_, frame) =>
        {
            TimeSpan now = clock.Elapsed;
            lock (synchronization)
            {
                if (lastProcessed is TimeSpan last &&
                    now - last < TimeSpan.FromMilliseconds(250))
                {
                    return;
                }

                lastProcessed = now;
            }

            if (!await inferenceGate.WaitAsync(0))
            {
                return;
            }

            try
            {
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

                double pipelineMilliseconds =
                    pose.InferenceDuration.TotalMilliseconds +
                    (face?.InferenceDuration.TotalMilliseconds ?? 0);
                lock (synchronization)
                {
                    processed++;
                    bodyCount += pose.HasBodyObservation ? 1 : 0;
                    usableNoseCount += pose.HasUsableNose ? 1 : 0;
                    faceCount += face is null ? 0 : 1;
                    totalPipelineMilliseconds += pipelineMilliseconds;
                    maximumPipelineMilliseconds = Math.Max(
                        maximumPipelineMilliseconds,
                        pipelineMilliseconds);
                    maximumWorkingSetBytes = Math.Max(
                        maximumWorkingSetBytes,
                        Environment.WorkingSet);
                }
            }
            catch (Exception exception)
            {
                failure ??= exception;
            }
            finally
            {
                inferenceGate.Release();
            }
        };

        using Process currentProcess = Process.GetCurrentProcess();
        TimeSpan startingCpuTime = currentProcess.TotalProcessorTime;
        await camera.StartAsync(cameras[0].Id);
        await Task.Delay(duration);
        await camera.StopAsync();
        await inferenceGate.WaitAsync();
        inferenceGate.Release();
        clock.Stop();
        currentProcess.Refresh();

        double elapsedSeconds = clock.Elapsed.TotalSeconds;
        double cpuSeconds =
            (currentProcess.TotalProcessorTime - startingCpuTime).TotalSeconds;
        int machineLogicalProcessorCount = GetMachineLogicalProcessorCount();
        double averageCpuPercent = elapsedSeconds <= 0
            ? 0
            : cpuSeconds / elapsedSeconds / machineLogicalProcessorCount * 100;

        string status = failure is null && processed > 0
            ? "passed"
            : "failed";
        var report = new
        {
            Status = status,
            RequestedDurationSeconds = duration.TotalSeconds,
            ActualDurationSeconds = elapsedSeconds,
            ProcessedSamples = processed,
            BodySamples = bodyCount,
            UsableNoseSamples = usableNoseCount,
            FaceFallbackSamples = faceCount,
            AveragePipelineMilliseconds =
                processed == 0 ? 0 : totalPipelineMilliseconds / processed,
            MaximumPipelineMilliseconds = maximumPipelineMilliseconds,
            AverageCpuPercent = averageCpuPercent,
            DotNetAvailableProcessorCount = Environment.ProcessorCount,
            MachineLogicalProcessorCount = machineLogicalProcessorCount,
            MaximumWorkingSetMegabytes =
                maximumWorkingSetBytes / 1024.0 / 1024.0,
            ExecutionProvider = poseDetector.ExecutionProvider,
            ProviderFallbackReason = poseDetector.InitializationWarning,
            Error = failure?.ToString(),
        };

        string fullOutputPath = Path.GetFullPath(outputPath);
        string? directory = Path.GetDirectoryName(fullOutputPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(
            fullOutputPath,
            JsonSerializer.Serialize(
                report,
                new JsonSerializerOptions { WriteIndented = true }));
        inferenceGate.Dispose();
        return status == "passed" ? 0 : 6;
    }

    private static int GetMachineLogicalProcessorCount()
    {
        string? value = Environment.GetEnvironmentVariable("NUMBER_OF_PROCESSORS");
        return int.TryParse(
            value,
            System.Globalization.NumberStyles.Integer,
            System.Globalization.CultureInfo.InvariantCulture,
            out int count) &&
            count > 0
            ? count
            : Math.Max(1, Environment.ProcessorCount);
    }
}
