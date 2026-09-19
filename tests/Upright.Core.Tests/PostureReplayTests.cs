using System.Text.Json;

namespace Upright.Core.Tests;

public sealed class PostureReplayTests
{
    private static readonly DateTimeOffset ReplayStart =
        new(2026, 7, 25, 0, 0, 0, TimeSpan.Zero);

    public static TheoryData<string> ReplayFiles
    {
        get
        {
            string directory = Path.Combine(
                AppContext.BaseDirectory,
                "testdata",
                "posture_sequences");

            var data = new TheoryData<string>();
            foreach (string path in Directory.GetFiles(directory, "*.json").Order())
            {
                data.Add(path);
            }

            return data;
        }
    }

    [Theory]
    [MemberData(nameof(ReplayFiles))]
    public void ReplayMatchesExpectedFinalState(string path)
    {
        ReplaySequence sequence = JsonSerializer.Deserialize<ReplaySequence>(
            File.ReadAllText(path),
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
            ?? throw new InvalidDataException($"Could not deserialize {path}.");

        ReplayResult first = Run(sequence);
        ReplayResult second = Run(sequence);

        Assert.Equal(first, second);
        Assert.Equal(sequence.Expected.IsSlouching, first.State.IsCurrentlySlouching);
        Assert.Equal(sequence.Expected.IsAway, first.State.IsCurrentlyAway);
        Assert.Equal(sequence.Expected.SlouchEventCount, first.SlouchEventCount);
    }

    private static ReplayResult Run(ReplaySequence sequence)
    {
        var evaluator = new PostureEvaluator(sequence.Calibration, sequence.DeadZone);
        var awayTracker = new AwayTracker(sequence.AwayEnabled);
        var config = new PostureConfig(
            WarningOnsetDelay: TimeSpan.FromMilliseconds(sequence.WarningOnsetDelayMs));
        PostureMonitoringState state = PostureMonitoringState.Empty;
        int slouchEventCount = 0;

        foreach (ReplayEvent replayEvent in sequence.Events)
        {
            DateTimeOffset now = ReplayStart.AddMilliseconds(replayEvent.AtMs);

            if (string.Equals(replayEvent.Kind, "missing", StringComparison.Ordinal))
            {
                AwayTransition transition = awayTracker.HandleNoDetection();
                if (transition.StateChanged)
                {
                    state = PostureEngine.ProcessAwayChange(transition.IsAway, state).NewState;
                }

                continue;
            }

            Assert.Equal("observation", replayEvent.Kind);
            Assert.NotNull(replayEvent.NoseY);

            AwayTransition detected = awayTracker.HandleDetection();
            if (detected.StateChanged)
            {
                state = PostureEngine.ProcessAwayChange(detected.IsAway, state).NewState;
            }

            PostureReading reading = evaluator.Evaluate(
                replayEvent.NoseY.Value,
                replayEvent.FaceWidth,
                now);
            PostureReadingResult result = PostureEngine.ProcessReading(
                reading,
                state,
                config,
                now,
                evaluator.IsCurrentlySlouching
                    ? PostureEngine.SlouchingFrameInterval
                    : PostureEngine.BaseFrameInterval);

            state = result.NewState;
            slouchEventCount += result.Effects
                .Count(effect => effect is PostureEngineEffect.RecordSlouchEvent);
        }

        return new ReplayResult(state, slouchEventCount);
    }

    private sealed record ReplayResult(
        PostureMonitoringState State,
        int SlouchEventCount);

    private sealed record ReplaySequence
    {
        public required string Name { get; init; }
        public required CameraCalibrationData Calibration { get; init; }
        public double DeadZone { get; init; }
        public bool AwayEnabled { get; init; }
        public double WarningOnsetDelayMs { get; init; }
        public required IReadOnlyList<ReplayEvent> Events { get; init; }
        public required ReplayExpected Expected { get; init; }
    }

    private sealed record ReplayEvent
    {
        public double AtMs { get; init; }
        public required string Kind { get; init; }
        public double? NoseY { get; init; }
        public double? FaceWidth { get; init; }
    }

    private sealed record ReplayExpected
    {
        public bool IsSlouching { get; init; }
        public bool IsAway { get; init; }
        public int SlouchEventCount { get; init; }
    }
}
