namespace NdCrosshair.Core.Tests;

public class DotParityTests
{
    [Theory]
    [InlineData(2, 2, false)]
    [InlineData(3, 1, false)]
    [InlineData(2, 3, true)]
    [InlineData(1, 2, true)]
    public void HasDotParityMismatch_ComparesDotAndLineParity(int thickness, int dotSize, bool expected)
    {
        var settings = new CrosshairSettings { LineThickness = thickness, ShowDot = true, DotSize = dotSize };

        Assert.Equal(expected, settings.HasDotParityMismatch());
    }

    [Fact]
    public void HasDotParityMismatch_IgnoresHiddenDotOrLines()
    {
        var mismatch = new CrosshairSettings { LineThickness = 2, ShowDot = true, DotSize = 3 };

        Assert.False((mismatch with { ShowDot = false }).HasDotParityMismatch());
        Assert.False((mismatch with { LineLength = 0 }).HasDotParityMismatch());
        Assert.False((mismatch with { ShowTop = false, ShowBottom = false, ShowLeft = false, ShowRight = false }).HasDotParityMismatch());
    }

    [Fact]
    public void Render_WithMatchingEvenParity_CentersDotOnLines()
    {
        var image = CrosshairRenderer.Render(new CrosshairSettings
        {
            LineThickness = 2,
            Gap = 2,
            LineLength = 3,
            ShowOutline = false,
            ShowDot = true,
            DotSize = 2,
        });
        var c = image.Center;

        var dotColumns = Enumerable.Range(c - 2, 5).Where(x => image.GetPixel(x, c).A != 0).ToList();

        Assert.Equal([c - 1, c], dotColumns);
    }
}
