namespace NdCrosshair.Core;

internal static class ShapeGeometry
{
    private const double DegreesPerArcSegment = 3;

    public static IReadOnlyList<IReadOnlyList<PointD>> Build(ShapeLayer layer, double scale)
    {
        var width = Scaled(layer.Width, scale);
        var height = layer.Kind == ShapeKind.Arc ? width : Scaled(layer.Height, scale);
        var thickness = Scaled(layer.Thickness, scale);

        var contours = layer.Kind switch
        {
            ShapeKind.Rectangle => Rectangle(width, height, layer.Filled ? 0 : thickness),
            ShapeKind.Triangle => Triangle(width, height, layer.Filled ? 0 : thickness),
            ShapeKind.Chevron => Chevron(width, height, thickness),
            ShapeKind.Arc => Arc(width / 2.0, thickness, layer.Sweep),
            _ => TShape(width, height, thickness),
        };

        var centerX = width % 2 == 1 ? 0.5 : 0;
        var centerY = height % 2 == 1 ? 0.5 : 0;
        var radians = layer.Rotation * Math.PI / 180;
        var cos = Math.Cos(radians);
        var sin = Math.Sin(radians);
        return contours
            .Select(contour => (IReadOnlyList<PointD>)contour
                .Select(point => new PointD(point.X * cos - point.Y * sin + centerX, point.X * sin + point.Y * cos + centerY))
                .ToList())
            .ToList();
    }

    private static int Scaled(int value, double scale) => Math.Max(1, (int)Math.Round(value * scale, MidpointRounding.AwayFromZero));

    private static List<PointD[]> Rectangle(double width, double height, double frame)
    {
        var outer = Box(width / 2, height / 2);
        return frame > 0 && frame * 2 < width && frame * 2 < height
            ? [outer, Box(width / 2 - frame, height / 2 - frame)]
            : [outer];
    }

    private static PointD[] Box(double halfWidth, double halfHeight) =>
    [
        new(-halfWidth, -halfHeight),
        new(halfWidth, -halfHeight),
        new(halfWidth, halfHeight),
        new(-halfWidth, halfHeight),
    ];

    private static List<PointD[]> Triangle(double width, double height, double frame)
    {
        PointD[] outer = [new(0, -height / 2), new(width / 2, height / 2), new(-width / 2, height / 2)];
        if (frame <= 0)
        {
            return [outer];
        }

        var a = Distance(outer[1], outer[2]);
        var b = Distance(outer[2], outer[0]);
        var c = Distance(outer[0], outer[1]);
        var perimeter = a + b + c;
        var incenter = new PointD(
            (a * outer[0].X + b * outer[1].X + c * outer[2].X) / perimeter,
            (a * outer[0].Y + b * outer[1].Y + c * outer[2].Y) / perimeter);
        var inradius = width * height / perimeter;
        if (inradius <= frame)
        {
            return [outer];
        }

        var factor = (inradius - frame) / inradius;
        var inner = outer
            .Select(point => new PointD(incenter.X + (point.X - incenter.X) * factor, incenter.Y + (point.Y - incenter.Y) * factor))
            .ToArray();
        return [outer, inner];
    }

    private static List<PointD[]> Chevron(double width, double height, double thickness)
    {
        var halfWidth = Math.Max(width / 2, 0.5);
        var leg = Math.Sqrt(halfWidth * halfWidth + height * height);
        var baseCut = thickness * leg / height;
        var apexCut = thickness * leg / halfWidth;
        if (baseCut >= halfWidth || apexCut >= height)
        {
            return Triangle(width, height, 0);
        }

        return
        [
            [
                new(0, -height / 2),
                new(halfWidth, height / 2),
                new(halfWidth - baseCut, height / 2),
                new(0, -height / 2 + apexCut),
                new(-halfWidth + baseCut, height / 2),
                new(-halfWidth, height / 2),
            ],
        ];
    }

    private static List<PointD[]> Arc(double radius, double thickness, int sweep)
    {
        var inner = Math.Max(0, radius - thickness);
        if (sweep >= ShapeLayer.MaxSweep)
        {
            var ring = Circle(radius, 0, 360);
            return inner > 0 ? [ring, Circle(inner, 0, 360)] : [ring];
        }

        var start = -90 - sweep / 2.0;
        var outerPoints = Circle(radius, start, sweep);
        var innerPoints = inner > 0 ? Circle(inner, start, sweep).Reverse() : [new PointD(0, 0)];
        return [[.. outerPoints, .. innerPoints]];
    }

    private static PointD[] Circle(double radius, double startDegrees, double sweepDegrees)
    {
        var segments = Math.Max(8, (int)Math.Ceiling(sweepDegrees / DegreesPerArcSegment));
        var closed = sweepDegrees >= 360;
        var count = closed ? segments : segments + 1;
        var points = new PointD[count];
        for (var i = 0; i < count; i++)
        {
            var angle = (startDegrees + sweepDegrees * i / segments) * Math.PI / 180;
            points[i] = new PointD(radius * Math.Cos(angle), radius * Math.Sin(angle));
        }

        return points;
    }

    private static List<PointD[]> TShape(double width, double height, double thickness)
    {
        if (thickness >= height)
        {
            return [Box(width / 2, height / 2)];
        }

        var top = -height / 2;
        var bar = top + thickness;
        var stem = Math.Min(thickness, width) / 2;
        return
        [
            [
                new(-width / 2, top),
                new(width / 2, top),
                new(width / 2, bar),
                new(stem, bar),
                new(stem, height / 2),
                new(-stem, height / 2),
                new(-stem, bar),
                new(-width / 2, bar),
            ],
        ];
    }

    private static double Distance(PointD a, PointD b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));
}
