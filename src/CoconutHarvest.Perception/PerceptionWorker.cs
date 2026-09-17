using System.Diagnostics;
using CoconutHarvest.Contracts;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenCvSharp;

namespace CoconutHarvest.Perception;

public sealed class PerceptionWorker(
    IOptions<PerceptionOptions> options,
    YoloDetector detector,
    IRangeSource rangeSource,
    ILogger<PerceptionWorker> log) : BackgroundService
{
    private readonly PerceptionOptions _o = options.Value;

    protected override Task ExecuteAsync(CancellationToken ct) =>
        Task.Factory.StartNew(() => Loop(ct), ct, TaskCreationOptions.LongRunning, TaskScheduler.Default);

    private void Loop(CancellationToken ct)
    {
        using var capture = new VideoCapture(_o.CameraIndex);
        if (!capture.IsOpened())
        {
            log.LogCritical("Camera {Index} could not be opened", _o.CameraIndex);
            return;
        }

        using var publisher = new DetectionPublisher(_o.PublishEndpoint);
        var tracker = new IouTracker();
        var geometry = new CameraGeometry(_o.HorizontalFovDeg, _o.VerticalFovDeg);
        using var frame = new Mat();
        using var window = _o.ShowPreview ? new Window("Perception (ESC to quit)") : null;

        long frameId = 0;
        var sw = new Stopwatch();
        log.LogInformation("Publishing on {Endpoint}", _o.PublishEndpoint);

        while (!ct.IsCancellationRequested)
        {
            if (!capture.Read(frame) || frame.Empty()) continue;
            frameId++;

            sw.Restart();
            var raw = detector.Detect(frame);
            var tracks = tracker.Update(raw);
            var inferenceMs = (float)sw.Elapsed.TotalMilliseconds;

            var detections = new List<Detection>(tracks.Count);
            foreach (var t in tracks)
            {
                var norm = t.Bbox.Normalise(frame.Width, frame.Height);
                var (bearing, elevation) = geometry.Angles(norm);
                var (range, src) = rangeSource.RangeFor(norm);
                detections.Add(new Detection(t.Id, t.Class, t.Confidence, norm, bearing, elevation, range, src, t.Age));
            }

            publisher.Publish(new DetectionFrame(
                DetectionFrame.CurrentSchemaVersion,
                frameId,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000,
                _o.CameraId,
                _o.ModelVersion,
                inferenceMs,
                detections));

            if (window is not null)
            {
                Draw(frame, tracks, inferenceMs);
                window.ShowImage(frame);
                if (Cv2.WaitKey(1) == 27) break;
            }
        }
    }

    private static void Draw(Mat frame, IReadOnlyList<IouTracker.Track> tracks, float ms)
    {
        foreach (var t in tracks)
        {
            var color = t.Class.IsAbortClass ? Scalar.Red : t.Class.IsHarvestTarget ? Scalar.LimeGreen : Scalar.Yellow;
            var r = new Rect((int)t.Bbox.X, (int)t.Bbox.Y, (int)t.Bbox.W, (int)t.Bbox.H);
            Cv2.Rectangle(frame, r, color, 2);
            Cv2.PutText(frame, $"#{t.Id} {t.Class} {t.Confidence:P0}", new Point(r.X, Math.Max(r.Y - 6, 12)),
                HersheyFonts.HersheySimplex, 0.55, color, 2);
        }
        Cv2.PutText(frame, $"{ms:F1} ms", new Point(10, 24), HersheyFonts.HersheySimplex, 0.7, Scalar.White, 2);
    }
}
