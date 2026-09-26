namespace NdCrosshair.Core;

public static class DesignRenderer
{
    public static CrosshairImage Render(CrosshairDesign design) => Rasterize(design).Compose(null);

    public static RasterizedDesign Rasterize(CrosshairDesign design, int spread = 0)
    {
        ArgumentNullException.ThrowIfNull(design);

        var placed = new List<PlacedLayer>();
        foreach (var layer in design.Normalize().Layers.Where(layer => layer.Visible))
        {
            if (RasterizeLayer(layer, spread) is { } placedLayer)
            {
                placed.Add(placedLayer);
            }
        }

        return new RasterizedDesign(placed);
    }

    private static PlacedLayer? RasterizeLayer(CrosshairLayer layer, int spread) => layer switch
    {
        ClassicLayer classic => Place(
            classic,
            CrosshairRenderer.RasterizeClassic(CrosshairRenderer.Transform(classic.Settings, classic.Scale, spread)),
            new LayerColoring(classic.Settings.Color, classic.Settings.ColorMode, classic.Settings.RainbowSpeed)),
        ShapeLayer shape => Place(shape, RasterizeShape(shape), Coloring(shape.Style)),
        _ => null,
    };

    private static LayerColoring Coloring(LayerStyle style) => new(style.Color, style.ColorMode, style.RainbowSpeed);

    private static LayerRaster RasterizeShape(ShapeLayer shape)
    {
        var contours = ShapeGeometry.Build(shape, shape.Scale / 100.0);
        var style = shape.Style;
        var outline = style.ShowOutline ? style.OutlineThickness : 0;
        var shadow = style.ShowShadow ? style.ShadowSize : 0;
        var fill = new PolygonShape(contours, 0);
        var grown = outline > 0 ? new PolygonShape(contours, outline) : fill;

        var origin = grown.Bounds.MaxExtent + shadow;
        var size = origin * 2 + 1;
        return LayerRaster.FromCoverage(
            size,
            SuperSampler.Rasterize([fill], size, origin),
            outline > 0 ? SuperSampler.Rasterize([grown], size, origin) : null,
            style.Opacity,
            style.OutlineColor,
            shadow,
            style.ShadowOpacity,
            style.ShadowColor);
    }

    private static PlacedLayer Place(CrosshairLayer layer, LayerRaster raster, LayerColoring coloring) =>
        new(raster.Blurred(layer.Blur), layer.OffsetX, layer.OffsetY, coloring);
}

internal readonly record struct LayerColoring(CrosshairColor Color, CrosshairColorMode Mode, int RainbowSpeed)
{
    public bool IsAnimated => Mode == CrosshairColorMode.Rainbow;

    public CrosshairColor At(TimeSpan? time) =>
        IsAnimated && time is { } elapsed ? ColorMath.Rainbow(Color, RainbowSpeed, elapsed.TotalSeconds) : Color;
}

internal sealed record PlacedLayer(LayerRaster Raster, int OffsetX, int OffsetY, LayerColoring Coloring);

public sealed class RasterizedDesign
{
    private readonly IReadOnlyList<PlacedLayer> layers;

    internal RasterizedDesign(IReadOnlyList<PlacedLayer> layers)
    {
        this.layers = layers;
        var extent = 0;
        foreach (var layer in layers)
        {
            var origin = layer.Raster.Origin;
            extent = Math.Max(extent, Math.Abs(layer.OffsetX) + origin);
            extent = Math.Max(extent, Math.Abs(layer.OffsetY) + origin);
        }

        Size = extent * 2 + 1;
        IsAnimated = layers.Any(layer => layer.Coloring.IsAnimated);
    }

    public int Size { get; }

    public bool IsAnimated { get; }

    public CrosshairImage Compose(TimeSpan? time)
    {
        var center = Size / 2;
        var pixelCount = Size * Size;
        var canvas = new double[pixelCount * 4];

        foreach (var layer in layers)
        {
            var raster = layer.Raster;
            var fill = layer.Coloring.At(time);
            var left = center + layer.OffsetX - raster.Origin;
            var top = center + layer.OffsetY - raster.Origin;
            for (var y = 0; y < raster.Size; y++)
            {
                var row = (top + y) * Size + left;
                for (var x = 0; x < raster.Size; x++)
                {
                    raster.Pixel(y * raster.Size + x, fill, out var b, out var g, out var r, out var a);
                    if (a <= 0)
                    {
                        continue;
                    }

                    var index = (row + x) * 4;
                    var keep = 1 - a;
                    canvas[index] = b + canvas[index] * keep;
                    canvas[index + 1] = g + canvas[index + 1] * keep;
                    canvas[index + 2] = r + canvas[index + 2] * keep;
                    canvas[index + 3] = a + canvas[index + 3] * keep;
                }
            }
        }

        var pixels = new byte[pixelCount * CrosshairImage.BytesPerPixel];
        for (var i = 0; i < canvas.Length; i++)
        {
            pixels[i] = CrosshairImage.ToByte(canvas[i]);
        }

        return new CrosshairImage(Size, pixels);
    }
}
