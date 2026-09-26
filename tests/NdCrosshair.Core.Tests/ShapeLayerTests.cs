namespace NdCrosshair.Core.Tests;

public class ShapeLayerTests
{
    private static readonly LayerStyle Plain = new() { ShowOutline = false };

    [Fact]
    public void FilledRectangle_IsPixelSharpAndExact()
    {
        var image = Render(new ShapeLayer { Kind = ShapeKind.Rectangle, Width = 10, Height = 6, Style = Plain });

        Assert.Equal(60, OpaqueCount(image));
        Assert.All(Alphas(image), alpha => Assert.True(alpha is 0 or 255));
    }

    [Fact]
    public void RectangleFrame_LeavesTheInsideEmpty()
    {
        var image = Render(new ShapeLayer { Kind = ShapeKind.Rectangle, Width = 10, Height = 10, Thickness = 2, Filled = false, Style = Plain });

        Assert.Equal(100 - 36, OpaqueCount(image));
        Assert.Equal(0, image.GetPixel(image.Center, image.Center).A);
    }

    [Fact]
    public void Triangle_PointsUpAndIsMirrorSymmetric()
    {
        var image = Render(new ShapeLayer { Kind = ShapeKind.Triangle, Width = 15, Height = 15, Style = Plain });
        var top = RowCoverage(image, image.Center - 6);
        var bottom = RowCoverage(image, image.Center + 6);

        Assert.True(top < bottom);
        AssertMirrorSymmetric(image);
    }

    [Fact]
    public void Chevron_IsHollowBelowTheApex()
    {
        var image = Render(new ShapeLayer { Kind = ShapeKind.Chevron, Width = 21, Height = 21, Thickness = 3, Style = Plain });
        var c = image.Center;

        Assert.True(image.GetPixel(c, c - 9).A > 0);
        Assert.Equal(0, image.GetPixel(c, c + 6).A);
        Assert.True(image.GetPixel(c - 9, c + 9).A > 0 && image.GetPixel(c + 9, c + 9).A > 0);
        AssertMirrorSymmetric(image);
    }

    [Fact]
    public void Arc_CoversOnlyTheTopForAQuarterSweep()
    {
        var image = Render(new ShapeLayer { Kind = ShapeKind.Arc, Width = 21, Thickness = 2, Sweep = 90, Style = Plain });
        var c = image.Center;

        Assert.True(image.GetPixel(c, c - 9).A > 0);
        Assert.Equal(0, image.GetPixel(c, c + 9).A);
        Assert.Equal(0, image.GetPixel(c - 9, c).A);
        Assert.Equal(0, image.GetPixel(c, c).A);
    }

    [Fact]
    public void FullArc_IsARing()
    {
        var image = Render(new ShapeLayer { Kind = ShapeKind.Arc, Width = 21, Thickness = 2, Sweep = 360, Style = Plain });
        var c = image.Center;

        Assert.True(image.GetPixel(c, c + 9).A > 0 && image.GetPixel(c - 9, c).A > 0);
        Assert.Equal(0, image.GetPixel(c, c).A);
    }

    [Fact]
    public void TShape_HasBarOnTopAndRotatesAroundItsCenter()
    {
        var upright = Render(new ShapeLayer { Kind = ShapeKind.TShape, Width = 11, Height = 11, Thickness = 3, Style = Plain });
        var turned = Render(new ShapeLayer { Kind = ShapeKind.TShape, Width = 11, Height = 11, Thickness = 3, Rotation = 90, Style = Plain });
        var c = upright.Center;

        Assert.True(upright.GetPixel(c - 5, c - 5).A > 0);
        Assert.Equal(0, upright.GetPixel(c - 5, c + 5).A);
        Assert.True(turned.GetPixel(c + 5, c - 5).A > 0);
        Assert.Equal(0, turned.GetPixel(c - 5, c - 5).A);
    }

    [Fact]
    public void Outline_SurroundsTheShape()
    {
        var shape = new ShapeLayer { Kind = ShapeKind.Rectangle, Width = 6, Height = 6, Style = new LayerStyle { OutlineThickness = 2 } };
        var image = Render(shape);
        var c = image.Center;

        Assert.Equal((0, 0, 0, 255), image.GetPixel(c - 4, c));
        Assert.Equal((0, 255, 0, 255), image.GetPixel(c, c));
    }

    [Fact]
    public void ShapeStyle_SupportsRainbow()
    {
        var rasterized = DesignRenderer.Rasterize(new CrosshairDesign
        {
            Layers = [new ShapeLayer { Style = new LayerStyle { ColorMode = CrosshairColorMode.Rainbow } }],
        });

        Assert.True(rasterized.IsAnimated);
    }

    [Fact]
    public void Normalize_ClampsShapeValues()
    {
        var shape = (ShapeLayer)new ShapeLayer { Width = 0, Height = 999, Thickness = 0, Sweep = 1, Rotation = 400, Kind = (ShapeKind)42 }.Normalize();

        Assert.Equal((ShapeLayer.MinSize, ShapeLayer.MaxSize, ShapeLayer.MinThickness, ShapeLayer.MinSweep), (shape.Width, shape.Height, shape.Thickness, shape.Sweep));
        Assert.Equal(CrosshairSettings.MaxRotation, shape.Rotation);
        Assert.Equal(ShapeKind.Triangle, shape.Kind);
    }

    [Fact]
    public void ShapeLayers_RoundTripThroughShareCode()
    {
        var design = new CrosshairDesign
        {
            Layers =
            [
                new ClassicLayer(),
                new ShapeLayer { Kind = ShapeKind.Chevron, Width = 14, Rotation = 180, OffsetY = 20, Style = new LayerStyle { Color = new CrosshairColor(255, 0, 0) } },
            ],
        };

        Assert.True(DesignShareCode.TryDecode(DesignShareCode.Encode(design), out var decoded, out _));
        Assert.Equal(design.Normalize(), decoded);
    }

    [Fact]
    public void LayerStyle_MapsClassicSettingsBothWays()
    {
        var settings = new CrosshairSettings { Color = new CrosshairColor(1, 2, 3), Opacity = 50, ShowShadow = true, ShadowSize = 7, LineLength = 9 };

        var style = LayerStyle.From(settings);

        Assert.Equal(settings, style.ApplyTo(settings with { Color = CrosshairColor.Black, Opacity = 100 }));
        Assert.Equal(9, style.ApplyTo(settings).LineLength);
    }

    private static CrosshairImage Render(ShapeLayer layer) => DesignRenderer.Render(new CrosshairDesign { Layers = [layer] });

    private static IEnumerable<byte> Alphas(CrosshairImage image) =>
        Enumerable.Range(0, image.Size * image.Size).Select(i => image.Pixels[i * 4 + 3]);

    private static int OpaqueCount(CrosshairImage image) => Alphas(image).Count(alpha => alpha == 255);

    private static int RowCoverage(CrosshairImage image, int y) =>
        Enumerable.Range(0, image.Size).Sum(x => image.GetPixel(x, y).A);

    private static void AssertMirrorSymmetric(CrosshairImage image)
    {
        var last = image.Size - 1;
        for (var y = 0; y < image.Size; y++)
        {
            for (var x = 0; x < image.Size; x++)
            {
                Assert.Equal(image.GetPixel(x, y).A, image.GetPixel(last - x, y).A);
            }
        }
    }
}
