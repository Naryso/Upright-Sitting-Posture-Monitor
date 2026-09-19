using System.Diagnostics;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Upright.Vision;

public sealed class TwoStagePoseDetector : IDisposable
{
    public const double BodyScoreThreshold = 0.7;

    private const int DetectorSize = 416;
    private const int PoseWidth = 192;
    private const int PoseHeight = 256;
    private const double PosePadding = 1.25;
    private const double SimccSplitRatio = 2;

    private static readonly float[] PoseMean = [123.675f, 116.28f, 103.53f];
    private static readonly float[] PoseStd = [58.395f, 57.12f, 57.375f];

    private readonly InferenceSession _detectorSession;
    private readonly InferenceSession _poseSession;

    private TwoStagePoseDetector(
        InferenceSession detectorSession,
        InferenceSession poseSession,
        string executionProvider,
        string? initializationWarning)
    {
        _detectorSession = detectorSession;
        _poseSession = poseSession;
        ExecutionProvider = executionProvider;
        InitializationWarning = initializationWarning;
    }

    public string ExecutionProvider { get; }
    public string? InitializationWarning { get; }

    public static TwoStagePoseDetector Create(
        string detectorModelPath,
        string poseModelPath,
        bool preferDirectMl = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(detectorModelPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(poseModelPath);

        if (preferDirectMl)
        {
            InferenceSession? detector = null;
            try
            {
                detector = CreateSession(detectorModelPath, directMl: true);
                InferenceSession pose = CreateSession(poseModelPath, directMl: true);
                return new TwoStagePoseDetector(detector, pose, "DirectML", null);
            }
            catch (OnnxRuntimeException exception)
            {
                detector?.Dispose();
                return CreateCpu(detectorModelPath, poseModelPath, exception.Message);
            }
        }

        return CreateCpu(detectorModelPath, poseModelPath, null);
    }

    public PoseObservation Detect(VisionFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        frame.Validate();
        var stopwatch = Stopwatch.StartNew();

        LetterboxInput detectorInput = CreateDetectorInput(frame);
        BoundingBox? body = DetectFirstBody(detectorInput, frame);
        if (body is null)
        {
            stopwatch.Stop();
            return Empty(stopwatch.Elapsed);
        }

        PoseInput poseInput = CreatePoseInput(frame, body);
        NamedOnnxValue poseValue =
            NamedOnnxValue.CreateFromTensor("input", poseInput.Tensor);
        using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> poseOutputs =
            _poseSession.Run([poseValue]);

        Tensor<float> simccX = poseOutputs
            .First(output => string.Equals(output.Name, "simcc_x", StringComparison.Ordinal))
            .AsTensor<float>();
        Tensor<float> simccY = poseOutputs
            .First(output => string.Equals(output.Name, "simcc_y", StringComparison.Ordinal))
            .AsTensor<float>();

        (int xIndex, float xScore) = FindMaximum(simccX, keypoint: 0);
        (int yIndex, float yScore) = FindMaximum(simccY, keypoint: 0);
        double noseConfidence = (xScore + yScore) * 0.5;

        double modelX = xIndex / SimccSplitRatio;
        double modelY = yIndex / SimccSplitRatio;
        double sourceX = (modelX / PoseWidth * poseInput.ScaleWidth) +
            poseInput.CenterX - (poseInput.ScaleWidth / 2);
        double sourceY = (modelY / PoseHeight * poseInput.ScaleHeight) +
            poseInput.CenterY - (poseInput.ScaleHeight / 2);

        stopwatch.Stop();
        double normalizedX = Math.Clamp(sourceX / frame.Width, 0, 1);
        double normalizedYFromTop = Math.Clamp(sourceY / frame.Height, 0, 1);

        return new PoseObservation(
            HasBodyObservation: true,
            NoseX: normalizedX,
            NoseYFromBottom: 1 - normalizedYFromTop,
            NoseConfidence: noseConfidence,
            BodyConfidence: body.Score,
            InferenceDuration: stopwatch.Elapsed,
            ExecutionProvider);
    }

    public void Dispose()
    {
        _detectorSession.Dispose();
        _poseSession.Dispose();
        GC.SuppressFinalize(this);
    }

    private static TwoStagePoseDetector CreateCpu(
        string detectorModelPath,
        string poseModelPath,
        string? initializationWarning) =>
        new(
            CreateSession(detectorModelPath, directMl: false),
            CreateSession(poseModelPath, directMl: false),
            "CPU",
            initializationWarning);

    private static InferenceSession CreateSession(string path, bool directMl)
    {
        using var options = new SessionOptions
        {
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
            ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
            IntraOpNumThreads = 2,
            InterOpNumThreads = 1,
        };
        if (directMl)
        {
            options.AppendExecutionProvider_DML(0);
        }

        return new InferenceSession(path, options);
    }

    private BoundingBox? DetectFirstBody(
        LetterboxInput input,
        VisionFrame sourceFrame)
    {
        NamedOnnxValue inputValue =
            NamedOnnxValue.CreateFromTensor("input", input.Tensor);
        using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs =
            _detectorSession.Run([inputValue]);

        Tensor<float> detections = outputs
            .First(output => string.Equals(output.Name, "dets", StringComparison.Ordinal))
            .AsTensor<float>();

        int count = detections.Dimensions[1];
        int selected = -1;
        float highestScore = (float)BodyScoreThreshold;
        for (int index = 0; index < count; index++)
        {
            float score = detections[0, index, 4];
            if (score > highestScore)
            {
                selected = index;
                highestScore = score;
            }
        }

        if (selected < 0)
        {
            return null;
        }

        double left = Math.Clamp(
            detections[0, selected, 0] / input.ResizeRatio,
            0,
            sourceFrame.Width);
        double top = Math.Clamp(
            detections[0, selected, 1] / input.ResizeRatio,
            0,
            sourceFrame.Height);
        double right = Math.Clamp(
            detections[0, selected, 2] / input.ResizeRatio,
            0,
            sourceFrame.Width);
        double bottom = Math.Clamp(
            detections[0, selected, 3] / input.ResizeRatio,
            0,
            sourceFrame.Height);

        return new BoundingBox(left, top, right, bottom, highestScore);
    }

    private PoseObservation Empty(TimeSpan elapsed) =>
        new(
            HasBodyObservation: false,
            NoseX: 0,
            NoseYFromBottom: 0,
            NoseConfidence: 0,
            BodyConfidence: 0,
            InferenceDuration: elapsed,
            ExecutionProvider);

    private static LetterboxInput CreateDetectorInput(VisionFrame frame)
    {
        double ratio = Math.Min(
            (double)DetectorSize / frame.Width,
            (double)DetectorSize / frame.Height);
        int resizedWidth = Math.Max(1, (int)(frame.Width * ratio));
        int resizedHeight = Math.Max(1, (int)(frame.Height * ratio));
        var tensor = new DenseTensor<float>(
            new[] { 1, 3, DetectorSize, DetectorSize });
        tensor.Buffer.Span.Fill(114);

        for (int y = 0; y < resizedHeight; y++)
        {
            double sourceY = ((y + 0.5) / ratio) - 0.5;
            for (int x = 0; x < resizedWidth; x++)
            {
                double sourceX = ((x + 0.5) / ratio) - 0.5;
                BgrPixel pixel = SampleBgr(frame, sourceX, sourceY);
                tensor[0, 0, y, x] = pixel.Blue;
                tensor[0, 1, y, x] = pixel.Green;
                tensor[0, 2, y, x] = pixel.Red;
            }
        }

        return new LetterboxInput(tensor, ratio);
    }

    private static PoseInput CreatePoseInput(VisionFrame frame, BoundingBox body)
    {
        double centerX = (body.Left + body.Right) * 0.5;
        double centerY = (body.Top + body.Bottom) * 0.5;
        double scaleWidth = (body.Right - body.Left) * PosePadding;
        double scaleHeight = (body.Bottom - body.Top) * PosePadding;
        double aspectRatio = (double)PoseWidth / PoseHeight;

        if (scaleWidth > scaleHeight * aspectRatio)
        {
            scaleHeight = scaleWidth / aspectRatio;
        }
        else
        {
            scaleWidth = scaleHeight * aspectRatio;
        }

        var tensor = new DenseTensor<float>(
            new[] { 1, 3, PoseHeight, PoseWidth });
        for (int y = 0; y < PoseHeight; y++)
        {
            double sourceY = centerY - (scaleHeight / 2) +
                ((y + 0.5) * scaleHeight / PoseHeight) - 0.5;
            for (int x = 0; x < PoseWidth; x++)
            {
                double sourceX = centerX - (scaleWidth / 2) +
                    ((x + 0.5) * scaleWidth / PoseWidth) - 0.5;

                BgrPixel pixel = SampleBgr(frame, sourceX, sourceY);
                tensor[0, 0, y, x] = (pixel.Red - PoseMean[0]) / PoseStd[0];
                tensor[0, 1, y, x] = (pixel.Green - PoseMean[1]) / PoseStd[1];
                tensor[0, 2, y, x] = (pixel.Blue - PoseMean[2]) / PoseStd[2];
            }
        }

        return new PoseInput(tensor, centerX, centerY, scaleWidth, scaleHeight);
    }

    private static BgrPixel SampleBgr(
        VisionFrame frame,
        double sourceX,
        double sourceY)
    {
        if (sourceX < 0 || sourceY < 0 ||
            sourceX > frame.Width - 1 || sourceY > frame.Height - 1)
        {
            return default;
        }

        int x0 = Math.Clamp((int)Math.Floor(sourceX), 0, frame.Width - 1);
        int y0 = Math.Clamp((int)Math.Floor(sourceY), 0, frame.Height - 1);
        int x1 = Math.Min(x0 + 1, frame.Width - 1);
        int y1 = Math.Min(y0 + 1, frame.Height - 1);
        double xWeight = Math.Clamp(sourceX - x0, 0, 1);
        double yWeight = Math.Clamp(sourceY - y0, 0, 1);

        int topLeft = ((y0 * frame.Width) + x0) * 4;
        int topRight = ((y0 * frame.Width) + x1) * 4;
        int bottomLeft = ((y1 * frame.Width) + x0) * 4;
        int bottomRight = ((y1 * frame.Width) + x1) * 4;
        ReadOnlySpan<byte> pixels = frame.BgraPixels;

        return new BgrPixel(
            SampleChannel(
                pixels,
                topLeft,
                topRight,
                bottomLeft,
                bottomRight,
                channel: 0,
                xWeight,
                yWeight),
            SampleChannel(
                pixels,
                topLeft,
                topRight,
                bottomLeft,
                bottomRight,
                channel: 1,
                xWeight,
                yWeight),
            SampleChannel(
                pixels,
                topLeft,
                topRight,
                bottomLeft,
                bottomRight,
                channel: 2,
                xWeight,
                yWeight));
    }

    private static float SampleChannel(
        ReadOnlySpan<byte> pixels,
        int topLeft,
        int topRight,
        int bottomLeft,
        int bottomRight,
        int channel,
        double xWeight,
        double yWeight)
    {
        float top = Lerp(
            pixels[topLeft + channel],
            pixels[topRight + channel],
            xWeight);
        float bottom = Lerp(
            pixels[bottomLeft + channel],
            pixels[bottomRight + channel],
            xWeight);
        return Lerp(top, bottom, yWeight);
    }

    private static float Lerp(float start, float end, double weight) =>
        (float)(start + ((end - start) * weight));

    private static (int Index, float Score) FindMaximum(
        Tensor<float> values,
        int keypoint)
    {
        int length = values.Dimensions[2];
        int selected = 0;
        float maximum = values[0, keypoint, 0];
        for (int index = 1; index < length; index++)
        {
            float value = values[0, keypoint, index];
            if (value > maximum)
            {
                maximum = value;
                selected = index;
            }
        }

        return (selected, maximum);
    }

    private sealed record LetterboxInput(
        DenseTensor<float> Tensor,
        double ResizeRatio);

    private sealed record PoseInput(
        DenseTensor<float> Tensor,
        double CenterX,
        double CenterY,
        double ScaleWidth,
        double ScaleHeight);

    private sealed record BoundingBox(
        double Left,
        double Top,
        double Right,
        double Bottom,
        double Score);

    private readonly record struct BgrPixel(
        float Blue,
        float Green,
        float Red);
}
