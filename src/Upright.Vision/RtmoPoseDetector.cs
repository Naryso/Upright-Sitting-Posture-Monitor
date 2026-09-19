using System.Diagnostics;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace Upright.Vision;

public sealed class RtmoPoseDetector : IDisposable
{
    public const double BodyScoreThreshold = 0.7;
    public const int NoseKeypointIndex = 0;

    private readonly InferenceSession _session;

    private RtmoPoseDetector(
        InferenceSession session,
        string executionProvider,
        string? initializationWarning = null)
    {
        _session = session;
        ExecutionProvider = executionProvider;
        InitializationWarning = initializationWarning;
    }

    public string ExecutionProvider { get; }
    public string? InitializationWarning { get; }

    public static RtmoPoseDetector Create(string modelPath, bool preferDirectMl = true)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(modelPath);

        if (preferDirectMl)
        {
            try
            {
                var directMlOptions = new SessionOptions
                {
                    GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
                    ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
                    IntraOpNumThreads = 2,
                    InterOpNumThreads = 1,
                };
                directMlOptions.AppendExecutionProvider_DML(0);
                return new RtmoPoseDetector(
                    new InferenceSession(modelPath, directMlOptions),
                    "DirectML");
            }
            catch (OnnxRuntimeException exception)
            {
                // The CPU provider is the required compatibility fallback.
                return CreateCpu(modelPath, exception.Message);
            }
        }

        return CreateCpu(modelPath, initializationWarning: null);
    }

    private static RtmoPoseDetector CreateCpu(
        string modelPath,
        string? initializationWarning)
    {
        var cpuOptions = new SessionOptions
        {
            GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL,
            ExecutionMode = ExecutionMode.ORT_SEQUENTIAL,
            IntraOpNumThreads = 2,
            InterOpNumThreads = 1,
        };
        return new RtmoPoseDetector(
            new InferenceSession(modelPath, cpuOptions),
            "CPU",
            initializationWarning);
    }

    public PoseObservation Detect(VisionFrame frame)
    {
        RtmoInput input = RtmoPreprocessor.CreateInput(frame);
        NamedOnnxValue inputValue = NamedOnnxValue.CreateFromTensor("input", input.Tensor);
        var inputs = new[] { inputValue };

        var stopwatch = Stopwatch.StartNew();
        using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> outputs =
            _session.Run(inputs);
        stopwatch.Stop();

        Tensor<float> detections = outputs
            .First(output => string.Equals(output.Name, "dets", StringComparison.Ordinal))
            .AsTensor<float>();
        Tensor<float> keypoints = outputs
            .First(output => string.Equals(output.Name, "keypoints", StringComparison.Ordinal))
            .AsTensor<float>();

        int selected = FindHighestScoringBody(detections);
        if (selected < 0)
        {
            return new PoseObservation(
                HasBodyObservation: false,
                NoseX: 0,
                NoseYFromBottom: 0,
                NoseConfidence: 0,
                BodyConfidence: 0,
                InferenceDuration: stopwatch.Elapsed,
                ExecutionProvider);
        }

        double bodyConfidence = detections[0, selected, 4];
        double noseXInSource = keypoints[0, selected, NoseKeypointIndex, 0] /
            input.ResizeRatio;
        double noseYInSource = keypoints[0, selected, NoseKeypointIndex, 1] /
            input.ResizeRatio;
        double noseConfidence = keypoints[0, selected, NoseKeypointIndex, 2];

        double normalizedX = Math.Clamp(noseXInSource / frame.Width, 0, 1);
        double normalizedYFromTop = Math.Clamp(noseYInSource / frame.Height, 0, 1);

        return new PoseObservation(
            HasBodyObservation: true,
            NoseX: normalizedX,
            NoseYFromBottom: 1 - normalizedYFromTop,
            NoseConfidence: noseConfidence,
            BodyConfidence: bodyConfidence,
            InferenceDuration: stopwatch.Elapsed,
            ExecutionProvider);
    }

    public void Dispose()
    {
        _session.Dispose();
        GC.SuppressFinalize(this);
    }

    private static int FindHighestScoringBody(Tensor<float> detections)
    {
        int count = detections.Dimensions[1];
        int selected = -1;
        float highestScore = (float)BodyScoreThreshold;

        for (int index = 0; index < count; index++)
        {
            float score = detections[0, index, 4];
            if (score > highestScore)
            {
                highestScore = score;
                selected = index;
            }
        }

        return selected;
    }
}
