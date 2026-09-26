namespace NdCrosshair.Core;

public interface ILayerContentProvider
{
    CoverageMask? RenderText(TextLayer layer, double scale);

    ImageFrame? RenderImage(ImageLayer layer, double scale, TimeSpan? time);

    bool IsAnimated(ImageLayer layer);
}

public sealed class CoverageMask
{
    public CoverageMask(int width, int height, double[] coverage)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentNullException.ThrowIfNull(coverage);
        if (coverage.Length != width * height)
        {
            throw new ArgumentException("Coverage size does not match the mask size.", nameof(coverage));
        }

        Width = width;
        Height = height;
        Coverage = coverage;
    }

    public int Width { get; }

    public int Height { get; }

    public double[] Coverage { get; }
}

public sealed class ImageFrame
{
    public ImageFrame(int width, int height, byte[] premultipliedBgra)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(width);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(height);
        ArgumentNullException.ThrowIfNull(premultipliedBgra);
        if (premultipliedBgra.Length != width * height * 4)
        {
            throw new ArgumentException("Pixel data does not match the frame size.", nameof(premultipliedBgra));
        }

        Width = width;
        Height = height;
        Pixels = premultipliedBgra;
    }

    public int Width { get; }

    public int Height { get; }

    public byte[] Pixels { get; }
}
