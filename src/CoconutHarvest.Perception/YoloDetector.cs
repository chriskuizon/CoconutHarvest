using CoconutHarvest.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;

namespace CoconutHarvest.Perception;

/// <summary>
/// Runs a YOLOv8-style ONNX model (output [1, 4+numClasses, N]) and returns NMS-filtered detections.
/// Ported from the webcam YoloDetector; the only coconut-specific parts are the class enum and thresholds.
/// </summary>
public sealed class YoloDetector : IDisposable
{
    private readonly InferenceSession _session;
    private readonly string _inputName;
    private readonly int _size;
    private readonly float _nmsIou;
    private readonly float[] _thresholds;
    private readonly Letterbox _letterbox;
    private readonly DenseTensor<float> _input;
    private readonly ILogger<YoloDetector> _log;

    private static readonly int ClassCount = Enum.GetValues<DetectionClass>().Length;

    public YoloDetector(IOptions<PerceptionOptions> options, ILogger<YoloDetector> log)
    {
        var o = options.Value;
        _log = log;
        _size = o.InputSize;
        _nmsIou = o.NmsIouThreshold;
        _letterbox = new Letterbox(_size);
        _input = new DenseTensor<float>([1, 3, _size, _size]);

        _thresholds = new float[ClassCount];
        foreach (var cls in Enum.GetValues<DetectionClass>())
        {
            var key = ContractsJsonNames.Of(cls);
            _thresholds[(int)cls] = o.ConfidenceThresholds.TryGetValue(key, out var t) ? t : 0.5f;
        }

        var so = new SessionOptions { GraphOptimizationLevel = GraphOptimizationLevel.ORT_ENABLE_ALL };
        // On the Jetson: so.AppendExecutionProvider_Tensorrt(); so.AppendExecutionProvider_CUDA();
        _session = new InferenceSession(o.ModelPath, so);
        _inputName = _session.InputMetadata.Keys.First();

        var outDims = _session.OutputMetadata.Values.First().Dimensions;
        try
        {
            ValidateOutputShape(outDims, ClassCount, o.AllowClassCountMismatch, _log);
        }
        catch
        {
            _session.Dispose();
            throw;
        }

        _log.LogInformation("Loaded {Model} ({Size}px)", o.ModelPath, _size);
    }

    /// <summary>
    /// Rejects a model whose output is not [1, 4+classes, N] with classes == <paramref name="classCount"/>.
    /// A mismatched model would otherwise run and silently reinterpret its class indices as
    /// DetectionClass, so a person could be published as a harvest target.
    /// </summary>
    public static void ValidateOutputShape(IReadOnlyList<int> outDims, int classCount, bool allowMismatch, ILogger log)
    {
        if (outDims.Count != 3)
        {
            throw new InvalidOperationException(
                $"Model output has rank {outDims.Count}; expected YOLOv8 layout [1, 4+classes, N].");
        }

        var modelClasses = outDims[1] - 4;
        if (modelClasses == classCount) return;

        if (!allowMismatch)
        {
            throw new InvalidOperationException(
                $"Model reports {modelClasses} classes but DetectionClass has {classCount}. " +
                "Class indices would be misinterpreted (check data.yaml vs enum order). " +
                "Set Perception:AllowClassCountMismatch=true only for transport smoke tests.");
        }

        log.LogCritical(
            "Model reports {N} classes but DetectionClass has {M}. AllowClassCountMismatch is set: " +
            "published classifications are INVALID, including the Human/Wire abort veto.",
            modelClasses, classCount);
    }

    public List<RawDetection> Detect(Mat frameBgr)
    {
        using var boxed = _letterbox.Apply(frameBgr);
        Preprocess(boxed);

        using var results = _session.Run([NamedOnnxValue.CreateFromTensor(_inputName, _input)]);
        var output = results.First().AsTensor<float>();
        return PostProcess(output, frameBgr.Width, frameBgr.Height);
    }

