namespace NdCrosshair.Core;

public sealed class AdaptiveColorSelector
{
    public const double ContrastThreshold = 40;
    public const double Hysteresis = 8;

    public static readonly IReadOnlyList<CrosshairColor> Candidates =
    [
        new(255, 255, 255),
        new(0, 0, 0),
        new(0, 255, 0),
        new(0, 255, 255),
        new(255, 255, 0),
        new(255, 0, 255),
        new(255, 0, 0),
    ];

    private readonly CrosshairColor preferred;

    public AdaptiveColorSelector(CrosshairColor preferred)
    {
        this.preferred = preferred;
        Current = preferred;
    }

    public CrosshairColor Current { get; private set; }

    public static CrosshairColor Choose(CrosshairColor preferred, CrosshairColor background) =>
        ColorMath.DeltaE(preferred, background) >= ContrastThreshold ? preferred : BestContrast(preferred, background);

    public CrosshairColor Update(CrosshairColor background)
    {
        var preferredContrast = ColorMath.DeltaE(preferred, background);

        if (Current == preferred)
        {
            if (preferredContrast >= ContrastThreshold - Hysteresis)
            {
                return Current;
            }
        }
        else
        {
            if (preferredContrast >= ContrastThreshold + Hysteresis)
            {
                return Current = preferred;
            }

            if (ColorMath.DeltaE(Current, background) >= ContrastThreshold)
            {
                return Current;
            }
        }

        return Current = BestContrast(preferred, background);
    }

    private static CrosshairColor BestContrast(CrosshairColor preferred, CrosshairColor background)
    {
        var best = preferred;
        var bestContrast = ColorMath.DeltaE(preferred, background);
        foreach (var candidate in Candidates)
        {
            var contrast = ColorMath.DeltaE(candidate, background);
            if (contrast > bestContrast)
            {
                best = candidate;
                bestContrast = contrast;
            }
        }

        return best;
    }
}
