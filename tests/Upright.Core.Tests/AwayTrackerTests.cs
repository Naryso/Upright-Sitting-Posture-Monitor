namespace Upright.Core.Tests;

public sealed class AwayTrackerTests
{
    [Fact]
    public void FifteenthMissingFrameEntersAwayState()
    {
        var tracker = new AwayTracker(enabled: true);

        for (int index = 0; index < 14; index++)
        {
            AwayTransition transition = tracker.HandleNoDetection();
            Assert.False(transition.IsAway);
            Assert.False(transition.StateChanged);
        }

        AwayTransition fifteenth = tracker.HandleNoDetection();
        Assert.True(fifteenth.IsAway);
        Assert.True(fifteenth.StateChanged);
    }

    [Fact]
    public void DetectionResetsCounterAndLeavesAwayState()
    {
        var tracker = new AwayTracker(enabled: true, frameThreshold: 2);
        tracker.HandleNoDetection();
        tracker.HandleNoDetection();

        AwayTransition detected = tracker.HandleDetection();

        Assert.False(detected.IsAway);
        Assert.True(detected.StateChanged);
        Assert.False(tracker.HandleNoDetection().IsAway);
    }

    [Fact]
    public void DisabledTrackerIgnoresMissingFrames()
    {
        var tracker = new AwayTracker(enabled: false, frameThreshold: 1);

        AwayTransition transition = tracker.HandleNoDetection();

        Assert.False(transition.IsAway);
        Assert.False(transition.StateChanged);
    }
}
