using System.Collections.ObjectModel;
using NdCrosshair.App.Localization;
using NdCrosshair.Core;

namespace NdCrosshair.App;

internal sealed partial class MainViewModel
{
    private static readonly string ClassicGlyph = char.ConvertFromUtf32(0xE710);
    private static readonly string ShapeGlyph = char.ConvertFromUtf32(0xF158);
    private static readonly string TextGlyph = char.ConvertFromUtf32(0xE8D2);
    private static readonly string ImageGlyph = char.ConvertFromUtf32(0xE91B);

    private int selectedLayerIndex;
    private bool syncingLayers;

    public ObservableCollection<LayerItem> Layers { get; } = [];

    public LayerItem? SelectedLayerItem
    {
        get => Layers.FirstOrDefault(item => item.DesignIndex == selectedLayerIndex);
        set
        {
            if (value is null || syncingLayers || value.DesignIndex == selectedLayerIndex)
            {
                return;
            }

            selectedLayerIndex = value.DesignIndex;
            OnLayerSelectionChanged();
        }
    }

    public CrosshairLayer SelectedLayer => Design.Layers[Math.Clamp(selectedLayerIndex, 0, Design.Layers.Count - 1)];

    public bool IsClassicLayer => SelectedLayer is ClassicLayer;

    public bool CanAddLayer => Design.Layers.Count < CrosshairDesign.MaxLayers;

    public bool CanRemoveLayer => Design.Layers.Count > 1;

    public bool CanMoveLayerForward => selectedLayerIndex < Design.Layers.Count - 1;

    public bool CanMoveLayerBackward => selectedLayerIndex > 0;

    public int LayerOffsetX
    {
        get => SelectedLayer.OffsetX;
        set => EditLayer(SelectedLayer with { OffsetX = value });
    }

    public int LayerOffsetY
    {
        get => SelectedLayer.OffsetY;
        set => EditLayer(SelectedLayer with { OffsetY = value });
    }

    public int LayerScale
    {
        get => SelectedLayer.Scale;
        set => EditLayer(SelectedLayer with { Scale = value });
    }

    public int LayerBlur
    {
        get => SelectedLayer.Blur;
        set => EditLayer(SelectedLayer with { Blur = value });
    }

    public int FireSpread
    {
        get => Design.FireSpread;
        set => EditDesign(Design with { FireSpread = value });
    }

    public int FireRecovery
    {
        get => Design.FireRecovery;
        set => EditDesign(Design with { FireRecovery = value });
    }

    public int MinLayerOffset => -CrosshairLayer.MaxOffset;

    public int MaxLayerOffset => CrosshairLayer.MaxOffset;

    private CrosshairDesign Design => selectedPreset.Design;

    private LayerStyle CurrentStyle => SelectedLayer switch
    {
        ClassicLayer classic => LayerStyle.From(classic.Settings),
        ShapeLayer shape => shape.Style,
        TextLayer text => text.Style,
        _ => new LayerStyle(),
    };

    private void EditStyle(LayerStyle style)
    {
        switch (SelectedLayer)
        {
            case ClassicLayer classic:
                EditLayer(classic with { Settings = style.ApplyTo(classic.Settings) });
                break;
            case TextLayer text:
                EditLayer(text with { Style = style });
                break;
            case ShapeLayer shape:
                EditLayer(shape with { Style = style });
                break;
            default:
                OnPropertyChanged(string.Empty);
                break;
        }
    }

    public void AddClassicLayer() => AddLayer(new ClassicLayer());

    public void RemoveSelectedLayer()
    {
        if (!CanRemoveLayer)
        {
            return;
        }

        var layers = Design.Layers.ToList();
        layers.RemoveAt(selectedLayerIndex);
        selectedLayerIndex = Math.Min(selectedLayerIndex, layers.Count - 1);
        ApplyDesign(Design with { Layers = layers });
    }

    public void MoveSelectedLayer(int step)
    {
        var target = selectedLayerIndex + step;
        if (target < 0 || target >= Design.Layers.Count)
        {
            return;
        }

        var layers = Design.Layers.ToList();
        (layers[selectedLayerIndex], layers[target]) = (layers[target], layers[selectedLayerIndex]);
        selectedLayerIndex = target;
        ApplyDesign(Design with { Layers = layers });
    }

