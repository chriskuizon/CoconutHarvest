using CoconutHarvest.Contracts;
using CoconutHarvest.Perception;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace CoconutHarvest.Tests;

/// <summary>A model with the wrong class count must not start, or COCO "person" publishes as bunch_mature.</summary>
public class YoloOutputShapeTests
{
    private static readonly int Classes = Enum.GetValues<DetectionClass>().Length;

    [Fact]
    public void Accepts_a_model_matching_DetectionClass()
    {
        YoloDetector.ValidateOutputShape([1, 4 + Classes, 8400], Classes, allowMismatch: false, NullLogger.Instance);
    }

    [Fact]
    public void Rejects_a_stock_COCO_model_by_default()
    {
        var ex = Assert.Throws<InvalidOperationException>(() =>
            YoloDetector.ValidateOutputShape([1, 84, 8400], Classes, allowMismatch: false, NullLogger.Instance));

        Assert.Contains("80 classes", ex.Message);
        Assert.Contains("AllowClassCountMismatch", ex.Message);
    }

    [Fact]
    public void Loads_a_mismatched_model_only_when_explicitly_allowed()
    {
        YoloDetector.ValidateOutputShape([1, 84, 8400], Classes, allowMismatch: true, NullLogger.Instance);
    }

    [Theory]
    [InlineData(new[] { 1, 84 })]
    [InlineData(new[] { 1, 8400, 84, 1 })]
    public void Rejects_an_unexpected_output_rank_even_when_mismatch_is_allowed(int[] dims)
    {
        Assert.Throws<InvalidOperationException>(() =>
            YoloDetector.ValidateOutputShape(dims, Classes, allowMismatch: true, NullLogger.Instance));
    }
}
