using CoconutHarvest.Contracts;

namespace CoconutHarvest.Perception;

/// <summary>
/// Supplies range to a detection. The stub returns null; wire in a stereo depth map
/// (e.g. ZED / RealSense SDK) or a LiDAR projection and sample the bbox centre region.
/// </summary>
public interface IRangeSource
{
    (float? RangeM, string? Source) RangeFor(in BoundingBox bboxNorm);
}

public sealed class NullRangeSource : IRangeSource
{
    public (float? RangeM, string? Source) RangeFor(in BoundingBox bboxNorm) => (null, null);
}
