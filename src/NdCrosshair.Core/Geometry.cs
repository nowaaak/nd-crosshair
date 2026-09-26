using System.Numerics;

namespace NdCrosshair.Core;

internal readonly record struct PixelBounds(int MinX, int MinY, int MaxX, int MaxY)
{
    private const double Epsilon = 1e-9;

    public int MaxExtent => Math.Max(Math.Max(-MinX, -MinY), Math.Max(MaxX, MaxY));

    public static PixelBounds FromEdges(double minX, double minY, double maxX, double maxY) => new(
        (int)Math.Floor(minX + Epsilon),
        (int)Math.Floor(minY + Epsilon),
        (int)Math.Ceiling(maxX - Epsilon) - 1,
        (int)Math.Ceiling(maxY - Epsilon) - 1);
}

internal abstract class Shape
{
    public abstract PixelBounds Bounds { get; }

    public abstract bool Contains(double x, double y);
}

internal sealed class RectShape : Shape
{
    private readonly double left;
    private readonly double top;
    private readonly double right;
    private readonly double bottom;
    private readonly double pivot;
    private readonly double cos;
    private readonly double sin;

    public RectShape(double left, double top, double right, double bottom, double rotationDegrees, double pivot)
    {
        this.left = left;
        this.top = top;
        this.right = right;
        this.bottom = bottom;
        this.pivot = pivot;

        var radians = rotationDegrees * Math.PI / 180;
        cos = rotationDegrees == 0 ? 1 : Math.Cos(radians);
        sin = rotationDegrees == 0 ? 0 : Math.Sin(radians);

        double[] xs = [RotateX(left, top), RotateX(right, top), RotateX(left, bottom), RotateX(right, bottom)];
        double[] ys = [RotateY(left, top), RotateY(right, top), RotateY(left, bottom), RotateY(right, bottom)];
        Bounds = PixelBounds.FromEdges(xs.Min(), ys.Min(), xs.Max(), ys.Max());
    }

    public override PixelBounds Bounds { get; }

    public override bool Contains(double x, double y)
    {
        var dx = x - pivot;
        var dy = y - pivot;
        var localX = dx * cos + dy * sin + pivot;
        var localY = -dx * sin + dy * cos + pivot;
        return localX >= left && localX < right && localY >= top && localY < bottom;
    }

    private double RotateX(double x, double y) => (x - pivot) * cos - (y - pivot) * sin + pivot;

    private double RotateY(double x, double y) => (x - pivot) * sin + (y - pivot) * cos + pivot;
}

internal sealed class RingShape : Shape
{
    private readonly double center;
    private readonly double innerSquared;
    private readonly double outerSquared;

    public RingShape(double center, double innerRadius, double outerRadius)
    {
        this.center = center;
        innerSquared = innerRadius > 0 ? innerRadius * innerRadius : -1;
        outerSquared = outerRadius * outerRadius;
        Bounds = PixelBounds.FromEdges(center - outerRadius, center - outerRadius, center + outerRadius, center + outerRadius);
    }

    public override PixelBounds Bounds { get; }

    public override bool Contains(double x, double y)
    {
        var dx = x - center;
        var dy = y - center;
        var distanceSquared = dx * dx + dy * dy;
        return distanceSquared < outerSquared && distanceSquared >= innerSquared;
    }
}

internal readonly record struct PointD(double X, double Y);

internal sealed class PolygonShape : Shape
{
    private readonly PointD[][] contours;
    private readonly double grow;
    private readonly double growSquared;

    public PolygonShape(IReadOnlyList<IReadOnlyList<PointD>> contours, double grow)
    {
        this.contours = contours.Select(contour => contour.ToArray()).Where(contour => contour.Length >= 3).ToArray();
        this.grow = Math.Max(0, grow);
        growSquared = this.grow * this.grow;

        var points = this.contours.SelectMany(contour => contour).ToList();
        Bounds = points.Count == 0
            ? new PixelBounds(0, 0, -1, -1)
            : PixelBounds.FromEdges(
                points.Min(point => point.X) - this.grow,
                points.Min(point => point.Y) - this.grow,
                points.Max(point => point.X) + this.grow,
                points.Max(point => point.Y) + this.grow);
    }

    public override PixelBounds Bounds { get; }

    public override bool Contains(double x, double y)
    {
        if (IsInside(x, y))
        {
            return true;
        }

        return grow > 0 && IsNearEdge(x, y);
    }

    private bool IsInside(double x, double y)
    {
        var inside = false;
        foreach (var contour in contours)
        {
            for (int i = 0, j = contour.Length - 1; i < contour.Length; j = i++)
            {
                var a = contour[i];
                var b = contour[j];
                if ((a.Y > y) != (b.Y > y) && x < (b.X - a.X) * (y - a.Y) / (b.Y - a.Y) + a.X)
                {
                    inside = !inside;
                }
            }
        }

        return inside;
    }

    private bool IsNearEdge(double x, double y)
    {
        foreach (var contour in contours)
        {
            for (int i = 0, j = contour.Length - 1; i < contour.Length; j = i++)
            {
                if (DistanceSquared(x, y, contour[j], contour[i]) <= growSquared)
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static double DistanceSquared(double x, double y, PointD a, PointD b)
    {
        var dx = b.X - a.X;
        var dy = b.Y - a.Y;
        var lengthSquared = dx * dx + dy * dy;
        var t = lengthSquared == 0 ? 0 : Math.Clamp(((x - a.X) * dx + (y - a.Y) * dy) / lengthSquared, 0, 1);
        var px = a.X + t * dx - x;
        var py = a.Y + t * dy - y;
        return px * px + py * py;
    }
}

internal static class SuperSampler
{
    private const int SamplesPerAxis = 4;
    private const double SampleCount = SamplesPerAxis * SamplesPerAxis;

    public static double[] Rasterize(IReadOnlyList<Shape> shapes, int size, int origin)
    {
        var masks = new ushort[size * size];
        foreach (var shape in shapes)
        {
            var bounds = shape.Bounds;
            var minY = Math.Max(bounds.MinY, -origin);
            var maxY = Math.Min(bounds.MaxY, size - 1 - origin);
            var minX = Math.Max(bounds.MinX, -origin);
            var maxX = Math.Min(bounds.MaxX, size - 1 - origin);
            for (var py = minY; py <= maxY; py++)
            {
                var row = (py + origin) * size + origin;
                for (var px = minX; px <= maxX; px++)
                {
                    var mask = 0;
                    for (var sy = 0; sy < SamplesPerAxis; sy++)
                    {
                        var y = py + (sy + 0.5) / SamplesPerAxis;
                        for (var sx = 0; sx < SamplesPerAxis; sx++)
                        {
                            if (shape.Contains(px + (sx + 0.5) / SamplesPerAxis, y))
                            {
                                mask |= 1 << (sy * SamplesPerAxis + sx);
                            }
                        }
                    }

                    masks[row + px] |= (ushort)mask;
                }
            }
        }

        var coverage = new double[masks.Length];
        for (var i = 0; i < masks.Length; i++)
        {
            coverage[i] = BitOperations.PopCount(masks[i]) / SampleCount;
        }

        return coverage;
    }
}
