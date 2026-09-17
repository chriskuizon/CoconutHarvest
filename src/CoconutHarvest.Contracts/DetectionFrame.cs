using System.Text.Json.Serialization;

namespace CoconutHarvest.Contracts;

public sealed record Detection(
    int TrackId,
    DetectionClass Class,
    float Confidence,
    BoundingBox BboxNorm,
    float BearingDeg,
    float ElevationDeg,
    float? RangeM,
    string? RangeSource,
    int AgeFrames);

public sealed record DetectionFrame(
    int SchemaVersion,
    long FrameId,
    long TimestampUs,
    string CameraId,
    string ModelVersion,
    float InferenceMs,
    IReadOnlyList<Detection> Detections)
{
    public const int CurrentSchemaVersion = 1;
    public const string Topic = "det";
}

[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    UseStringEnumConverter = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull)]
[JsonSerializable(typeof(DetectionFrame))]
public partial class ContractsJsonContext : JsonSerializerContext;
