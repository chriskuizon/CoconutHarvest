using CoconutHarvest.Perception;
using Microsoft.ML.OnnxRuntime.Tensors;
using OpenCvSharp;
using Xunit;

namespace CoconutHarvest.Tests;

public class YoloInputTests
{
    private const int Size = 64;

    [Fact]
    public void Fill_input_matches_per_pixel_bgr_to_rgb_chw_conversion()
    {
        using var img = new Mat(Size, Size, MatType.CV_8UC3);
        Cv2.Randu(img, Scalar.All(0), Scalar.All(256));

        // Reference: the original per-pixel conversion.
        var expected = new DenseTensor<float>([1, 3, Size, Size]);
        img.GetArray(out Vec3b[] pixels);
        for (var i = 0; i < pixels.Length; i++)
        {
            var y = i / Size;
            var x = i % Size;
            expected[0, 0, y, x] = pixels[i].Item2 / 255f; // R
            expected[0, 1, y, x] = pixels[i].Item1 / 255f; // G
            expected[0, 2, y, x] = pixels[i].Item0 / 255f; // B
        }

        var actual = new DenseTensor<float>([1, 3, Size, Size]);
        YoloDetector.FillInput(img, actual.Buffer.Span, Size);

        Assert.Equal(expected.Buffer.ToArray(), actual.Buffer.ToArray());
    }

    [Fact]
    public void Fill_input_rejects_an_image_that_is_not_letterboxed_bgr()
    {
        var buffer = new float[3 * Size * Size];
        using var gray = new Mat(Size, Size, MatType.CV_8UC1);
        using var wrongSize = new Mat(Size, Size * 2, MatType.CV_8UC3);

        Assert.Throws<ArgumentException>(() => YoloDetector.FillInput(gray, buffer, Size));
        Assert.Throws<ArgumentException>(() => YoloDetector.FillInput(wrongSize, buffer, Size));
    }
}
