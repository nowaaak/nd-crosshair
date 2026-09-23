using System.Numerics;

namespace NdCrosshair.Core;

public static class CrosshairRenderer
{
    private const int SamplesPerAxis = 4;
    private const double SampleCount = SamplesPerAxis * SamplesPerAxis;
    private const double BoundsEpsilon = 1e-9;

    public static CrosshairImage Render(CrosshairSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return Rasterize(settings).Compose(settings.Clamp().Color);
    }

    public static CrosshairLayers Rasterize(CrosshairSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var s = settings.Clamp();
        var outline = s.ShowOutline ? s.OutlineThickness : 0;
        var fills = BuildShapes(s, 0);
        var outlines = outline > 0 ? BuildShapes(s, outline) : [];
        var outer = outline > 0 ? outlines : fills;
        var shadow = s.ShowShadow ? s.ShadowSize : 0;

        var origin = outer.Count == 0 ? 0 : outer.Max(shape => shape.Bounds.MaxExtent) + shadow;
        var size = origin * 2 + 1;
        var pixelCount = size * size;

        var fillCoverage = Rasterize(fills, size, origin);
        var outlineCoverage = outline > 0 ? Rasterize(outlines, size, origin) : new double[pixelCount];
        var opacity = Math.Round(s.Opacity * 255 / 100.0) / 255.0;

        var visibleOutline = new double[pixelCount];
        var alpha = new double[pixelCount];
        for (var i = 0; i < pixelCount; i++)
        {
            visibleOutline[i] = outlineCoverage[i] * (1 - fillCoverage[i]);
            alpha[i] = (fillCoverage[i] + visibleOutline[i]) * opacity;
        }

        double[]? shadowAlpha = null;
        if (shadow > 0)
        {
            var blurred = Blur(alpha, size, shadow);
            var shadowOpacity = s.ShadowOpacity / 100.0;
            shadowAlpha = new double[pixelCount];
            for (var i = 0; i < pixelCount; i++)
            {
                shadowAlpha[i] = blurred[i] * shadowOpacity * (1 - alpha[i]);
            }
        }

        return new CrosshairLayers(size, fillCoverage, visibleOutline, shadowAlpha, s.OutlineColor, s.ShadowColor, opacity);
    }

    private static List<Shape> BuildShapes(CrosshairSettings s, int grow)
    {
        var shapes = new List<Shape>();
        var low = -(s.LineThickness / 2);
        var high = low + s.LineThickness;
        var center = (low + high) / 2.0;
        var gap = s.Gap;
        var length = s.LineLength;

        if (length > 0)
        {
            if (s.ShowTop)
            {
                shapes.Add(new RectShape(low - grow, low - gap - length - grow, high + grow, low - gap + grow, s.Rotation, center));
            }

            if (s.ShowBottom)
            {
                shapes.Add(new RectShape(low - grow, high + gap - grow, high + grow, high + gap + length + grow, s.Rotation, center));
            }

            if (s.ShowLeft)
            {
                shapes.Add(new RectShape(low - gap - length - grow, low - grow, low - gap + grow, high + grow, s.Rotation, center));
            }

            if (s.ShowRight)
            {
                shapes.Add(new RectShape(high + gap - grow, low - grow, high + gap + length + grow, high + grow, s.Rotation, center));
            }
        }

        if (s.ShowDot)
        {
            var dotLow = -(s.DotSize / 2);
            var dotHigh = dotLow + s.DotSize;
            var dotCenter = (dotLow + dotHigh) / 2.0;
            shapes.Add(s.RoundDot
                ? new RingShape(dotCenter, 0, s.DotSize / 2.0 + grow)
                : new RectShape(dotLow - grow, dotLow - grow, dotHigh + grow, dotHigh + grow, s.Rotation, dotCenter));
        }

        if (s.ShowRing)
        {
            var halfThickness = s.RingThickness / 2.0;
            shapes.Add(new RingShape(center, s.RingRadius - halfThickness - grow, s.RingRadius + halfThickness + grow));
        }

        return shapes;
    }

    private static double[] Rasterize(List<Shape> shapes, int size, int origin)
    {
        var masks = new ushort[size * size];
        foreach (var shape in shapes)
        {
            var bounds = shape.Bounds;
            for (var py = bounds.MinY; py <= bounds.MaxY; py++)
            {
                var row = (py + origin) * size + origin;
                for (var px = bounds.MinX; px <= bounds.MaxX; px++)
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

    private static double[] Blur(double[] source, int size, int radius)
    {
        var sigma = Math.Max(0.5, radius / 2.0);
        var kernel = new double[radius * 2 + 1];
        var sum = 0.0;
        for (var k = -radius; k <= radius; k++)
        {
            kernel[k + radius] = Math.Exp(-(k * k) / (2 * sigma * sigma));
            sum += kernel[k + radius];
        }

        for (var k = 0; k < kernel.Length; k++)
        {
            kernel[k] /= sum;
        }

        var horizontal = new double[source.Length];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var value = 0.0;
                for (var k = -radius; k <= radius; k++)
                {
                    var xx = x + k;
                    if (xx >= 0 && xx < size)
                    {
                        value += source[y * size + xx] * kernel[k + radius];
                    }
                }

                horizontal[y * size + x] = value;
            }
        }

        var result = new double[source.Length];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var value = 0.0;
                for (var k = -radius; k <= radius; k++)
                {
                    var yy = y + k;
                    if (yy >= 0 && yy < size)
                    {
                        value += horizontal[yy * size + x] * kernel[k + radius];
                    }
                }

                result[y * size + x] = value;
            }
        }

        return result;
    }

    private readonly record struct PixelBounds(int MinX, int MinY, int MaxX, int MaxY)
    {
        public int MaxExtent => Math.Max(Math.Max(-MinX, -MinY), Math.Max(MaxX, MaxY));

        public static PixelBounds FromEdges(double minX, double minY, double maxX, double maxY) => new(
            (int)Math.Floor(minX + BoundsEpsilon),
            (int)Math.Floor(minY + BoundsEpsilon),
            (int)Math.Ceiling(maxX - BoundsEpsilon) - 1,
            (int)Math.Ceiling(maxY - BoundsEpsilon) - 1);
    }

    private abstract class Shape
    {
        public abstract PixelBounds Bounds { get; }

        public abstract bool Contains(double x, double y);
    }

    private sealed class RectShape : Shape
    {
        private readonly double left;
        private readonly double top;
        private readonly double right;
        private readonly double bottom;
        private readonly double pivot;
        private readonly double cos;
        private readonly double sin;

        public RectShape(double left, double top, double right, double bottom, int rotationDegrees, double pivot)
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

    private sealed class RingShape : Shape
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
}
