namespace NdCrosshair.Core;

public sealed record LayerStyle
{
    public CrosshairColor Color { get; init; } = CrosshairColor.Green;

    public CrosshairColorMode ColorMode { get; init; }

    public int RainbowSpeed { get; init; } = 5;

    public int Opacity { get; init; } = 100;

    public bool ShowOutline { get; init; } = true;

    public int OutlineThickness { get; init; } = 1;

    public CrosshairColor OutlineColor { get; init; } = CrosshairColor.Black;

    public bool ShowShadow { get; init; }

    public int ShadowSize { get; init; } = 3;

    public int ShadowOpacity { get; init; } = 60;

    public CrosshairColor ShadowColor { get; init; } = CrosshairColor.Black;

    public static LayerStyle From(CrosshairSettings settings) => new()
    {
        Color = settings.Color,
        ColorMode = settings.ColorMode,
        RainbowSpeed = settings.RainbowSpeed,
        Opacity = settings.Opacity,
        ShowOutline = settings.ShowOutline,
        OutlineThickness = settings.OutlineThickness,
        OutlineColor = settings.OutlineColor,
        ShowShadow = settings.ShowShadow,
        ShadowSize = settings.ShadowSize,
        ShadowOpacity = settings.ShadowOpacity,
        ShadowColor = settings.ShadowColor,
    };

    public CrosshairSettings ApplyTo(CrosshairSettings settings) => settings with
    {
        Color = Color,
        ColorMode = ColorMode,
        RainbowSpeed = RainbowSpeed,
        Opacity = Opacity,
        ShowOutline = ShowOutline,
        OutlineThickness = OutlineThickness,
        OutlineColor = OutlineColor,
        ShowShadow = ShowShadow,
        ShadowSize = ShadowSize,
        ShadowOpacity = ShadowOpacity,
        ShadowColor = ShadowColor,
    };

    public LayerStyle Clamp() => From(ApplyTo(new CrosshairSettings()).Clamp());
}
