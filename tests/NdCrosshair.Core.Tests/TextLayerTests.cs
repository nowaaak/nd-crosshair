namespace NdCrosshair.Core.Tests;

public class TextLayerTests
{
    [Fact]
    public void WithoutContentProvider_TextLayersAreSkipped()
    {
        var design = new CrosshairDesign { Layers = [new TextLayer()] };

        var image = DesignRenderer.Render(design);

        Assert.Equal(1, image.Size);
        Assert.Equal(0, image.GetPixel(0, 0).A);
    }

    [Fact]
    public void TextMask_IsCenteredAndColored()
    {
        var provider = new BlockProvider(7, 5);
        var layer = new TextLayer { Style = new LayerStyle { ShowOutline = false, Color = new CrosshairColor(255, 0, 0) } };

        var image = DesignRenderer.Render(new CrosshairDesign { Layers = [layer] }, provider);
        var c = image.Center;

        Assert.Equal(35, Enumerable.Range(0, image.Size * image.Size).Count(i => image.Pixels[i * 4 + 3] == 255));
        Assert.Equal((0, 0, 255, 255), image.GetPixel(c, c));
        Assert.Equal(255, image.GetPixel(c - 3, c - 2).A);
        Assert.Equal(0, image.GetPixel(c - 4, c).A);
    }

    [Fact]
    public void Outline_GrowsAroundTheMask()
    {
        var provider = new BlockProvider(5, 5);
        var layer = new TextLayer { Style = new LayerStyle { OutlineThickness = 2 } };

        var image = DesignRenderer.Render(new CrosshairDesign { Layers = [layer] }, provider);
        var c = image.Center;

        Assert.Equal((0, 0, 0, 255), image.GetPixel(c - 3, c));
        Assert.InRange(image.GetPixel(c - 4, c).A, 1, 254);
        Assert.Equal(0, image.GetPixel(c - 6, c).A);
    }

    [Fact]
    public void Provider_ReceivesLayerAndScale()
    {
        var provider = new BlockProvider(3, 3);
        var layer = new TextLayer { Text = "AB", Rotation = 45, Scale = 150 };

        DesignRenderer.Render(new CrosshairDesign { Layers = [layer] }, provider);

        Assert.Equal("AB", provider.LastLayer?.Text);
        Assert.Equal(45, provider.LastLayer?.Rotation);
        Assert.Equal(1.5, provider.LastScale);
    }

    [Fact]
    public void Normalize_LimitsTextAndFont()
    {
        var layer = (TextLayer)new TextLayer { Text = new string('x', 100) + "\n", FontFamily = "  ", FontSize = 1000 }.Normalize();

        Assert.Equal(TextLayer.MaxTextLength, layer.Text.Length);
        Assert.Equal(TextLayer.DefaultFontFamily, layer.FontFamily);
        Assert.Equal(TextLayer.MaxFontSize, layer.FontSize);
    }

    [Fact]
    public void TextLayers_RoundTripThroughShareCode()
    {
        var design = new CrosshairDesign { Layers = [new ClassicLayer(), new TextLayer { Text = "GG ✓", FontFamily = "Consolas", OffsetY = -20 }] };

        Assert.True(DesignShareCode.TryDecode(DesignShareCode.Encode(design), out var decoded, out _));
        Assert.Equal(design.Normalize(), decoded);
    }

    [Fact]
    public void CoverageMask_RejectsMismatchedSize()
    {
        Assert.Throws<ArgumentException>(() => new CoverageMask(2, 2, new double[3]));
    }

    private sealed class BlockProvider(int width, int height) : ILayerContentProvider
    {
        public TextLayer? LastLayer { get; private set; }

        public double LastScale { get; private set; }

        public CoverageMask RenderText(TextLayer layer, double scale)
        {
            LastLayer = layer;
            LastScale = scale;
            return new CoverageMask(width, height, Enumerable.Repeat(1.0, width * height).ToArray());
        }
    }
}
