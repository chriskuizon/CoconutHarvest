using CoconutHarvest.Contracts;

namespace CoconutHarvest.Perception;

/// <summary>
/// Minimal IoU-association tracker (ByteTrack-lite). Assigns stable track IDs across frames
/// so mission logic can lock onto a bunch rather than a single detection.
/// </summary>
public sealed class IouTracker(float matchIou = 0.3f, int maxMissedFrames = 15)
{
    public sealed class Track
    {
        public required int Id { get; init; }
        public required DetectionClass Class { get; init; }
        public BoundingBox Bbox { get; set; }
        public float Confidence { get; set; }
        public int Age { get; set; }
        public int Missed { get; set; }
    }

    private readonly List<Track> _tracks = [];
    private int _nextId = 1;

    public IReadOnlyList<Track> Tracks => _tracks;

    public IReadOnlyList<Track> Update(IReadOnlyList<RawDetection> detections)
    {
        var matchedTracks = new HashSet<Track>();
        var matchedDets = new bool[detections.Count];

        // Greedy best-IoU matching, same class only
        var pairs = new List<(float iou, Track t, int d)>();
        foreach (var t in _tracks)
        {
            for (var d = 0; d < detections.Count; d++)
            {
                if (detections[d].Class != t.Class) continue;
                var iou = t.Bbox.IoU(detections[d].BboxPx);
                if (iou >= matchIou) pairs.Add((iou, t, d));
            }
        }
        pairs.Sort((a, b) => b.iou.CompareTo(a.iou));

        foreach (var (_, t, d) in pairs)
        {
            if (matchedTracks.Contains(t) || matchedDets[d]) continue;
            t.Bbox = detections[d].BboxPx;
            t.Confidence = detections[d].Confidence;
            t.Age++;
            t.Missed = 0;
            matchedTracks.Add(t);
            matchedDets[d] = true;
        }

        // Unmatched tracks: age out
        foreach (var t in _tracks)
        {
            if (!matchedTracks.Contains(t)) t.Missed++;
        }
        _tracks.RemoveAll(t => t.Missed > maxMissedFrames);

        // Unmatched detections: new tracks
        for (var d = 0; d < detections.Count; d++)
        {
            if (matchedDets[d]) continue;
            _tracks.Add(new Track
            {
                Id = _nextId++,
                Class = detections[d].Class,
                Bbox = detections[d].BboxPx,
                Confidence = detections[d].Confidence,
                Age = 1,
            });
        }

        // Only report tracks seen this frame
        return _tracks.Where(t => t.Missed == 0).ToList();
    }
}
