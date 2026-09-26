using System.Text.Json.Serialization;

namespace NdCrosshair.Core;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ClassicLayer), "classic")]
[JsonDerivedType(typeof(ShapeLayer), "shape")]
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

public enum ShapeKind
{
    Rectangle,
    Triangle,
    Chevron,
    Arc,
    TShape,
}

public sealed record ShapeLayer : CrosshairLayer
{
    public const int MinSize = 1;
    public const int MaxSize = 200;
    public const int MinThickness = 1;
    public const int MaxThickness = 50;
    public const int MinSweep = 10;
    public const int MaxSweep = 360;

    public ShapeKind Kind { get; init; } = ShapeKind.Triangle;

    public int Width { get; init; } = 12;

    public int Height { get; init; } = 10;

    public int Thickness { get; init; } = 2;

    public int Sweep { get; init; } = 90;

    public int Rotation { get; init; }

    public bool Filled { get; init; } = true;

    public LayerStyle Style { get; init; } = new();

    [JsonIgnore]
    public bool SupportsFill => Kind is ShapeKind.Rectangle or ShapeKind.Triangle;

    protected override CrosshairLayer NormalizeContent() => this with
    {
        Kind = Enum.IsDefined(Kind) ? Kind : ShapeKind.Triangle,
        Width = Math.Clamp(Width, MinSize, MaxSize),
        Height = Math.Clamp(Height, MinSize, MaxSize),
        Thickness = Math.Clamp(Thickness, MinThickness, MaxThickness),
        Sweep = Math.Clamp(Sweep, MinSweep, MaxSweep),
        Rotation = Math.Clamp(Rotation, CrosshairSettings.MinRotation, CrosshairSettings.MaxRotation),
        Style = (Style ?? new LayerStyle()).Clamp(),
    };
}
