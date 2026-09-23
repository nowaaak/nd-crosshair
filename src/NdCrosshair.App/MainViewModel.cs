using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NdCrosshair.App.Localization;
using NdCrosshair.Core;

namespace NdCrosshair.App;

[Flags]
internal enum ChangeKind
{
    None = 0,
    Overlay = 1,
    Hotkey = 2,
    Presets = 4,
    System = 8,
    Autostart = 16,
    Language = 32,
    All = Overlay | Hotkey | Presets | System | Language,
}

internal enum AppPage
{
    Crosshair,
    Presets,
    Overlay,
    Hotkeys,
    System,
    About,
}

internal enum DesignTab
{
    Shape,
    Color,
    Effects,
}

internal enum PreviewScene
{
    Dark,
    Light,
    Sky,
    Forest,
}

internal sealed record MonitorOption(string? DeviceName, string Label);

internal sealed partial class MainViewModel : INotifyPropertyChanged
{
    private const double PreviewTargetSize = 240;
    private const int MaxPreviewZoom = 12;

    private static readonly IReadOnlyDictionary<PreviewScene, CrosshairColor> SceneColors = new Dictionary<PreviewScene, CrosshairColor>
    {
        [PreviewScene.Dark] = new(22, 25, 29),
        [PreviewScene.Light] = new(228, 231, 234),
        [PreviewScene.Sky] = new(134, 183, 232),
        [PreviewScene.Forest] = new(62, 106, 54),
    };

    private PresetItem selectedPreset = null!;
    private AppPage currentPage;
    private DesignTab designTab;
    private PreviewScene previewScene;
    private string importCode = string.Empty;
    private string shareStatus = string.Empty;

