namespace NdCrosshair.Core;

public sealed class CrosshairImage
{
    public const int BytesPerPixel = 4;

    internal CrosshairImage(int size, byte[] pixels)
    {
        Size = size;
        Pixels = pixels;
    }

    public int Size { get; }

    public int Center => Size / 2;

    public int Stride => Size * BytesPerPixel;

    public byte[] Pixels { get; }

    public (byte B, byte G, byte R, byte A) GetPixel(int x, int y)
    {
        var index = (y * Size + x) * BytesPerPixel;
        return (Pixels[index], Pixels[index + 1], Pixels[index + 2], Pixels[index + 3]);
    }
}
