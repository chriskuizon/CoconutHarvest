namespace CoconutHarvest.Perception;

public sealed class PerceptionOptions
{
    public const string Section = "Perception";

    public string ModelPath { get; set; } = "coconut-yolov8s.onnx";
    public string ModelVersion { get; set; } = "unknown";
    public int CameraIndex { get; set; }
    public string CameraId { get; set; } = "front";
    public int InputSize { get; set; } = 640;
    public float HorizontalFovDeg { get; set; } = 90f;
    public float VerticalFovDeg { get; set; } = 60f;
    public float NmsIouThreshold { get; set; } = 0.45f;
    public string PublishEndpoint { get; set; } = "tcp://127.0.0.1:5556";
    public bool ShowPreview { get; set; } = true;

    /// <summary>
    /// Smoke-test escape hatch. When false (default), a model whose class count differs from
    /// <c>DetectionClass</c> fails startup. When true it loads anyway and its class indices are
    /// reinterpreted as <c>DetectionClass</c> — e.g. COCO "person" publishes as bunch_mature — so
    /// the output classifications, including the Human/Wire abort veto, are meaningless.
    /// </summary>
    public bool AllowClassCountMismatch { get; set; }

    /// <summary>Per-class thresholds keyed by JSON enum name (e.g. "human"). Abort classes run low.</summary>
    public Dictionary<string, float> ConfidenceThresholds { get; set; } = new();
}
