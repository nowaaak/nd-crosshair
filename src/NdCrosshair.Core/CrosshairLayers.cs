namespace NdCrosshair.Core;

public sealed class CrosshairLayers
{
    private readonly double[] fill;
    private readonly double[] outline;
    private readonly double[]? shadow;
    private readonly CrosshairColor outlineColor;
    private readonly CrosshairColor shadowColor;
    private readonly double opacity;

    internal CrosshairLayers(
        int size,
        double[] fill,
        double[] outline,
        double[]? shadow,
        CrosshairColor outlineColor,
        CrosshairColor shadowColor,
        double opacity)
    {
        Size = size;
        this.fill = fill;
        this.outline = outline;
        this.shadow = shadow;
        this.outlineColor = outlineColor;
        this.shadowColor = shadowColor;
        this.opacity = opacity;
    }

    public int Size { get; }

    public CrosshairImage Compose(CrosshairColor fillColor)
    {
        var pixelCount = Size * Size;
        var pixels = new byte[pixelCount * CrosshairImage.BytesPerPixel];

        for (var i = 0; i < pixelCount; i++)
        {
            var f = fill[i] * opacity;
            var o = outline[i] * opacity;
            var s = shadow?[i] ?? 0;

            var index = i * CrosshairImage.BytesPerPixel;
            pixels[index] = ToByte((fillColor.B * f + outlineColor.B * o + shadowColor.B * s) / 255.0);
            pixels[index + 1] = ToByte((fillColor.G * f + outlineColor.G * o + shadowColor.G * s) / 255.0);
            pixels[index + 2] = ToByte((fillColor.R * f + outlineColor.R * o + shadowColor.R * s) / 255.0);
            pixels[index + 3] = ToByte(f + o + s);
        }

        return new CrosshairImage(Size, pixels);
    }

    private static byte ToByte(double value) => (byte)Math.Clamp(Math.Round(value * 255), 0, 255);
}
