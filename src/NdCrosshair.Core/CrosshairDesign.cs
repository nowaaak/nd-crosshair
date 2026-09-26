namespace NdCrosshair.Core;

public sealed record CrosshairDesign
{
    public const int MaxLayers = 12;
    public const int MinFireSpread = 0;
    public const int MaxFireSpread = 30;
    public const int MinFireRecovery = 50;
    public const int MaxFireRecovery = 1000;
    public const int DefaultFireRecovery = 250;

    public IReadOnlyList<CrosshairLayer> Layers { get; init; } = [new ClassicLayer()];

    public int FireSpread { get; init; }

    public int FireRecovery { get; init; } = DefaultFireRecovery;

    public static CrosshairDesign FromClassic(CrosshairSettings settings) => new()
    {
        Layers = [new ClassicLayer { Settings = settings }],
    };

    public CrosshairSettings? ClassicSettings() => Layers.OfType<ClassicLayer>().FirstOrDefault()?.Settings;

    public CrosshairDesign WithLayer(int index, CrosshairLayer layer)
    {
        var layers = Layers.ToList();
        layers[index] = layer;
        return this with { Layers = layers };
    }

    public bool TryGetClassicOnly(out CrosshairSettings settings)
    {
        if (FireSpread == MinFireSpread && Layers is [ClassicLayer { Visible: true, HasDefaultTransform: true } classic])
        {
            settings = classic.Settings;
            return true;
        }

        settings = new CrosshairSettings();
        return false;
    }

    public CrosshairDesign Normalize()
    {
        var layers = (Layers ?? [])
            .Where(layer => layer is not null)
            .Select(layer => layer.Normalize())
            .Take(MaxLayers)
            .ToList();

        return this with
        {
            Layers = layers.Count == 0 ? [new ClassicLayer()] : layers,
            FireSpread = Math.Clamp(FireSpread, MinFireSpread, MaxFireSpread),
            FireRecovery = Math.Clamp(FireRecovery, MinFireRecovery, MaxFireRecovery),
        };
    }

    public bool Equals(CrosshairDesign? other) =>
        other is not null
        && FireSpread == other.FireSpread
        && FireRecovery == other.FireRecovery
        && Layers.SequenceEqual(other.Layers);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(FireSpread);
        hash.Add(FireRecovery);
        foreach (var layer in Layers)
        {
            hash.Add(layer);
        }

        return hash.ToHashCode();
    }
}
