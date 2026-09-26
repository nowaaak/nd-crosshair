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
    Layer,
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

    private PresetItem selectedPreset = null!;
    private AppPage currentPage;
    private DesignTab designTab;
    private PreviewScene previewScene;
    private string importCode = string.Empty;
    private string shareStatus = string.Empty;

    public MainViewModel(AppConfig config, bool startWithWindows, UpdateViewModel updates)
    {
        this.startWithWindows = startWithWindows;
        Updates = updates;
        LoadConfig(config);
        RefreshMonitors();
        RenderPreview();
        Loc.Instance.LanguageChanged += (_, _) =>
        {
            RefreshMonitors();
            SyncPresetChoices();
            SyncLayers();
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
        set => SetField(ref previewScene, value);
    }

    public BitmapSource PreviewImage { get; private set; } = null!;

    public int PreviewZoom { get; private set; }

    public bool IsCrosshairEmpty { get; private set; }

    public double PreviewZoomedSize => Math.Min(PreviewImage.PixelWidth * PreviewZoom, PreviewTargetSize);

    public string PreviewScaleText => PreviewImage.PixelWidth > PreviewTargetSize
        ? Loc.T("PreviewScaledDown")
        : PreviewZoom > 1 ? Loc.Format("PreviewZoomed", PreviewZoom) : Loc.T("PreviewActualSize");

    public bool ShowParityHint => Current.HasDotParityMismatch();

    public string PreviewModeHint => Current.ColorMode switch
    {
        CrosshairColorMode.Rainbow => Loc.T("PreviewHintRainbow"),
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
            ResetLayerSelection();
            shareStatus = string.Empty;
            RefreshCurrent();
            Changed?.Invoke(this, ChangeKind.Overlay | ChangeKind.Presets);
        }
    }

    public int SelectedPresetIndex => Presets.IndexOf(selectedPreset);

    public bool CanDeletePreset => Presets.Count > 1;

    public CrosshairSettings Current => SelectedLayer is ClassicLayer classic ? classic.Settings : new CrosshairSettings();

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

    public int ArmCount
    {
        get => Current.ArmCount;
        set => Edit(Current with { ArmCount = value });
    }

    public bool IsFourArms => Current.ArmCount == CrosshairSettings.DefaultArmCount;

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

    public string ShareCodeText => DesignShareCode.Encode(selectedPreset.Design);

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

    public void AddPreset() => InsertPreset(new PresetItem(Loc.T("NewPresetName"), new CrosshairDesign()));

    public void DuplicatePreset() => InsertPreset(new PresetItem(Loc.Format("CopyPresetName", selectedPreset.Name), selectedPreset.Design));

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
        foreach (var rule in GameRules.Where(rule => rule.PresetId == removed.Id))
        {
            rule.PresetId = null;
        }

        SyncPresetChoices();
        OnPropertyChanged(nameof(CanDeletePreset));
        OnPropertyChanged(nameof(SelectedPresetIndex));
        Changed?.Invoke(this, ChangeKind.Presets | ChangeKind.Hotkey | ChangeKind.Overlay);
    }

    public int IndexOfPreset(Guid id)
    {
        for (var i = 0; i < Presets.Count; i++)
        {
            if (Presets[i].Id == id)
            {
                return i;
            }
        }

        return -1;
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
        if (GameCodeImporter.IsGameCode(ImportCode))
        {
            ImportGameCode();
            return;
        }

        if (!DesignShareCode.TryDecode(ImportCode, out var design, out var error))
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

        InsertPreset(new PresetItem(Loc.T("ImportedPresetName"), design));
        ImportCode = string.Empty;
        ShareStatus = Loc.T("ShareImported");
    }

    private void ImportGameCode()
    {
        var screenHeight = DisplayMonitors.Resolve(monitorDeviceName)?.Height ?? 0;
        if (!GameCodeImporter.TryImport(ImportCode, screenHeight, out var result, out var error) || result is null)
        {
            ShareStatus = error switch
            {
                GameCodeError.ChecksumMismatch => Loc.T("ShareErrorChecksum"),
                GameCodeError.OutdatedCs2Format => Loc.T("GameCodeOutdatedCs2"),
                _ => Loc.T("ShareErrorInvalid"),
            };
            return;
        }

        var name = result.Source == GameCodeSource.CounterStrike2 ? Loc.T("ImportedCs2PresetName") : Loc.T("ImportedValorantPresetName");
        InsertPreset(new PresetItem(name, CrosshairDesign.FromClassic(result.Settings)));
        ImportCode = string.Empty;
        ShareStatus = result.Notes.Count == 0
            ? Loc.T("ShareImported")
            : Loc.Format("GameCodeImportedWithNotes", string.Join(" ", result.Notes.Select(GameCodeNoteText)));
    }

    private static string GameCodeNoteText(GameCodeNote note) => note switch
    {
        GameCodeNote.DynamicShownStatic => Loc.T("NoteDynamicShownStatic"),
        GameCodeNote.OuterLinesIgnored => Loc.T("NoteOuterLinesIgnored"),
        GameCodeNote.SeparateVerticalLengthIgnored => Loc.T("NoteSeparateVerticalLength"),
        GameCodeNote.OutlineOpacityApproximated => Loc.T("NoteOutlineOpacity"),
        GameCodeNote.DotOpacityApproximated => Loc.T("NoteDotOpacity"),
        GameCodeNote.CircleApproximated => Loc.T("NoteCircle"),
        GameCodeNote.SquareApproximated => Loc.T("NoteSquare"),
        GameCodeNote.HalfOutlineApproximated => Loc.T("NoteHalfOutline"),
        GameCodeNote.FollowRecoilIgnored => Loc.T("NoteFollowRecoil"),
        _ => Loc.T("NoteValuesClamped"),
    };

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
        PositionHotkeysEnabled = positionHotkeysEnabled,
        ShowOnlyOverGame = showOnlyOverGame,
        GameRules = GameRules.Select(rule => rule.ToRule()).ToList(),
        AimHideButton = aimHideButton,
        AimHideMode = aimHideMode,
        StreamerMode = streamerMode,
        StartMinimized = startMinimized,
        MinimizeToTray = minimizeToTray,
        CloseToTray = closeToTray,
        ShowPresetNotifications = showPresetNotifications,
        CheckForUpdates = checkForUpdates,
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

        foreach (var rule in GameRules)
        {
            rule.PropertyChanged -= OnGameRuleChanged;
        }

        GameRules.Clear();

        foreach (var item in Presets)
        {
            item.PropertyChanged -= OnPresetItemChanged;
        }

        Presets.Clear();
        foreach (var preset in config.Presets)
        {
            AddPresetItem(new PresetItem(preset.Name, preset.Design, preset.Id, preset.Hotkey));
        }

        SyncPresetChoices();
        selectedPreset = Presets[config.ActivePresetIndex];
        ResetLayerSelection();
        monitorDeviceName = config.MonitorDeviceName;
        offsetX = config.OffsetX;
        offsetY = config.OffsetY;
        overlayVisible = config.OverlayVisible;
        toggleHotkey = config.Hotkey;
        nextPresetHotkey = config.NextPresetHotkey;
        previousPresetHotkey = config.PreviousPresetHotkey;
        positionHotkeysEnabled = config.PositionHotkeysEnabled;
        showOnlyOverGame = config.ShowOnlyOverGame;
        foreach (var rule in config.GameRules)
        {
            AddGameRuleItem(new GameRuleItem(rule));
        }

        aimHideButton = config.AimHideButton;
        aimHideMode = config.AimHideMode;
        streamerMode = config.StreamerMode;
        startMinimized = config.StartMinimized;
        minimizeToTray = config.MinimizeToTray;
        closeToTray = config.CloseToTray;
        showPresetNotifications = config.ShowPresetNotifications;
        checkForUpdates = config.CheckForUpdates;
        language = config.Language;
    }

    private void InsertPreset(PresetItem item)
    {
        AddPresetItem(item);
        SyncPresetChoices();
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
            SyncPresetChoices();
            Changed?.Invoke(this, ChangeKind.Presets);
        }
        else if (e.PropertyName == nameof(PresetItem.Hotkey))
        {
            Changed?.Invoke(this, ChangeKind.Hotkey);
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
        if (SelectedLayer is ClassicLayer classic)
        {
            EditLayer(classic with { Settings = updated.Clamp() });
        }
        else
        {
            OnPropertyChanged(string.Empty);
        }
    }

    private void RefreshCurrent()
    {
        RenderPreview();
        OnPropertyChanged(string.Empty);
    }

    private void RenderPreview()
    {
        var rasterized = DesignRenderer.Rasterize(selectedPreset.Design);
        var image = rasterized.Compose(null);
        IsCrosshairEmpty = !image.Pixels.Where((_, index) => index % CrosshairImage.BytesPerPixel == 3).Any(alpha => alpha > 0);
        PreviewImage = CrosshairBitmaps.ToBitmapSource(image);
        PreviewZoom = Math.Clamp((int)(PreviewTargetSize / rasterized.Size), 1, MaxPreviewZoom);
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
