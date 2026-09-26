namespace NdCrosshair.Core;

internal sealed class LayerRaster
{
    private LayerRaster(int size, double[] fill, double[] outline, double[]? shadow, CrosshairColor outlineColor, CrosshairColor shadowColor, double opacity, double[]? image)
    {
        Size = size;
        Fill = fill;
        Outline = outline;
        Shadow = shadow;
        OutlineColor = outlineColor;
        ShadowColor = shadowColor;
        Opacity = opacity;
        Image = image;
    }

    public int Size { get; }

    public int Origin => Size / 2;

    public double[] Fill { get; }

    public double[] Outline { get; }

    public double[]? Shadow { get; }

    public CrosshairColor OutlineColor { get; }

    public CrosshairColor ShadowColor { get; }

    public double Opacity { get; }

    public double[]? Image { get; }

    public static LayerRaster Empty { get; } = new(1, [0], [0], null, CrosshairColor.Black, CrosshairColor.Black, 1, null);

    public static LayerRaster FromCoverage(
        int size,
        double[] fill,
        double[]? outlineCoverage,
        int opacityPercent,
        CrosshairColor outlineColor,
        int shadowSize,
        int shadowOpacityPercent,
        CrosshairColor shadowColor)
    {
        var pixelCount = size * size;
        var opacity = Math.Round(opacityPercent * 255 / 100.0) / 255.0;

        var visibleOutline = new double[pixelCount];
        var alpha = new double[pixelCount];
        for (var i = 0; i < pixelCount; i++)
        {
            visibleOutline[i] = (outlineCoverage?[i] ?? 0) * (1 - fill[i]);
            alpha[i] = (fill[i] + visibleOutline[i]) * opacity;
        }

        double[]? shadow = null;
        if (shadowSize > 0)
        {
            var blurred = Blur(alpha, size, shadowSize);
            var shadowOpacity = shadowOpacityPercent / 100.0;
            shadow = new double[pixelCount];
            for (var i = 0; i < pixelCount; i++)
            {
                shadow[i] = blurred[i] * shadowOpacity * (1 - alpha[i]);
            }
        }

        return new LayerRaster(size, fill, visibleOutline, shadow, outlineColor, shadowColor, opacity, null);
    }

    public static LayerRaster FromImage(int size, double[] premultipliedBgra, int opacityPercent) =>
        new(size, new double[size * size], new double[size * size], null, CrosshairColor.Black, CrosshairColor.Black,
            Math.Round(opacityPercent * 255 / 100.0) / 255.0, premultipliedBgra);

    public LayerRaster Blurred(int radius)
    {
        if (radius <= 0)
        {
            return this;
        }

        var size = Size + radius * 2;
        var image = Image is null ? null : BlurChannels(Pad(Image, 4, radius), size, radius, 4);
        return new LayerRaster(
            size,
            Blur(Pad(Fill, 1, radius), size, radius),
            Blur(Pad(Outline, 1, radius), size, radius),
            Shadow is null ? null : Blur(Pad(Shadow, 1, radius), size, radius),
            OutlineColor,
            ShadowColor,
            Opacity,
            image);
    }

    public void Pixel(int index, CrosshairColor fillColor, out double b, out double g, out double r, out double a)
    {
        if (Image is not null)
        {
            var offset = index * 4;
            b = Image[offset] * Opacity;
            g = Image[offset + 1] * Opacity;
            r = Image[offset + 2] * Opacity;
            a = Image[offset + 3] * Opacity;
            return;
        }

        var f = Fill[index] * Opacity;
        var o = Outline[index] * Opacity;
        var s = Shadow?[index] ?? 0;
        b = (fillColor.B * f + OutlineColor.B * o + ShadowColor.B * s) / 255.0;
        g = (fillColor.G * f + OutlineColor.G * o + ShadowColor.G * s) / 255.0;
        r = (fillColor.R * f + OutlineColor.R * o + ShadowColor.R * s) / 255.0;
        a = f + o + s;
    }

    public static double[] Blur(double[] source, int size, int radius) => BlurChannels(source, size, radius, 1);

    private static double[] BlurChannels(double[] source, int size, int radius, int channels)
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
                for (var c = 0; c < channels; c++)
                {
                    var value = 0.0;
                    for (var k = -radius; k <= radius; k++)
                    {
                        var xx = x + k;
                        if (xx >= 0 && xx < size)
                        {
                            value += source[(y * size + xx) * channels + c] * kernel[k + radius];
                        }
                    }

                    horizontal[(y * size + x) * channels + c] = value;
                }
            }
        }

        var result = new double[source.Length];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                for (var c = 0; c < channels; c++)
                {
                    var value = 0.0;
                    for (var k = -radius; k <= radius; k++)
                    {
                        var yy = y + k;
                        if (yy >= 0 && yy < size)
                        {
                            value += horizontal[(yy * size + x) * channels + c] * kernel[k + radius];
                        }
                    }

                    result[(y * size + x) * channels + c] = value;
                }
            }
        }

        return result;
    }

    private double[] Pad(double[] source, int channels, int padding)
    {
        var size = Size + padding * 2;
        var result = new double[size * size * channels];
        for (var y = 0; y < Size; y++)
        {
            Array.Copy(source, y * Size * channels, result, ((y + padding) * size + padding) * channels, Size * channels);
        }

        return result;
    }
}
