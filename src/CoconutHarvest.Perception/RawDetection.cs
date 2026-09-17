using CoconutHarvest.Contracts;

namespace CoconutHarvest.Perception;

/// <summary>A detection in source-image pixel coordinates, before tracking.</summary>
public readonly record struct RawDetection(DetectionClass Class, float Confidence, BoundingBox BboxPx);
