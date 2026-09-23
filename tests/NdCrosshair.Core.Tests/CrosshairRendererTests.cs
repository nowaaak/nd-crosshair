using NdCrosshair.Core;

namespace NdCrosshair.Core.Tests;

public class CrosshairRendererTests
{
    private static readonly CrosshairColor Red = new(255, 0, 0);
    private static readonly CrosshairColor Blue = new(0, 0, 255);

    [Fact]
    public void Render_WithNothingEnabled_ReturnsSingleTransparentPixel()
    {
        var image = CrosshairRenderer.Render(new CrosshairSettings
        {
            ShowTop = false,
            ShowBottom = false,
            ShowLeft = false,
            ShowRight = false,
            ShowDot = false,
        });

        Assert.Equal(1, image.Size);
        Assert.Equal(0, image.GetPixel(0, 0).A);
    }

    [Fact]
    public void Render_LeavesGapTransparentAndStartsLineAfterGap()
    {
        var image = CrosshairRenderer.Render(new CrosshairSettings
        {
            LineThickness = 1,
            Gap = 3,
            LineLength = 4,
            ShowOutline = false,
            Color = Red,
        });
        var c = image.Center;

        for (var offset = 0; offset <= 3; offset++)
        {
            Assert.Equal(0, image.GetPixel(c + offset, c).A);
            Assert.Equal(0, image.GetPixel(c - offset, c).A);
            Assert.Equal(0, image.GetPixel(c, c + offset).A);
            Assert.Equal(0, image.GetPixel(c, c - offset).A);
        }

        Assert.Equal((0, 0, 255, 255), image.GetPixel(c + 4, c));
        Assert.Equal((0, 0, 255, 255), image.GetPixel(c + 7, c));
        Assert.Equal(c + 7, image.Size - 1);
    }

    [Theory]
    [InlineData(1, 0, 1)]
    [InlineData(3, 2, 2)]
    [InlineData(5, 4, 3)]
    public void Render_WithOddThickness_IsSymmetricAroundCenter(int thickness, int gap, int outline)
    {
        var image = CrosshairRenderer.Render(new CrosshairSettings
        {
            LineThickness = thickness,
            Gap = gap,
            LineLength = 7,
            ShowOutline = true,
            OutlineThickness = outline,
            ShowDot = true,
            DotSize = thickness,
        });

        var last = image.Size - 1;
        for (var y = 0; y < image.Size; y++)
        {
            for (var x = 0; x < image.Size; x++)
            {
                var pixel = image.GetPixel(x, y);
                Assert.Equal(pixel, image.GetPixel(last - x, y));
                Assert.Equal(pixel, image.GetPixel(x, last - y));
                Assert.Equal(pixel, image.GetPixel(y, x));
            }
        }
    }

    [Fact]
    public void Render_WithEvenThickness_KeepsGapEqualOnBothSides()
    {
        var image = CrosshairRenderer.Render(new CrosshairSettings
        {
            LineThickness = 2,
            Gap = 2,
            LineLength = 3,
            ShowOutline = false,
        });
        var c = image.Center;

        var filledColumns = Enumerable.Range(0, image.Size)
            .Where(x => image.GetPixel(x, c).A != 0)
            .ToList();

        Assert.Equal([c - 6, c - 5, c - 4, c + 3, c + 4, c + 5], filledColumns);
    }

    [Fact]
    public void Render_DrawsOutlineAroundLines()
    {
        var image = CrosshairRenderer.Render(new CrosshairSettings
        {
            LineThickness = 1,
            Gap = 2,
            LineLength = 3,
            ShowOutline = true,
            OutlineThickness = 1,
            Color = Red,
            OutlineColor = Blue,
        });
        var c = image.Center;

        Assert.Equal((0, 0, 255, 255), image.GetPixel(c + 3, c));
        Assert.Equal((255, 0, 0, 255), image.GetPixel(c + 3, c - 1));
        Assert.Equal((255, 0, 0, 255), image.GetPixel(c + 3, c + 1));
        Assert.Equal((255, 0, 0, 255), image.GetPixel(c + 2, c));
        Assert.Equal((255, 0, 0, 255), image.GetPixel(c + 6, c));
        Assert.Equal(0, image.GetPixel(c + 1, c).A);
        Assert.Equal(0, image.GetPixel(c, c).A);
    }

