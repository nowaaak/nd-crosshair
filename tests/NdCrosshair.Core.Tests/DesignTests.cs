namespace NdCrosshair.Core.Tests;

public class DesignTests
{
    public static TheoryData<CrosshairSettings> ClassicSamples => new()
    {
        new CrosshairSettings(),
        new CrosshairSettings { LineThickness = 3, Gap = 5, ShowDot = true, DotSize = 3, ShowRing = true, RingRadius = 14 },
        new CrosshairSettings { Rotation = 45, ShowShadow = true, ShadowSize = 4, Opacity = 70, Color = new CrosshairColor(200, 40, 90) },
        new CrosshairSettings { ShowTop = false, ShowOutline = false, LineLength = 12, RoundDot = true, ShowDot = true },
    };

    [Theory]
    [MemberData(nameof(ClassicSamples))]
    public void ClassicDesign_RendersExactlyLikeClassicRenderer(CrosshairSettings settings)
    {
        var expected = CrosshairRenderer.Render(settings);
        var actual = DesignRenderer.Render(CrosshairDesign.FromClassic(settings));

        Assert.Equal(expected.Size, actual.Size);
        Assert.Equal(expected.Pixels, actual.Pixels);
    }

    [Fact]
    public void Offset_MovesLayerAndGrowsCanvas()
    {
        var settings = new CrosshairSettings { ShowDot = true, DotSize = 1, ShowOutline = false, LineLength = 0 };
        var image = DesignRenderer.Render(new CrosshairDesign { Layers = [new ClassicLayer { Settings = settings, OffsetX = 5, OffsetY = -3 }] });

        Assert.Equal(11, image.Size);
        Assert.Equal(255, image.GetPixel(image.Center + 5, image.Center - 3).A);
        Assert.Equal(0, image.GetPixel(image.Center, image.Center).A);
    }

    [Fact]
    public void LaterLayers_AreDrawnOnTop()
    {
        var red = new CrosshairColor(255, 0, 0);
        var blue = new CrosshairColor(0, 0, 255);
        var dot = new CrosshairSettings { ShowDot = true, DotSize = 3, LineLength = 0, ShowOutline = false };
        var design = new CrosshairDesign
        {
            Layers =
            [
                new ClassicLayer { Settings = dot with { Color = red } },
                new ClassicLayer { Settings = dot with { Color = blue } },
            ],
        };

        var image = DesignRenderer.Render(design);

        Assert.Equal((255, 0, 0, 255), image.GetPixel(image.Center, image.Center));
    }

    [Fact]
    public void HiddenLayers_AreIgnored()
    {
        var design = new CrosshairDesign
        {
            Layers =
            [
                new ClassicLayer(),
                new ClassicLayer { Visible = false, OffsetX = 150, Settings = new CrosshairSettings { ShowDot = true } },
            ],
        };

        Assert.Equal(DesignRenderer.Render(CrosshairDesign.FromClassic(new CrosshairSettings())).Pixels, DesignRenderer.Render(design).Pixels);
    }

    [Fact]
    public void Scale_EnlargesGeometry()
    {
        var settings = new CrosshairSettings { LineLength = 5, Gap = 2, ShowOutline = false };
        var normal = DesignRenderer.Render(CrosshairDesign.FromClassic(settings));
        var doubled = DesignRenderer.Render(new CrosshairDesign { Layers = [new ClassicLayer { Settings = settings, Scale = 200 }] });

        Assert.Equal(normal.Size * 2 - 1, doubled.Size);
    }

    [Fact]
    public void Blur_SoftensEdgesWithoutLosingCoverage()
    {
        var settings = new CrosshairSettings { ShowOutline = false, LineLength = 6 };
        var sharp = DesignRenderer.Render(CrosshairDesign.FromClassic(settings));
        var blurred = DesignRenderer.Render(new CrosshairDesign { Layers = [new ClassicLayer { Settings = settings, Blur = 3 }] });

        Assert.Equal(sharp.Size + 6, blurred.Size);
        Assert.Equal(TotalAlpha(sharp), TotalAlpha(blurred), 0.02 * TotalAlpha(sharp));
        Assert.Equal(255, sharp.GetPixel(0, sharp.Center).A);
        Assert.InRange(blurred.GetPixel(3, blurred.Center).A, 1, 254);
        Assert.InRange(blurred.GetPixel(1, blurred.Center).A, 1, 254);
    }

    [Fact]
    public void Rainbow_IsAnimatedAndUsesBaseColorWithoutTime()
    {
        var settings = new CrosshairSettings { ColorMode = CrosshairColorMode.Rainbow, Color = new CrosshairColor(255, 0, 0), ShowOutline = false };
        var rasterized = DesignRenderer.Rasterize(CrosshairDesign.FromClassic(settings));
        var c = rasterized.Size / 2;
        var arm = c + settings.Gap + 2;

        Assert.True(rasterized.IsAnimated);
        Assert.Equal((0, 0, 255, 255), rasterized.Compose(null).GetPixel(arm, c));
        Assert.NotEqual(rasterized.Compose(TimeSpan.FromSeconds(1)).GetPixel(arm, c), rasterized.Compose(null).GetPixel(arm, c));
        Assert.False(DesignRenderer.Rasterize(new CrosshairDesign()).IsAnimated);
    }

    [Fact]
    public void Normalize_ClampsLayerTransformAndLimitsLayerCount()
    {
        var layers = Enumerable.Range(0, CrosshairDesign.MaxLayers + 5)
            .Select(_ => (CrosshairLayer)new ClassicLayer { OffsetX = 9999, Scale = 1, Blur = 99 })
            .ToList();

        var design = new CrosshairDesign { Layers = layers }.Normalize();

        Assert.Equal(CrosshairDesign.MaxLayers, design.Layers.Count);
        Assert.All(design.Layers, layer =>
        {
            Assert.Equal(CrosshairLayer.MaxOffset, layer.OffsetX);
            Assert.Equal(CrosshairLayer.MinScale, layer.Scale);
            Assert.Equal(CrosshairLayer.MaxBlur, layer.Blur);
        });
        Assert.Single(new CrosshairDesign { Layers = [] }.Normalize().Layers);
    }

    [Fact]
    public void Designs_CompareByLayerContent()
    {
        var first = new CrosshairDesign { Layers = [new ClassicLayer { OffsetX = 3 }] };
        var second = new CrosshairDesign { Layers = [new ClassicLayer { OffsetX = 3 }] };

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotEqual(first, second with { Layers = [new ClassicLayer { OffsetX = 4 }] });
    }

    private static double TotalAlpha(CrosshairImage image) =>
        Enumerable.Range(0, image.Size * image.Size).Sum(i => image.Pixels[i * 4 + 3]);
}
