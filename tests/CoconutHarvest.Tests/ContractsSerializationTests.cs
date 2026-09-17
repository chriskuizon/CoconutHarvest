using System.Text.Json;
using CoconutHarvest.Contracts;
using Xunit;

namespace CoconutHarvest.Tests;

public class ContractsSerializationTests
{
    [Fact]
    public void Round_trips_with_snake_case_and_enum_names()
    {
        var frame = new DetectionFrame(1, 42, 123456, "front", "m1", 18.4f,
            [new Detection(7, DetectionClass.BunchMature, 0.91f, new BoundingBox(0.42f, 0.31f, 0.11f, 0.14f), -3.2f, 12.8f, 3.6f, "stereo", 22)]);

        var json = JsonSerializer.Serialize(frame, ContractsJsonContext.Default.DetectionFrame);
        Assert.Contains("\"bunch_mature\"", json);
        Assert.Contains("\"track_id\":7", json);
        Assert.Contains("\"bbox_norm\"", json);

        var back = JsonSerializer.Deserialize(json, ContractsJsonContext.Default.DetectionFrame);
        Assert.NotNull(back);
        Assert.Equal(frame.Detections[0], back!.Detections[0]);
    }
}
