namespace CoconutHarvest.Contracts;

/// <summary>Axis-aligned box. Coordinates are either normalised (0..1) or pixels; see usage.</summary>
public readonly record struct BoundingBox(float X, float Y, float W, float H)
{
    public float CenterX => X + W / 2f;
    public float CenterY => Y + H / 2f;
    public float Right => X + W;
    public float Bottom => Y + H;
    public float Area => W * H;

    public float IoU(in BoundingBox other)
    {
        var ix = MathF.Max(0, MathF.Min(Right, other.Right) - MathF.Max(X, other.X));
        var iy = MathF.Max(0, MathF.Min(Bottom, other.Bottom) - MathF.Max(Y, other.Y));
        var inter = ix * iy;
        var union = Area + other.Area - inter;
        return union <= 0 ? 0 : inter / union;
    }

    public BoundingBox Normalise(int width, int height) =>
        new(X / width, Y / height, W / width, H / height);
}