    private void Preprocess(Mat boxed) => FillInput(boxed, _input.Buffer.Span, _size);

    /// <summary>
    /// Letterboxed BGR uint8 image -> RGB float [0,1] in CHW order, written straight into the tensor's
    /// flat buffer. One pass over the pixel bytes: no per-frame pixel array and no 4-D indexer, which made
    /// the previous version ~55 ms of a ~99 ms frame at 640 px.
    /// </summary>
    public static void FillInput(Mat boxed, Span<float> chw, int size)
    {
        if (boxed.Type() != MatType.CV_8UC3 || boxed.Width != size || boxed.Height != size || !boxed.IsContinuous())
        {
            throw new ArgumentException($"Expected a continuous {size}x{size} CV_8UC3 image.", nameof(boxed));
        }
        var plane = size * size;
        if (chw.Length < 3 * plane) throw new ArgumentException("Tensor buffer too small.", nameof(chw));

        var bgr = boxed.AsSpan<byte>();
        var r = chw[..plane];
        var g = chw.Slice(plane, plane);
        var b = chw.Slice(2 * plane, plane);
        for (int i = 0, p = 0; i < plane; i++, p += 3)
        {
            b[i] = bgr[p] / 255f;
            g[i] = bgr[p + 1] / 255f;
            r[i] = bgr[p + 2] / 255f;
        }
    }


    private List<RawDetection> PostProcess(Tensor<float> output, int imgW, int imgH)
    {
        var rows = output.Dimensions[1]; // 4 + classes
        var anchors = output.Dimensions[2];
        var numClasses = rows - 4;
        var candidates = new List<RawDetection>(64);

        for (var a = 0; a < anchors; a++)
        {
            var bestCls = -1;
            var bestConf = 0f;
            for (var c = 0; c < numClasses && c < ClassCount; c++)
            {
                var conf = output[0, 4 + c, a];
                if (conf > bestConf) { bestConf = conf; bestCls = c; }
            }
            if (bestCls < 0 || bestConf < _thresholds[bestCls]) continue;

            var (x, y, w, h) = _letterbox.Unmap(output[0, 0, a], output[0, 1, a], output[0, 2, a], output[0, 3, a]);
            x = Math.Clamp(x, 0, imgW); y = Math.Clamp(y, 0, imgH);
            w = Math.Clamp(w, 0, imgW - x); h = Math.Clamp(h, 0, imgH - y);
            candidates.Add(new RawDetection((DetectionClass)bestCls, bestConf, new BoundingBox(x, y, w, h)));
        }

        return Nms(candidates);
    }

    /// <summary>Per-class greedy NMS.</summary>
    private List<RawDetection> Nms(List<RawDetection> dets)
    {
        dets.Sort((a, b) => b.Confidence.CompareTo(a.Confidence));
        var kept = new List<RawDetection>(dets.Count);
        var suppressed = new bool[dets.Count];
        for (var i = 0; i < dets.Count; i++)
        {
            if (suppressed[i]) continue;
            kept.Add(dets[i]);
            for (var j = i + 1; j < dets.Count; j++)
            {
                if (!suppressed[j] && dets[i].Class == dets[j].Class &&
                    dets[i].BboxPx.IoU(dets[j].BboxPx) > _nmsIou)
                {
                    suppressed[j] = true;
                }
            }
        }
        return kept;
    }

    public void Dispose() => _session.Dispose();
}

internal static class ContractsJsonNames
{
    public static string Of(DetectionClass cls) => cls switch
    {
        DetectionClass.BunchMature => "bunch_mature",
        DetectionClass.BunchTender => "bunch_tender",
        DetectionClass.BunchDry => "bunch_dry",
        DetectionClass.Trunk => "trunk",
        DetectionClass.Frond => "frond",
        DetectionClass.Human => "human",
        DetectionClass.Wire => "wire",
        _ => cls.ToString().ToLowerInvariant(),
    };
}
