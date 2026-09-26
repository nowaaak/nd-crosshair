namespace NdCrosshair.Core;

public static class CrosshairRenderer
{
    public static CrosshairImage Render(CrosshairSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return Rasterize(settings).Compose(settings.Clamp().Color);
    }

    public static CrosshairLayers Rasterize(CrosshairSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);
        return new CrosshairLayers(RasterizeClassic(settings.Clamp()));
    }

    internal static LayerRaster RasterizeClassic(CrosshairSettings s)
    {
        var outline = s.ShowOutline ? s.OutlineThickness : 0;
        var fills = BuildShapes(s, 0);
        var outlines = outline > 0 ? BuildShapes(s, outline) : [];
        var outer = outline > 0 ? outlines : fills;
        var shadow = s.ShowShadow ? s.ShadowSize : 0;

        var origin = outer.Count == 0 ? 0 : outer.Max(shape => shape.Bounds.MaxExtent) + shadow;
        var size = origin * 2 + 1;

        return LayerRaster.FromCoverage(
            size,
            SuperSampler.Rasterize(fills, size, origin),
            outline > 0 ? SuperSampler.Rasterize(outlines, size, origin) : null,
            s.Opacity,
            s.OutlineColor,
            shadow,
            s.ShadowOpacity,
            s.ShadowColor);
    }

    internal static CrosshairSettings Transform(CrosshairSettings s, int scalePercent, int spread)
    {
        if (scalePercent != 100)
        {
            var factor = scalePercent / 100.0;
            int Scaled(int value, int minimum) => Math.Max(minimum, (int)Math.Round(value * factor, MidpointRounding.AwayFromZero));
            s = s with
            {
                LineLength = Scaled(s.LineLength, 0),
                LineThickness = Scaled(s.LineThickness, 1),
                Gap = Scaled(s.Gap, 0),
                DotSize = Scaled(s.DotSize, 1),
                RingRadius = Scaled(s.RingRadius, 1),
                RingThickness = Scaled(s.RingThickness, 1),
            };
        }

        return spread == 0 ? s : s with { Gap = s.Gap + spread, RingRadius = s.RingRadius + spread };
    }

    private static List<Shape> BuildShapes(CrosshairSettings s, int grow)
    {
        var shapes = new List<Shape>();
        var low = -(s.LineThickness / 2);
        var high = low + s.LineThickness;
        var center = (low + high) / 2.0;
        var gap = s.Gap;
        var length = s.LineLength;

        if (length > 0 && s.ArmCount != CrosshairSettings.DefaultArmCount)
        {
            for (var arm = 0; arm < s.ArmCount; arm++)
            {
                var angle = s.Rotation + arm * 360.0 / s.ArmCount;
                shapes.Add(new RectShape(low - grow, low - gap - length - grow, high + grow, low - gap + grow, angle, center));
            }
        }
        else if (length > 0)
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
}
