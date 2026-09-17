using CoconutHarvest.Contracts;
using CoconutHarvest.Perception;
using Xunit;

namespace CoconutHarvest.Tests;

public class CameraGeometryTests
{
    private static readonly CameraGeometry Geom = new(hfovDeg: 90f, vfovDeg: 60f);

    /// <summary>Box of the given size centred on (cx, cy) -- Angles() works off the centre.</summary>
    private static BoundingBox Centred(float cx, float cy, float w = 0.1f, float h = 0.1f) =>
        new(cx - w / 2f, cy - h / 2f, w, h);

    [Fact]
    public void Centre_of_frame_is_zero_bearing_and_elevation()
    {
        var (bearing, elevation) = Geom.Angles(Centred(0.5f, 0.5f));

        Assert.Equal(0f, bearing, 4);
        Assert.Equal(0f, elevation, 4);
    }

    [Fact]
    public void Frame_edges_map_to_half_the_field_of_view()
    {
        var (right, _) = Geom.Angles(Centred(1.0f, 0.5f));
        var (left, _) = Geom.Angles(Centred(0.0f, 0.5f));

        Assert.Equal(45f, right, 3);   // hfov 90 => +/- 45 at the edges
        Assert.Equal(-45f, left, 3);
    }

    [Fact]
    public void Elevation_is_positive_above_centre_and_negative_below()
    {
        var (_, top) = Geom.Angles(Centred(0.5f, 0.0f));
        var (_, bottom) = Geom.Angles(Centred(0.5f, 1.0f));

        Assert.Equal(30f, top, 3);     // vfov 60 => +/- 30, y inverted so top is +up
        Assert.Equal(-30f, bottom, 3);
    }

    [Fact]
    public void Bearing_is_symmetric_about_the_optical_axis()
    {
        var (right, _) = Geom.Angles(Centred(0.75f, 0.5f));
        var (left, _) = Geom.Angles(Centred(0.25f, 0.5f));

        Assert.True(right > 0, "right of centre must be a positive bearing");
        Assert.Equal(-right, left, 4);
    }
}
