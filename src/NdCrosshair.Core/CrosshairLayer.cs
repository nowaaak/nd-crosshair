using System.Text.Json.Serialization;

namespace NdCrosshair.Core;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ClassicLayer), "classic")]
public abstract record CrosshairLayer
{
    public const int MaxOffset = 200;
    public const int MinScale = 10;
    public const int MaxScale = 400;
    public const int MinBlur = 0;
    public const int MaxBlur = 10;

    public bool Visible { get; init; } = true;

    public int OffsetX { get; init; }

    public int OffsetY { get; init; }

    public int Scale { get; init; } = 100;

    public int Blur { get; init; }

    [JsonIgnore]
    public bool HasDefaultTransform => OffsetX == 0 && OffsetY == 0 && Scale == 100 && Blur == 0;

    public CrosshairLayer Normalize() => NormalizeContent() with
    {
        OffsetX = Math.Clamp(OffsetX, -MaxOffset, MaxOffset),
        OffsetY = Math.Clamp(OffsetY, -MaxOffset, MaxOffset),
        Scale = Math.Clamp(Scale, MinScale, MaxScale),
        Blur = Math.Clamp(Blur, MinBlur, MaxBlur),
    };

    protected abstract CrosshairLayer NormalizeContent();
}

public sealed record ClassicLayer : CrosshairLayer
{
    public CrosshairSettings Settings { get; init; } = new();

    protected override CrosshairLayer NormalizeContent() => this with { Settings = (Settings ?? new CrosshairSettings()).Clamp() };
}
