using CoconutHarvest.Contracts;

namespace CoconutHarvest.Perception;

/// <summary>
/// Converts a normalised bbox centre into camera-frame bearing/elevation using a pinhole
/// approximation. Replace with calibrated intrinsics (OpenCV calibrateCamera) before flight.
/// </summary>
public sealed class CameraGeometry(float hfovDeg, float vfovDeg)
{
    private readonly float _tanHalfH = MathF.Tan(hfovDeg * MathF.PI / 360f);
    private readonly float _tanHalfV = MathF.Tan(vfovDeg * MathF.PI / 360f);

    /// <returns>Bearing: +right. Elevation: +up. Degrees.</returns>
    public (float BearingDeg, float ElevationDeg) Angles(in BoundingBox norm)
    {
        var nx = (norm.CenterX - 0.5f) * 2f; // -1..1
        var ny = (0.5f - norm.CenterY) * 2f; // -1..1, up positive
        var bearing = MathF.Atan(nx * _tanHalfH) * 180f / MathF.PI;
        var elevation = MathF.Atan(ny * _tanHalfV) * 180f / MathF.PI;
        return (bearing, elevation);
    }
}
