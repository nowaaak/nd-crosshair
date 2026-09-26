namespace NdCrosshair.Core;

public static class DesignRenderer
{
    public static CrosshairImage Render(CrosshairDesign design, ILayerContentProvider? content = null) =>
        Rasterize(design, 0, content).Compose(null);

    public static RasterizedDesign Rasterize(CrosshairDesign design, int spread = 0, ILayerContentProvider? content = null)
    {
        ArgumentNullException.ThrowIfNull(design);

        var placed = new List<PlacedLayer>();
        foreach (var layer in design.Normalize().Layers.Where(layer => layer.Visible))
        {
            if (RasterizeLayer(layer, spread, content) is { } placedLayer)
            {
                placed.Add(placedLayer);
            }
        }

        return new RasterizedDesign(placed);
    }

    private static PlacedLayer? RasterizeLayer(CrosshairLayer layer, int spread, ILayerContentProvider? content) => layer switch
    {
        ClassicLayer classic => Place(
            classic,
            CrosshairRenderer.RasterizeClassic(CrosshairRenderer.Transform(classic.Settings, classic.Scale, spread)),
            new LayerColoring(classic.Settings.Color, classic.Settings.ColorMode, classic.Settings.RainbowSpeed)),
        ShapeLayer shape => Place(shape, RasterizeShape(shape), Coloring(shape.Style)),
        TextLayer text when content?.RenderText(text, text.Scale / 100.0) is { } mask =>
            Place(text, RasterizeMask(mask, text.Style), Coloring(text.Style)),
        ImageLayer image when content?.RenderImage(image, image.Scale / 100.0, null) is { } frame =>
            PlaceImage(image, frame, content),
        _ => null,
    };

    private static PlacedLayer PlaceImage(ImageLayer image, ImageFrame first, ILayerContentProvider content)
    {
        var scale = image.Scale / 100.0;
        Func<TimeSpan, LayerRaster?>? frames = content.IsAnimated(image)
            ? time => content.RenderImage(image, scale, time) is { } frame ? RasterizeImage(frame, image) : null
            : null;
        return new PlacedLayer(
            RasterizeImage(first, image),
            image.OffsetX,
            image.OffsetY,
            new LayerColoring(CrosshairColor.Black, CrosshairColorMode.Static, CrosshairSettings.MinRainbowSpeed),
            frames);
    }

    private static LayerRaster RasterizeImage(ImageFrame frame, ImageLayer image)
    {
        var origin = Math.Max(frame.Width, frame.Height) / 2 + 1;
        var size = origin * 2 + 1;
        var pixels = new double[size * size * 4];
        var left = origin - frame.Width / 2;
        var top = origin - frame.Height / 2;
        for (var y = 0; y < frame.Height; y++)
        {
            for (var x = 0; x < frame.Width * 4; x++)
            {
                pixels[((top + y) * size + left) * 4 + x] = frame.Pixels[y * frame.Width * 4 + x] / 255.0;
            }
        }

        return LayerRaster.FromImage(size, pixels, image.Opacity).Blurred(image.Blur);
    }

    private static LayerColoring Coloring(LayerStyle style) => new(style.Color, style.ColorMode, style.RainbowSpeed);

    private static LayerRaster RasterizeMask(CoverageMask mask, LayerStyle style)
    {
        var outline = style.ShowOutline ? style.OutlineThickness : 0;
        var shadow = style.ShowShadow ? style.ShadowSize : 0;
        var origin = Math.Max(mask.Width, mask.Height) / 2 + 1 + outline + shadow;
        var size = origin * 2 + 1;

        var fill = new double[size * size];
        var left = origin - mask.Width / 2;
        var top = origin - mask.Height / 2;
        for (var y = 0; y < mask.Height; y++)
        {
            Array.Copy(mask.Coverage, y * mask.Width, fill, (top + y) * size + left, mask.Width);
        }

        return LayerRaster.FromCoverage(
            size,
            fill,
            outline > 0 ? Dilate(fill, size, outline) : null,
            style.Opacity,
            style.OutlineColor,
            shadow,
            style.ShadowOpacity,
            style.ShadowColor);
    }

    private static double[] Dilate(double[] source, int size, int radius)
    {
        var reach = radius + 1;
        var offsets = new List<(int Dx, int Dy, double Weight)>();
        for (var dy = -reach; dy <= reach; dy++)
        {
            for (var dx = -reach; dx <= reach; dx++)
            {
                var weight = Math.Clamp(radius + 0.5 - Math.Sqrt(dx * dx + dy * dy), 0, 1);
                if (weight > 0)
                {
                    offsets.Add((dx, dy, weight));
                }
            }
        }

        var result = new double[source.Length];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var value = 0.0;
                foreach (var (dx, dy, weight) in offsets)
                {
                    var sx = x + dx;
                    var sy = y + dy;
                    if (sx >= 0 && sx < size && sy >= 0 && sy < size)
                    {
                        value = Math.Max(value, source[sy * size + sx] * weight);
                    }
                }

                result[y * size + x] = value;
            }
        }

        return result;
    }

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

internal sealed record PlacedLayer(LayerRaster Raster, int OffsetX, int OffsetY, LayerColoring Coloring, Func<TimeSpan, LayerRaster?>? Frames = null)
{
    public bool IsAnimated => Coloring.IsAnimated || Frames is not null;

    public LayerRaster RasterAt(TimeSpan? time) =>
        Frames is not null && time is { } elapsed && Frames(elapsed) is { } frame && frame.Size == Raster.Size ? frame : Raster;
}

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
        IsAnimated = layers.Any(layer => layer.IsAnimated);
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
            var raster = layer.RasterAt(time);
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
