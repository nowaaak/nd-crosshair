namespace NdCrosshair.Core.Tests;

public class FireSpreadTests
{
    private const double Frame = 1 / 100.0;

    [Fact]
    public void Firing_ReachesFullSpreadQuickly()
    {
        var animator = new FireSpreadAnimator();

        var spread = Run(animator, firing: true, seconds: 0.2, maxSpread: 12, recovery: 250);

        Assert.Equal(12, spread);
        Assert.False(animator.IsResting);
    }

    [Fact]
    public void Releasing_ReturnsWithinRecoveryTimeAndEasesOut()
    {
        var animator = new FireSpreadAnimator();
        Run(animator, firing: true, seconds: 0.2, maxSpread: 12, recovery: 300);

        var values = new List<int>();
        for (var t = 0.0; t < 0.45; t += Frame)
        {
            values.Add(animator.Update(false, Frame, 12, 300));
        }

        Assert.Equal(0, values[^1]);
        Assert.True(animator.IsResting);
        Assert.Equal(values.OrderByDescending(value => value), values);
        Assert.True(values[5] < 12 && values[5] > 6);
    }

    [Fact]
    public void LongerRecovery_ReturnsMoreSlowly()
    {
        var fast = new FireSpreadAnimator();
        var slow = new FireSpreadAnimator();
        Run(fast, true, 0.2, 20, 100);
        Run(slow, true, 0.2, 20, 900);

        Assert.True(Run(fast, false, 0.1, 20, 100) < Run(slow, false, 0.1, 20, 900));
    }

    [Fact]
    public void Design_ClampsSpreadSettingsAndComparesThem()
    {
        var design = new CrosshairDesign { FireSpread = 99, FireRecovery = 1 }.Normalize();

        Assert.Equal(CrosshairDesign.MaxFireSpread, design.FireSpread);
        Assert.Equal(CrosshairDesign.MinFireRecovery, design.FireRecovery);
        Assert.NotEqual(new CrosshairDesign(), new CrosshairDesign { FireSpread = 4 });
    }

    [Fact]
    public void Spread_WidensClassicLayers()
    {
        var design = CrosshairDesign.FromClassic(new CrosshairSettings { ShowOutline = false });

        var resting = DesignRenderer.Rasterize(design);
        var spread = DesignRenderer.Rasterize(design, spread: 5);

        Assert.Equal(resting.Size + 10, spread.Size);
    }

    [Fact]
    public void SpreadSettings_ForceVersion4ShareCodeAndRoundTrip()
    {
        var design = CrosshairDesign.FromClassic(new CrosshairSettings()) with { FireSpread = 8, FireRecovery = 400 };

        var code = DesignShareCode.Encode(design);

        Assert.StartsWith(DesignShareCode.Prefix, code);
        Assert.True(DesignShareCode.TryDecode(code, out var decoded, out _));
        Assert.Equal(design, decoded);
    }

    private static int Run(FireSpreadAnimator animator, bool firing, double seconds, int maxSpread, int recovery)
    {
        var value = 0;
        for (var t = 0.0; t < seconds; t += Frame)
        {
            value = animator.Update(firing, Frame, maxSpread, recovery);
        }

        return value;
    }
}
