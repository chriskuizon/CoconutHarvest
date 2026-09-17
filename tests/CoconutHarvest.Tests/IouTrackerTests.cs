using CoconutHarvest.Contracts;
using CoconutHarvest.Perception;
using Xunit;

namespace CoconutHarvest.Tests;

public class IouTrackerTests
{
    [Fact]
    public void Same_bunch_keeps_its_track_id_across_frames()
    {
        var tracker = new IouTracker();
        var f1 = tracker.Update([new RawDetection(DetectionClass.BunchMature, 0.9f, new BoundingBox(100, 100, 50, 60))]);
        var f2 = tracker.Update([new RawDetection(DetectionClass.BunchMature, 0.88f, new BoundingBox(104, 102, 50, 60))]);

        Assert.Single(f1);
        Assert.Single(f2);
        Assert.Equal(f1[0].Id, f2[0].Id);
        Assert.Equal(2, f2[0].Age);
    }

    [Fact]
    public void Different_class_at_same_location_gets_new_id()
    {
        var tracker = new IouTracker();
        var f1 = tracker.Update([new RawDetection(DetectionClass.BunchMature, 0.9f, new BoundingBox(0, 0, 50, 50))]);
        var f2 = tracker.Update([new RawDetection(DetectionClass.Trunk, 0.9f, new BoundingBox(0, 0, 50, 50))]);

        Assert.NotEqual(f1[0].Id, f2[0].Id);
    }

    [Fact]
    public void Track_survives_brief_occlusion()
    {
        var tracker = new IouTracker(maxMissedFrames: 3);
        var box = new RawDetection(DetectionClass.BunchMature, 0.9f, new BoundingBox(0, 0, 50, 50));
        var id = tracker.Update([box])[0].Id;
        tracker.Update([]);
        tracker.Update([]);
        var back = tracker.Update([box]);

        Assert.Equal(id, back[0].Id);
    }
}
