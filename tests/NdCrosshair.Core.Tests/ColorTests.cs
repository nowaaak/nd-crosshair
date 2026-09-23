using NdCrosshair.Core;

namespace NdCrosshair.Core.Tests;

public class ColorTests
{
    private static readonly CrosshairColor Green = new(0, 255, 0);
    private static readonly CrosshairColor White = new(255, 255, 255);
    private static readonly CrosshairColor Black = new(0, 0, 0);

    [Theory]
    [InlineData(0, 255, 0, 0)]
    [InlineData(120, 0, 255, 0)]
    [InlineData(240, 0, 0, 255)]
    [InlineData(60, 255, 255, 0)]
    [InlineData(300, 255, 0, 255)]
    [InlineData(360, 255, 0, 0)]
    [InlineData(-60, 255, 0, 255)]
    public void FromHsv_ProducesPrimaryColors(double hue, byte r, byte g, byte b)
    {
        Assert.Equal(new CrosshairColor(r, g, b), ColorMath.FromHsv(hue, 1, 1));
    }

    [Theory]
    [InlineData(12, 200, 99)]
    [InlineData(255, 128, 0)]
    [InlineData(0, 0, 0)]
    [InlineData(255, 255, 255)]
    [InlineData(17, 17, 200)]
    public void HsvRoundTrip_PreservesColor(byte r, byte g, byte b)
    {
        var color = new CrosshairColor(r, g, b);
        var (hue, saturation, value) = ColorMath.ToHsv(color);

        Assert.Equal(color, ColorMath.FromHsv(hue, saturation, value));
    }

    [Fact]
    public void DeltaE_MatchesKnownReferenceValues()
    {
        Assert.Equal(0, ColorMath.DeltaE(Green, Green), 6);
        Assert.Equal(100, ColorMath.DeltaE(White, Black), 0);
        Assert.InRange(ColorMath.DeltaE(Green, new CrosshairColor(10, 250, 10)), 0, 5);
    }

    [Fact]
    public void Choose_KeepsPreferredColorWhenContrastIsHigh()
    {
        Assert.Equal(Green, AdaptiveColorSelector.Choose(Green, new CrosshairColor(20, 20, 30)));
    }

    [Fact]
    public void Choose_SwitchesAwayFromPreferredOnSimilarBackground()
    {
        var foliage = new CrosshairColor(40, 200, 40);

        var chosen = AdaptiveColorSelector.Choose(Green, foliage);

        Assert.NotEqual(Green, chosen);
        Assert.True(ColorMath.DeltaE(chosen, foliage) >= AdaptiveColorSelector.ContrastThreshold);
    }

    [Fact]
    public void Update_UsesHysteresisToAvoidFlicker()
    {
        var selector = new AdaptiveColorSelector(Green);
        var similar = new CrosshairColor(30, 230, 30);
        var dark = new CrosshairColor(10, 10, 10);

        var alternative = selector.Update(similar);
        Assert.NotEqual(Green, alternative);

        Assert.Equal(alternative, selector.Update(similar));
        Assert.Equal(Green, selector.Update(dark));
        Assert.Equal(Green, selector.Update(dark));
    }

    [Fact]
    public void Update_KeepsPreferredInsideHysteresisBand()
    {
        var selector = new AdaptiveColorSelector(Green);
        var background = FindBackgroundWithContrast(Green, AdaptiveColorSelector.ContrastThreshold - AdaptiveColorSelector.Hysteresis / 2);

        Assert.Equal(Green, selector.Update(background));
    }

    [Fact]
    public void Compose_WithBaseColor_MatchesRender()
    {
        var settings = new CrosshairSettings { ShowShadow = true, ShowRing = true, Rotation = 30, Opacity = 70 };

        var composed = CrosshairRenderer.Rasterize(settings).Compose(settings.Color);

        Assert.Equal(CrosshairRenderer.Render(settings).Pixels, composed.Pixels);
    }

    [Fact]
    public void Compose_WithOtherColor_OnlyChangesFillPixels()
    {
        var settings = new CrosshairSettings { LineThickness = 1, ShowOutline = true };
        var layers = CrosshairRenderer.Rasterize(settings);

        var green = layers.Compose(Green);
        var red = layers.Compose(new CrosshairColor(255, 0, 0));
        var c = green.Center;

        Assert.Equal(green.GetPixel(c + settings.Gap + 1, c).A, red.GetPixel(c + settings.Gap + 1, c).A);
        Assert.Equal((0, 0, 255, 255), red.GetPixel(c + settings.Gap + 1, c));
        Assert.Equal(green.GetPixel(c + settings.Gap + 1, c + 1), red.GetPixel(c + settings.Gap + 1, c + 1));
    }

    private static CrosshairColor FindBackgroundWithContrast(CrosshairColor color, double target)
    {
        var best = color;
        var bestDistance = double.MaxValue;
        for (var value = 0; value <= 255; value++)
        {
            var candidate = new CrosshairColor((byte)value, 255, (byte)value);
            var distance = Math.Abs(ColorMath.DeltaE(color, candidate) - target);
            if (distance < bestDistance)
            {
                best = candidate;
                bestDistance = distance;
            }
        }

        return best;
    }
}
