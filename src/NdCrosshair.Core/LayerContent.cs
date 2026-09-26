namespace NdCrosshair.Core;

public interface ILayerContentProvider
{
    CoverageMask? RenderText(TextLayer layer, double scale);
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
