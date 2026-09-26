namespace NdCrosshair.Core.Tests;

public class ImageLayerTests
{
    private static readonly ImageFrame RedFrame = Solid(4, 2, 0, 0, 255, 255);
    private static readonly ImageFrame BlueFrame = Solid(4, 2, 255, 0, 0, 255);

    [Fact]
    public void ImageFrame_IsCenteredWithOriginalColors()
    {
        var image = DesignRenderer.Render(new CrosshairDesign { Layers = [new ImageLayer { FileName = "a.png" }] }, new FrameProvider(RedFrame));
        var c = image.Center;

        Assert.Equal(8, Enumerable.Range(0, image.Size * image.Size).Count(i => image.Pixels[i * 4 + 3] == 255));
        Assert.Equal((0, 0, 255, 255), image.GetPixel(c, c));
        Assert.Equal((0, 0, 255, 255), image.GetPixel(c - 2, c - 1));
        Assert.Equal(0, image.GetPixel(c + 2, c).A);
    }

    [Fact]
    public void Opacity_FadesTheImage()
    {
        var image = DesignRenderer.Render(new CrosshairDesign { Layers = [new ImageLayer { FileName = "a.png", Opacity = 50 }] }, new FrameProvider(RedFrame));

        Assert.Equal((0, 0, 128, 128), image.GetPixel(image.Center, image.Center));
    }

    [Fact]
    public void AnimatedImages_SwitchFramesOverTime()
    {
        var provider = new FrameProvider(RedFrame, BlueFrame);
        var rasterized = DesignRenderer.Rasterize(new CrosshairDesign { Layers = [new ImageLayer { FileName = "a.gif" }] }, 0, provider);
        var c = rasterized.Size / 2;

        Assert.True(rasterized.IsAnimated);
        Assert.Equal((0, 0, 255, 255), rasterized.Compose(null).GetPixel(c, c));
        Assert.Equal((255, 0, 0, 255), rasterized.Compose(TimeSpan.FromSeconds(1)).GetPixel(c, c));
    }

    [Fact]
    public void WithoutContent_ImageLayersAreSkipped()
    {
        Assert.Equal(1, DesignRenderer.Render(new CrosshairDesign { Layers = [new ImageLayer { FileName = "a.png" }] }).Size);
    }

    [Fact]
    public void ShareCode_OmitsImageLayers()
    {
        var design = new CrosshairDesign
        {
            Layers = [new ClassicLayer { Settings = new CrosshairSettings { Gap = 8 } }, new ImageLayer { FileName = "logo.png" }],
        };

        Assert.True(DesignShareCode.OmitsLayers(design));
        Assert.True(DesignShareCode.TryDecode(DesignShareCode.Encode(design), out var decoded, out _));
        Assert.Equal(CrosshairDesign.FromClassic(new CrosshairSettings { Gap = 8 }).Normalize(), decoded);
        Assert.False(DesignShareCode.OmitsLayers(decoded));
    }

    [Theory]
    [InlineData("../evil.png")]
    [InlineData(@"C:\Windows\evil.png")]
    [InlineData("sub/evil.png")]
    [InlineData("")]
    public void Normalize_RejectsUnsafeFileNames(string fileName)
    {
        Assert.Equal(string.Empty, ((ImageLayer)new ImageLayer { FileName = fileName }.Normalize()).FileName);
    }

    [Fact]
    public void Normalize_KeepsPlainFileNamesAndClampsValues()
    {
        var layer = (ImageLayer)new ImageLayer { FileName = "0123abcd.gif", Width = 9999, Opacity = 0 }.Normalize();

        Assert.Equal("0123abcd.gif", layer.FileName);
        Assert.Equal(ImageLayer.MaxWidth, layer.Width);
        Assert.Equal(CrosshairSettings.MinOpacity, layer.Opacity);
    }

    [Fact]
    public void ImageFrame_RejectsMismatchedSize()
    {
        Assert.Throws<ArgumentException>(() => new ImageFrame(2, 2, new byte[3]));
    }

    private static ImageFrame Solid(int width, int height, byte b, byte g, byte r, byte a)
    {
        var pixels = new byte[width * height * 4];
        for (var i = 0; i < width * height; i++)
        {
            pixels[i * 4] = b;
            pixels[i * 4 + 1] = g;
            pixels[i * 4 + 2] = r;
            pixels[i * 4 + 3] = a;
        }

        return new ImageFrame(width, height, pixels);
    }

    private sealed class FrameProvider(params ImageFrame[] frames) : ILayerContentProvider
    {
        public CoverageMask? RenderText(TextLayer layer, double scale) => null;

        public ImageFrame? RenderImage(ImageLayer layer, double scale, TimeSpan? time) =>
            time is { } elapsed && frames.Length > 1 ? frames[(int)elapsed.TotalSeconds % frames.Length] : frames[0];

        public bool IsAnimated(ImageLayer layer) => frames.Length > 1;
    }
}
