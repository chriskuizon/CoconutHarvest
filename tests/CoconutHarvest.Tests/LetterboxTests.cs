using CoconutHarvest.Perception;
using OpenCvSharp;
using Xunit;

namespace CoconutHarvest.Tests;

/// <summary>Exercises the real OpenCV native runtime, so it also proves the interop is wired up.</summary>
public class LetterboxTests
{
    private const int Size = 640;
    private const int SrcW = 1280;
    private const int SrcH = 720;

    private static Mat NewSource() => new(SrcH, SrcW, MatType.CV_8UC3, new Scalar(10, 20, 30));

    [Fact]
    public void Pads_to_a_square_and_preserves_aspect_ratio()
    {
        using var src = NewSource();
        var lb = new Letterbox(Size);

        using var dst = lb.Apply(src);

        Assert.Equal(Size, dst.Width);
        Assert.Equal(Size, dst.Height);
        Assert.Equal(0.5f, lb.Scale, 4);  // 640/1280
        Assert.Equal(0, lb.PadX);         // 1280*0.5 = 640, fills the width
        Assert.Equal(140, lb.PadY);       // (640 - 720*0.5) / 2
    }

    [Fact]
    public void Unmap_returns_a_box_to_original_image_coordinates()
    {
        using var src = NewSource();
        var lb = new Letterbox(Size);
        using var dst = lb.Apply(src);

        // 100x50 box centred in the letterboxed image -> centre of the 1280x720 source.
        var (x, y, w, h) = lb.Unmap(cx: 320f, cy: 320f, w: 100f, h: 50f);

        Assert.Equal(200f, w, 3);
        Assert.Equal(100f, h, 3);
        Assert.Equal(SrcW / 2f - 100f, x, 3);
        Assert.Equal(SrcH / 2f - 50f, y, 3);
    }

    [Fact]
    public void Padding_is_the_neutral_grey_ultralytics_trains_with()
    {
        using var src = NewSource();
        var lb = new Letterbox(Size);

        using var dst = lb.Apply(src);

        var pad = dst.Get<Vec3b>(5, 5);            // inside the top letterbox band
        var image = dst.Get<Vec3b>(Size / 2, Size / 2); // inside the copied source
        Assert.Equal(new Vec3b(114, 114, 114), pad);
        Assert.Equal(new Vec3b(10, 20, 30), image);
    }
}