    [Fact]
    public void Render_DrawsCenterDot()
    {
        var image = CrosshairRenderer.Render(new CrosshairSettings
        {
            ShowTop = false,
            ShowBottom = false,
            ShowLeft = false,
            ShowRight = false,
            ShowDot = true,
            DotSize = 1,
            ShowOutline = false,
            Color = Red,
        });

        Assert.Equal(1, image.Size);
        Assert.Equal((0, 0, 255, 255), image.GetPixel(0, 0));
    }

    [Fact]
    public void Render_PremultipliesColorByOpacity()
    {
        var image = CrosshairRenderer.Render(new CrosshairSettings
        {
            ShowTop = false,
            ShowBottom = false,
            ShowLeft = false,
            ShowRight = false,
            ShowDot = true,
            DotSize = 1,
            ShowOutline = false,
            Color = new CrosshairColor(200, 100, 50),
            Opacity = 50,
        });

        Assert.Equal((25, 50, 100, 128), image.GetPixel(0, 0));
    }

    [Fact]
    public void Render_KeepsAxisAlignedEdgesPixelSharp()
    {
        var image = CrosshairRenderer.Render(new CrosshairSettings
        {
            LineThickness = 3,
            Gap = 2,
            LineLength = 5,
            ShowOutline = true,
            ShowDot = true,
        });

        Assert.All(Alphas(image), alpha => Assert.True(alpha is 0 or 255));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    public void Render_RotatedByNinetyDegrees_MatchesUnrotated(int thickness)
    {
        var settings = new CrosshairSettings { LineThickness = thickness, Gap = 3, LineLength = 6, ShowDot = true, DotSize = thickness };

        var straight = CrosshairRenderer.Render(settings);
        var rotated = CrosshairRenderer.Render(settings with { Rotation = 90 });

        Assert.Equal(straight.Size, rotated.Size);
        Assert.Equal(straight.Pixels, rotated.Pixels);
    }

    [Fact]
    public void Render_RotatedByFortyFiveDegrees_IsAntiAliasedAndSymmetric()
    {
        var image = CrosshairRenderer.Render(new CrosshairSettings
        {
            LineThickness = 3,
            Gap = 3,
            LineLength = 8,
            ShowOutline = false,
            Rotation = 45,
        });
        var c = image.Center;
        var last = image.Size - 1;

        Assert.Contains(Alphas(image), alpha => alpha is > 0 and < 255);
        Assert.Equal(0, image.GetPixel(c + 5, c).A);
        Assert.Equal(255, image.GetPixel(c + 6, c + 6).A);
        for (var y = 0; y < image.Size; y++)
        {
            for (var x = 0; x < image.Size; x++)
            {
                Assert.Equal(image.GetPixel(x, y), image.GetPixel(last - x, y));
                Assert.Equal(image.GetPixel(x, y), image.GetPixel(x, last - y));
            }
        }
    }

    [Fact]
    public void Render_DrawsRingAroundCenter()
    {
        var image = CrosshairRenderer.Render(new CrosshairSettings
        {
            ShowTop = false,
            ShowBottom = false,
            ShowLeft = false,
            ShowRight = false,
            ShowOutline = false,
            LineThickness = 1,
            ShowRing = true,
            RingRadius = 10,
            RingThickness = 3,
        });
        var c = image.Center;

        Assert.Equal(0, image.GetPixel(c, c).A);
        Assert.Equal(0, image.GetPixel(c + 5, c).A);
        Assert.Equal(255, image.GetPixel(c + 10, c).A);
        Assert.Equal(255, image.GetPixel(c, c - 10).A);
        Assert.Contains(Alphas(image), alpha => alpha is > 0 and < 255);
        Assert.Equal(image.Size - 1 - c, c);
    }

    [Fact]
    public void Render_RoundDot_SoftensCornersOnly()
    {
        var image = CrosshairRenderer.Render(new CrosshairSettings
        {
            ShowTop = false,
            ShowBottom = false,
            ShowLeft = false,
            ShowRight = false,
            ShowOutline = false,
            ShowDot = true,
            RoundDot = true,
            DotSize = 7,
        });
        var last = image.Size - 1;

        Assert.Equal(7, image.Size);
        Assert.Equal(255, image.GetPixel(image.Center, image.Center).A);
        Assert.True(image.GetPixel(0, 0).A < 128);
        Assert.Equal(image.GetPixel(0, 0), image.GetPixel(last, last));
    }

    [Fact]
    public void Render_Shadow_AddsSoftHaloOutsideCrosshair()
    {
        var settings = new CrosshairSettings
        {
            LineThickness = 1,
            Gap = 3,
            LineLength = 5,
            ShowOutline = false,
            ShadowColor = new CrosshairColor(0, 0, 0),
        };

        var plain = CrosshairRenderer.Render(settings);
        var shadowed = CrosshairRenderer.Render(settings with { ShowShadow = true, ShadowSize = 4, ShadowOpacity = 100 });
        var c = shadowed.Center;

        Assert.Equal(plain.Size + 8, shadowed.Size);
        Assert.Equal((0, 255, 0, 255), shadowed.GetPixel(c + 4, c));
        var halo = shadowed.GetPixel(c + 6, c + 1);
        Assert.Equal((0, 0, 0), (halo.B, halo.G, halo.R));
        Assert.InRange(halo.A, 1, 254);
        Assert.Equal(0, shadowed.GetPixel(0, 0).A);
    }

    [Fact]
    public void Render_ClampsExtremeValues()
    {
        var image = CrosshairRenderer.Render(new CrosshairSettings
        {
            LineLength = int.MaxValue,
            LineThickness = int.MaxValue,
            Gap = int.MaxValue,
            DotSize = int.MinValue,
            OutlineThickness = int.MaxValue,
            Opacity = -10,
        });

        var maxRadius = CrosshairSettings.MaxLineThickness
            + CrosshairSettings.MaxGap
            + CrosshairSettings.MaxLineLength
            + CrosshairSettings.MaxOutlineThickness;
        Assert.True(image.Size <= maxRadius * 2 + 1);
        Assert.Equal(image.Size * image.Size * 4, image.Pixels.Length);
    }

    [Fact]
    public void Render_WithEverythingAtMaximum_StaysWithinBoundsAndIsFast()
    {
        var settings = new CrosshairSettings
        {
            LineLength = CrosshairSettings.MaxLineLength,
            LineThickness = CrosshairSettings.MaxLineThickness,
            Gap = CrosshairSettings.MaxGap,
            Rotation = 45,
            ShowDot = true,
            RoundDot = true,
            DotSize = CrosshairSettings.MaxDotSize,
            ShowRing = true,
            RingRadius = CrosshairSettings.MaxRingRadius,
            RingThickness = CrosshairSettings.MaxRingThickness,
            OutlineThickness = CrosshairSettings.MaxOutlineThickness,
            ShowShadow = true,
            ShadowSize = CrosshairSettings.MaxShadowSize,
        };

        CrosshairRenderer.Render(settings);
        var watch = System.Diagnostics.Stopwatch.StartNew();
        var image = CrosshairRenderer.Render(settings);
        watch.Stop();

        Assert.True(image.Size < 400, $"Size was {image.Size}.");
        Assert.True(watch.ElapsedMilliseconds < 100, $"Rendering took {watch.ElapsedMilliseconds} ms.");
    }

    private static IEnumerable<byte> Alphas(CrosshairImage image) =>
        Enumerable.Range(0, image.Size * image.Size).Select(i => image.Pixels[i * 4 + 3]);
}
