namespace NdCrosshair.Core.Tests;

public class ArmCountTests
{
    private static readonly CrosshairSettings Base = new() { LineLength = 10, LineThickness = 3, Gap = 4, ShowOutline = false };

    [Fact]
    public void TwoArms_DrawOnlyTheVerticalLine()
    {
        var image = CrosshairRenderer.Render(Base with { ArmCount = 2 });
        var c = image.Center;

        Assert.True(image.GetPixel(c, c - 8).A > 0);
        Assert.True(image.GetPixel(c, c + 8).A > 0);
        Assert.Equal(0, image.GetPixel(c - 8, c).A);
        Assert.Equal(0, image.GetPixel(c + 8, c).A);
    }

    [Fact]
    public void EightArms_AddDiagonals()
    {
        var image = CrosshairRenderer.Render(Base with { ArmCount = 8 });
        var c = image.Center;
        var diagonal = (int)Math.Round(9 / Math.Sqrt(2));

        Assert.True(image.GetPixel(c + diagonal, c - diagonal).A > 0);
        Assert.True(image.GetPixel(c - diagonal, c + diagonal).A > 0);
        Assert.True(image.GetPixel(c, c - 9).A > 0);
        Assert.True(image.GetPixel(c + 9, c).A > 0);
    }

    [Fact]
    public void ThreeArms_AreSpreadEvenly()
    {
        var image = CrosshairRenderer.Render(Base with { ArmCount = 3 });
        var c = image.Center;

        Assert.True(image.GetPixel(c, c - 9).A > 0);
        Assert.Equal(0, image.GetPixel(c, c + 9).A);
        Assert.True(Alpha(image, c + 9 * Math.Sin(Math.PI * 2 / 3), c - 9 * Math.Cos(Math.PI * 2 / 3)) > 0);
        Assert.True(Alpha(image, c - 9 * Math.Sin(Math.PI * 2 / 3), c - 9 * Math.Cos(Math.PI * 2 / 3)) > 0);
    }

    [Fact]
    public void FourArms_MatchLegacyToggles()
    {
        var legacy = CrosshairRenderer.Render(Base with { ShowLeft = false });
        var explicitFour = CrosshairRenderer.Render(Base with { ShowLeft = false, ArmCount = 4 });

        Assert.Equal(legacy.Pixels, explicitFour.Pixels);
    }

    [Fact]
    public void OtherArmCounts_IgnoreSingleLineToggles()
    {
        var all = CrosshairRenderer.Render(Base with { ArmCount = 6 });
        var toggled = CrosshairRenderer.Render(Base with { ArmCount = 6, ShowTop = false, ShowLeft = false });

        Assert.Equal(all.Pixels, toggled.Pixels);
    }

    [Theory]
    [InlineData(0, CrosshairSettings.MinArmCount)]
    [InlineData(99, CrosshairSettings.MaxArmCount)]
    public void Clamp_KeepsArmCountInRange(int armCount, int expected)
    {
        Assert.Equal(expected, (Base with { ArmCount = armCount }).Clamp().ArmCount);
    }

    [Fact]
    public void ArmCountAndWideRotation_SurviveShareCode()
    {
        var design = CrosshairDesign.FromClassic(Base with { ArmCount = 6, Rotation = 200 });

        var code = DesignShareCode.Encode(design);

        Assert.StartsWith(DesignShareCode.Prefix, code);
        Assert.True(DesignShareCode.TryDecode(code, out var decoded, out _));
        Assert.Equal(design.Normalize(), decoded);
    }

    private static int Alpha(CrosshairImage image, double x, double y) => image.GetPixel((int)Math.Round(x), (int)Math.Round(y)).A;
}
