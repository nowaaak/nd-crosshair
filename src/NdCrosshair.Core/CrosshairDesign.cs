namespace NdCrosshair.Core;

public sealed record CrosshairDesign
{
    public const int MaxLayers = 12;

    public IReadOnlyList<CrosshairLayer> Layers { get; init; } = [new ClassicLayer()];

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
        if (Layers is [ClassicLayer { Visible: true, HasDefaultTransform: true } classic])
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

        return this with { Layers = layers.Count == 0 ? [new ClassicLayer()] : layers };
    }

    public bool Equals(CrosshairDesign? other) =>
        other is not null && Layers.SequenceEqual(other.Layers);

    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var layer in Layers)
        {
            hash.Add(layer);
        }

        return hash.ToHashCode();
    }
}
