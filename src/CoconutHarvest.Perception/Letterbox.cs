using OpenCvSharp;

namespace CoconutHarvest.Perception;

/// <summary>Resize with aspect ratio preserved and grey padding, as Ultralytics does at train time.</summary>
public sealed class Letterbox(int size)
{
    public float Scale { get; private set; } = 1f;
    public int PadX { get; private set; }
    public int PadY { get; private set; }

    public Mat Apply(Mat src)
    {
        Scale = MathF.Min((float)size / src.Width, (float)size / src.Height);
        var newW = (int)MathF.Round(src.Width * Scale);
        var newH = (int)MathF.Round(src.Height * Scale);
        PadX = (size - newW) / 2;
        PadY = (size - newH) / 2;

        using var resized = new Mat();
        Cv2.Resize(src, resized, new Size(newW, newH));

        var dst = new Mat(size, size, MatType.CV_8UC3, new Scalar(114, 114, 114));
        using var roi = new Mat(dst, new Rect(PadX, PadY, newW, newH));
        resized.CopyTo(roi);
        return dst;
    }

    /// <summary>Map a box from letterboxed coords back to the original image.</summary>
    public (float X, float Y, float W, float H) Unmap(float cx, float cy, float w, float h) =>
        ((cx - w / 2f - PadX) / Scale, (cy - h / 2f - PadY) / Scale, w / Scale, h / Scale);
}
