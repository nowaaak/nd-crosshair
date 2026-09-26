namespace NdCrosshair.Core;

public sealed class CrosshairLayers
{
    private readonly LayerRaster raster;

    internal CrosshairLayers(LayerRaster raster)
    {
        this.raster = raster;
    }

    public int Size => raster.Size;

    public CrosshairImage Compose(CrosshairColor fillColor)
    {
        var pixelCount = Size * Size;
        var pixels = new byte[pixelCount * CrosshairImage.BytesPerPixel];

        for (var i = 0; i < pixelCount; i++)
        {
            raster.Pixel(i, fillColor, out var b, out var g, out var r, out var a);
            var index = i * CrosshairImage.BytesPerPixel;
            pixels[index] = CrosshairImage.ToByte(b);
            pixels[index + 1] = CrosshairImage.ToByte(g);
            pixels[index + 2] = CrosshairImage.ToByte(r);
            pixels[index + 3] = CrosshairImage.ToByte(a);
        }

        return new CrosshairImage(Size, pixels);
    }
}