    public MainViewModel(AppConfig config, bool startWithWindows)
    {
        this.startWithWindows = startWithWindows;
        LoadConfig(config);
        RefreshMonitors();
        RenderPreview();
        Loc.Instance.LanguageChanged += (_, _) =>
        {
            RefreshMonitors();
            OnPropertyChanged(string.Empty);
        };
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<ChangeKind>? Changed;

    public ObservableCollection<PresetItem> Presets { get; } = [];

    public IReadOnlyList<string> ColorSwatches { get; } =
        ["#00FF00", "#00FFFF", "#FFFF00", "#FF00FF", "#FF0000", "#FFFFFF"];

    public AppPage CurrentPage
    {
        get => currentPage;
        set => SetField(ref currentPage, value);
    }

    public DesignTab DesignTab
    {
        get => designTab;
        set => SetField(ref designTab, value);
    }

    public PreviewScene PreviewScene
    {
        get => previewScene;
        set
        {
            if (SetField(ref previewScene, value))
            {
                RenderPreview();
                OnPropertyChanged(nameof(PreviewImage));
                OnPropertyChanged(nameof(PreviewZoomedSize));
            }
        }
    }

    public BitmapSource PreviewImage { get; private set; } = null!;

    public int PreviewZoom { get; private set; }

    public bool IsCrosshairEmpty { get; private set; }

    public double PreviewZoomedSize => Math.Min(PreviewImage.PixelWidth * PreviewZoom, PreviewTargetSize);

    public string PreviewScaleText => PreviewImage.PixelWidth > PreviewTargetSize
        ? Loc.T("PreviewScaledDown")
        : PreviewZoom > 1 ? Loc.Format("PreviewZoomed", PreviewZoom) : Loc.T("PreviewActualSize");

    public string PreviewModeHint => Current.ColorMode switch
    {
        CrosshairColorMode.Rainbow => Loc.T("PreviewHintRainbow"),
        CrosshairColorMode.Adaptive => Loc.T("PreviewHintAdaptive"),
        _ => string.Empty,
    };

    public PresetItem SelectedPreset
    {
        get => selectedPreset;
        set
        {
            if (value is null || ReferenceEquals(value, selectedPreset))
            {
                return;
            }

            selectedPreset = value;
            shareStatus = string.Empty;
            RefreshCurrent();
            Changed?.Invoke(this, ChangeKind.Overlay | ChangeKind.Presets);
        }
    }

    public int SelectedPresetIndex => Presets.IndexOf(selectedPreset);

    public bool CanDeletePreset => Presets.Count > 1;

    public CrosshairSettings Current => selectedPreset.Settings;

    public bool ShowTop
    {
        get => Current.ShowTop;
        set => Edit(Current with { ShowTop = value });
    }

    public bool ShowBottom
    {
        get => Current.ShowBottom;
        set => Edit(Current with { ShowBottom = value });
    }

    public bool ShowLeft
    {
        get => Current.ShowLeft;
        set => Edit(Current with { ShowLeft = value });
    }

    public bool ShowRight
    {
        get => Current.ShowRight;
        set => Edit(Current with { ShowRight = value });
    }

    public int LineLength
    {
        get => Current.LineLength;
        set => Edit(Current with { LineLength = value });
    }

    public int LineThickness
    {
        get => Current.LineThickness;
        set => Edit(Current with { LineThickness = value });
    }

    public int Gap
    {
        get => Current.Gap;
        set => Edit(Current with { Gap = value });
    }

    public int Rotation
    {
        get => Current.Rotation;
        set => Edit(Current with { Rotation = value });
    }

    public bool ShowDot
    {
        get => Current.ShowDot;
        set => Edit(Current with { ShowDot = value });
    }

    public bool RoundDot
    {
        get => Current.RoundDot;
        set => Edit(Current with { RoundDot = value });
    }

    public int DotSize
    {
        get => Current.DotSize;
        set => Edit(Current with { DotSize = value });
    }

    public bool ShowRing
    {
        get => Current.ShowRing;
        set => Edit(Current with { ShowRing = value });
    }

    public int RingRadius
    {
        get => Current.RingRadius;
        set => Edit(Current with { RingRadius = value });
    }

    public int RingThickness
    {
        get => Current.RingThickness;
        set => Edit(Current with { RingThickness = value });
    }

    public bool ShowOutline
    {
        get => Current.ShowOutline;
        set => Edit(Current with { ShowOutline = value });
    }

    public int OutlineThickness
    {
        get => Current.OutlineThickness;
        set => Edit(Current with { OutlineThickness = value });
    }

    public bool ShowShadow
    {
        get => Current.ShowShadow;
        set => Edit(Current with { ShowShadow = value });
    }

    public int ShadowSize
    {
        get => Current.ShadowSize;
        set => Edit(Current with { ShadowSize = value });
    }

    public int ShadowOpacity
    {
        get => Current.ShadowOpacity;
        set => Edit(Current with { ShadowOpacity = value });
    }

    public int Opacity
    {
        get => Current.Opacity;
        set => Edit(Current with { Opacity = value });
    }

    public CrosshairColorMode ColorMode
    {
        get => Current.ColorMode;
        set => Edit(Current with { ColorMode = value });
    }

    public int RainbowSpeed
    {
        get => Current.RainbowSpeed;
        set => Edit(Current with { RainbowSpeed = value });
    }

    public string ColorHex
    {
        get => Current.Color.ToHex();
        set => EditColor(value, color => Current with { Color = color });
    }

    public string OutlineColorHex
    {
        get => Current.OutlineColor.ToHex();
        set => EditColor(value, color => Current with { OutlineColor = color });
    }

    public string ShadowColorHex
    {
        get => Current.ShadowColor.ToHex();
        set => EditColor(value, color => Current with { ShadowColor = color });
    }

    public string ShareCodeText => ShareCode.Encode(Current);

    public string ImportCode
    {
        get => importCode;
        set => SetField(ref importCode, value);
    }

    public string ShareStatus
    {
        get => shareStatus;
        set => SetField(ref shareStatus, value);
    }

    public void AddPreset() => InsertPreset(new PresetItem(Loc.T("NewPresetName"), new CrosshairSettings()));

    public void DuplicatePreset() => InsertPreset(new PresetItem(Loc.Format("CopyPresetName", selectedPreset.Name), Current));

    public void DeletePreset()
    {
        if (Presets.Count <= 1)
        {
            return;
        }

        var index = Presets.IndexOf(selectedPreset);
        var removed = selectedPreset;
        SelectedPreset = Presets[index == Presets.Count - 1 ? index - 1 : index + 1];
        removed.PropertyChanged -= OnPresetItemChanged;
        Presets.Remove(removed);
        OnPropertyChanged(nameof(CanDeletePreset));
        OnPropertyChanged(nameof(SelectedPresetIndex));
        Changed?.Invoke(this, ChangeKind.Presets);
    }

    public void SelectPreset(int index)
    {
        if (index >= 0 && index < Presets.Count)
        {
            SelectedPreset = Presets[index];
        }
    }

    public void SelectRelativePreset(int step)
    {
        var count = Presets.Count;
        SelectPreset(((SelectedPresetIndex + step) % count + count) % count);
    }

    public void ImportShareCode()
    {
        if (!ShareCode.TryDecode(ImportCode, out var settings, out var error))
        {
            ShareStatus = error switch
            {
                ShareCodeError.Empty => Loc.T("ShareErrorEmpty"),
                ShareCodeError.UnknownPrefix => Loc.T("ShareErrorPrefix"),
                ShareCodeError.ChecksumMismatch => Loc.T("ShareErrorChecksum"),
                _ => Loc.T("ShareErrorInvalid"),
            };
            return;
        }

        InsertPreset(new PresetItem(Loc.T("ImportedPresetName"), settings));
        ImportCode = string.Empty;
        ShareStatus = Loc.T("ShareImported");
    }

    public AppConfig ToConfig() => new AppConfig
    {
        Presets = Presets.Select(item => item.ToPreset()).ToList(),
        ActivePresetIndex = Presets.IndexOf(selectedPreset),
        MonitorDeviceName = monitorDeviceName,
        OffsetX = offsetX,
        OffsetY = offsetY,
        OverlayVisible = overlayVisible,
        Hotkey = toggleHotkey,
        NextPresetHotkey = nextPresetHotkey,
        PreviousPresetHotkey = previousPresetHotkey,
        ShowOnlyOverGame = showOnlyOverGame,
        GameWindowTitles = GameWindowTitles.ToList(),
        StreamerMode = streamerMode,
        StartMinimized = startMinimized,
        MinimizeToTray = minimizeToTray,
        CloseToTray = closeToTray,
        ShowPresetNotifications = showPresetNotifications,
        Language = language,
    }.Normalize();

    public void ApplyConfig(AppConfig config)
    {
        LoadConfig(config);
        RenderPreview();
        OnPropertyChanged(string.Empty);
        Changed?.Invoke(this, ChangeKind.All);
    }

    private void LoadConfig(AppConfig config)
    {
        config = config.Normalize();

        foreach (var item in Presets)
        {
            item.PropertyChanged -= OnPresetItemChanged;
        }

        Presets.Clear();
        foreach (var preset in config.Presets)
        {
            AddPresetItem(new PresetItem(preset.Name, preset.Settings));
        }

        selectedPreset = Presets[config.ActivePresetIndex];
        monitorDeviceName = config.MonitorDeviceName;
        offsetX = config.OffsetX;
        offsetY = config.OffsetY;
        overlayVisible = config.OverlayVisible;
        toggleHotkey = config.Hotkey;
        nextPresetHotkey = config.NextPresetHotkey;
        previousPresetHotkey = config.PreviousPresetHotkey;
        showOnlyOverGame = config.ShowOnlyOverGame;
        GameWindowTitles.Clear();
        foreach (var title in config.GameWindowTitles)
        {
            GameWindowTitles.Add(title);
        }

        streamerMode = config.StreamerMode;
        startMinimized = config.StartMinimized;
        minimizeToTray = config.MinimizeToTray;
        closeToTray = config.CloseToTray;
        showPresetNotifications = config.ShowPresetNotifications;
        language = config.Language;
    }

    private void InsertPreset(PresetItem item)
    {
        AddPresetItem(item);
        OnPropertyChanged(nameof(CanDeletePreset));
        SelectedPreset = item;
    }

    private void AddPresetItem(PresetItem item)
    {
        item.PropertyChanged += OnPresetItemChanged;
        Presets.Add(item);
    }

    private void OnPresetItemChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PresetItem.Name))
        {
            Changed?.Invoke(this, ChangeKind.Presets);
        }
    }

    private void EditColor(string value, Func<CrosshairColor, CrosshairSettings> update, [CallerMemberName] string? propertyName = null)
    {
        if (CrosshairColor.TryParseHex(value, out var color))
        {
            Edit(update(color));
        }
        else
        {
            OnPropertyChanged(propertyName);
        }
    }

    private void Edit(CrosshairSettings updated)
    {
        updated = updated.Clamp();
        if (updated == Current)
        {
            OnPropertyChanged(string.Empty);
            return;
        }

        selectedPreset.Settings = updated;
        shareStatus = string.Empty;
        RefreshCurrent();
        Changed?.Invoke(this, ChangeKind.Overlay);
    }

    private void RefreshCurrent()
    {
        RenderPreview();
        OnPropertyChanged(string.Empty);
    }

    private void RenderPreview()
    {
        var layers = CrosshairRenderer.Rasterize(Current);
        var fill = Current.ColorMode == CrosshairColorMode.Adaptive
            ? AdaptiveColorSelector.Choose(Current.Color, SceneColors[previewScene])
            : Current.Color;
        var image = layers.Compose(fill);
        IsCrosshairEmpty = !image.Pixels.Where((_, index) => index % CrosshairImage.BytesPerPixel == 3).Any(alpha => alpha > 0);
        PreviewImage = CrosshairBitmaps.ToBitmapSource(image);
        PreviewZoom = Math.Clamp((int)(PreviewTargetSize / layers.Size), 1, MaxPreviewZoom);
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