    private void AddLayer(CrosshairLayer layer)
    {
        if (!CanAddLayer)
        {
            return;
        }

        selectedLayerIndex = Design.Layers.Count;
        ApplyDesign(Design with { Layers = [.. Design.Layers, layer] });
        DesignTab = DesignTab.Shape;
    }

    private void EditLayer(CrosshairLayer updated)
    {
        var normalized = updated.Normalize();
        if (normalized == SelectedLayer)
        {
            OnPropertyChanged(string.Empty);
            return;
        }

        ApplyDesign(Design.WithLayer(selectedLayerIndex, normalized));
    }

    private void EditDesign(CrosshairDesign updated)
    {
        var normalized = updated.Normalize();
        if (normalized == Design)
        {
            OnPropertyChanged(string.Empty);
            return;
        }

        ApplyDesign(normalized);
    }

    private void ApplyDesign(CrosshairDesign design)
    {
        selectedPreset.Design = design.Normalize();
        selectedLayerIndex = Math.Clamp(selectedLayerIndex, 0, selectedPreset.Design.Layers.Count - 1);
        shareStatus = string.Empty;
        SyncLayers();
        RefreshCurrent();
        Changed?.Invoke(this, ChangeKind.Overlay);
    }

    private void OnLayerSelectionChanged()
    {
        EnsureTabAvailable();
        OnPropertyChanged(string.Empty);
    }

    private void ResetLayerSelection()
    {
        var classic = Design.Layers.ToList().FindIndex(layer => layer is ClassicLayer);
        selectedLayerIndex = classic < 0 ? 0 : classic;
        SyncLayers();
        EnsureTabAvailable();
    }

    private void EnsureTabAvailable()
    {
        if (SelectedLayer is ImageLayer && DesignTab == DesignTab.Color)
        {
            DesignTab = DesignTab.Shape;
        }
    }

    private void SetLayerVisibility(LayerItem item, bool visible)
    {
        if (syncingLayers || item.DesignIndex >= Design.Layers.Count)
        {
            return;
        }

        var layer = Design.Layers[item.DesignIndex];
        if (layer.Visible != visible)
        {
            ApplyDesign(Design.WithLayer(item.DesignIndex, layer with { Visible = visible }));
        }
    }

    private void SyncLayers()
    {
        syncingLayers = true;
        try
        {
            var layers = Design.Layers;
            while (Layers.Count > layers.Count)
            {
                Layers.RemoveAt(Layers.Count - 1);
            }

            while (Layers.Count < layers.Count)
            {
                Layers.Add(new LayerItem(SetLayerVisibility));
            }

            for (var designIndex = 0; designIndex < layers.Count; designIndex++)
            {
                var layer = layers[designIndex];
                var item = Layers[layers.Count - 1 - designIndex];
                item.DesignIndex = designIndex;
                item.Title = LayerTitle(layer);
                item.Subtitle = Loc.Format("LayerNumber", designIndex + 1);
                item.Glyph = LayerGlyph(layer);
                item.SetVisibleSilently(layer.Visible);
            }
        }
        finally
        {
            syncingLayers = false;
        }

        OnPropertyChanged(nameof(SelectedLayerItem));
    }

    private static string LayerTitle(CrosshairLayer layer) => layer switch
    {
        ShapeLayer shape => ShapeName(shape.Kind),
        TextLayer text => string.IsNullOrWhiteSpace(text.Text) ? Loc.T("LayerText") : text.Text,
        ImageLayer => Loc.T("LayerImage"),
        _ => Loc.T("LayerClassic"),
    };

    private static string ShapeName(ShapeKind kind) => kind switch
    {
        ShapeKind.Rectangle => Loc.T("ShapeRectangle"),
        ShapeKind.Chevron => Loc.T("ShapeChevron"),
        ShapeKind.Arc => Loc.T("ShapeArc"),
        ShapeKind.TShape => Loc.T("ShapeTShape"),
        _ => Loc.T("ShapeTriangle"),
    };

    private static string LayerGlyph(CrosshairLayer layer) => layer switch
    {
        ShapeLayer => ShapeGlyph,
        TextLayer => TextGlyph,
        ImageLayer => ImageGlyph,
        _ => ClassicGlyph,
    };
}
