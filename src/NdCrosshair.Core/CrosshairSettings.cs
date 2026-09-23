namespace NdCrosshair.Core;

public sealed record CrosshairSettings
{
    public const int MinLineLength = 0;
    public const int MaxLineLength = 100;
    public const int MinLineThickness = 1;
    public const int MaxLineThickness = 20;
    public const int MinGap = 0;
    public const int MaxGap = 50;
    public const int MinRotation = 0;
    public const int MaxRotation = 90;
    public const int MinDotSize = 1;
    public const int MaxDotSize = 20;
    public const int MinRingRadius = 2;
    public const int MaxRingRadius = 60;
    public const int MinRingThickness = 1;
    public const int MaxRingThickness = 10;
    public const int MinOutlineThickness = 1;
    public const int MaxOutlineThickness = 5;
    public const int MinShadowSize = 1;
    public const int MaxShadowSize = 10;
    public const int MinShadowOpacity = 5;
    public const int MaxShadowOpacity = 100;
    public const int MinRainbowSpeed = 1;
    public const int MaxRainbowSpeed = 10;
    public const int MinOpacity = 5;
    public const int MaxOpacity = 100;

    public bool ShowTop { get; init; } = true;

    public bool ShowBottom { get; init; } = true;

    public bool ShowLeft { get; init; } = true;

    public bool ShowRight { get; init; } = true;

    public int LineLength { get; init; } = 6;

    public int LineThickness { get; init; } = 2;

    public int Gap { get; init; } = 4;

    public int Rotation { get; init; }

    public bool ShowDot { get; init; }

    public bool RoundDot { get; init; }

    public int DotSize { get; init; } = 2;

    public bool ShowRing { get; init; }

    public int RingRadius { get; init; } = 12;

    public int RingThickness { get; init; } = 1;

    public bool ShowOutline { get; init; } = true;

    public int OutlineThickness { get; init; } = 1;

    public bool ShowShadow { get; init; }

    public int ShadowSize { get; init; } = 3;

    public int ShadowOpacity { get; init; } = 60;

    public CrosshairColor Color { get; init; } = CrosshairColor.Green;

    public CrosshairColor OutlineColor { get; init; } = CrosshairColor.Black;

    public CrosshairColor ShadowColor { get; init; } = CrosshairColor.Black;

    public CrosshairColorMode ColorMode { get; init; }

    public int RainbowSpeed { get; init; } = 5;

    public int Opacity { get; init; } = 100;

    public CrosshairSettings Clamp() => this with
    {
        LineLength = Math.Clamp(LineLength, MinLineLength, MaxLineLength),
        LineThickness = Math.Clamp(LineThickness, MinLineThickness, MaxLineThickness),
        Gap = Math.Clamp(Gap, MinGap, MaxGap),
        Rotation = Math.Clamp(Rotation, MinRotation, MaxRotation),
        DotSize = Math.Clamp(DotSize, MinDotSize, MaxDotSize),
        RingRadius = Math.Clamp(RingRadius, MinRingRadius, MaxRingRadius),
        RingThickness = Math.Clamp(RingThickness, MinRingThickness, MaxRingThickness),
        OutlineThickness = Math.Clamp(OutlineThickness, MinOutlineThickness, MaxOutlineThickness),
        ShadowSize = Math.Clamp(ShadowSize, MinShadowSize, MaxShadowSize),
        ShadowOpacity = Math.Clamp(ShadowOpacity, MinShadowOpacity, MaxShadowOpacity),
        ColorMode = Enum.IsDefined(ColorMode) ? ColorMode : CrosshairColorMode.Static,
        RainbowSpeed = Math.Clamp(RainbowSpeed, MinRainbowSpeed, MaxRainbowSpeed),
        Opacity = Math.Clamp(Opacity, MinOpacity, MaxOpacity),
    };
}
