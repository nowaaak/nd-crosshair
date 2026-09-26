using System.Text.Json.Serialization;

namespace NdCrosshair.Core;

public sealed record Preset(string Name, CrosshairSettings Settings)
{
    public const int MaxNameLength = 40;
    public const string FallbackName = "Preset";

    public Guid Id { get; init; } = Guid.NewGuid();

    public HotkeyBinding Hotkey { get; init; } = HotkeyBinding.None;

    public static string NormalizeName(string? name)
    {
        var trimmed = name?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return FallbackName;
        }

        return trimmed.Length > MaxNameLength ? trimmed[..MaxNameLength] : trimmed;
    }
}

public sealed record HotkeyBinding(int Modifiers, int VirtualKey)
{
    public const int ModifierAlt = 0x1;
    public const int ModifierControl = 0x2;
    public const int ModifierShift = 0x4;
    public const int ModifierWin = 0x8;
    public const int AllModifiers = ModifierAlt | ModifierControl | ModifierShift | ModifierWin;

    private const int VirtualKeyF8 = 0x77;

    public static HotkeyBinding Default => new(0, VirtualKeyF8);

    public static HotkeyBinding None => new(0, 0);

    [JsonIgnore]
    public bool IsNone => VirtualKey == 0;

    public HotkeyBinding Normalize()
    {
        if (VirtualKey is < 0 or > 0xFE)
        {
            return None;
        }

        return new HotkeyBinding(Modifiers & AllModifiers, VirtualKey);
    }
}

public enum AimButton
{
    None,
    Right,
    Middle,
    Back,
    Forward,
}

public enum AimHideMode
{
    Hold,
    Toggle,
}

public enum AppLanguage
{
    System,
    German,
    English,
}

public sealed record AppConfig
{
    public const int MaxOffset = 500;
    public const int MaxGameRules = 20;
    public const string DefaultGameProcess = "FortniteClient-Win64-Shipping.exe";

    public IReadOnlyList<Preset> Presets { get; init; } = [new Preset("Standard", new CrosshairSettings())];

    public int ActivePresetIndex { get; init; }

    public string? MonitorDeviceName { get; init; }

    public int OffsetX { get; init; }

    public int OffsetY { get; init; }

    public bool OverlayVisible { get; init; } = true;

    public HotkeyBinding Hotkey { get; init; } = HotkeyBinding.Default;

    public HotkeyBinding NextPresetHotkey { get; init; } = HotkeyBinding.None;

    public HotkeyBinding PreviousPresetHotkey { get; init; } = HotkeyBinding.None;

    public bool PositionHotkeysEnabled { get; init; }

    public bool ShowOnlyOverGame { get; init; }

    public IReadOnlyList<GameRule> GameRules { get; init; } = [new GameRule(GameMatchKind.Process, DefaultGameProcess)];

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<string>? GameWindowTitles { get; init; }

    public AimButton AimHideButton { get; init; }

    public AimHideMode AimHideMode { get; init; }

    public bool StreamerMode { get; init; }

    public bool StartMinimized { get; init; } = true;

    public bool MinimizeToTray { get; init; } = true;

    public bool CloseToTray { get; init; } = true;

    public bool ShowPresetNotifications { get; init; } = true;

    public bool CheckForUpdates { get; init; } = true;

    public AppLanguage Language { get; init; }

    public AppConfig Normalize()
    {
        var presets = new List<Preset>();
        var usedIds = new HashSet<Guid>();
        foreach (var preset in (Presets ?? []).Where(preset => preset is not null))
        {
            var id = preset.Id == Guid.Empty || usedIds.Contains(preset.Id) ? Guid.NewGuid() : preset.Id;
            usedIds.Add(id);
            presets.Add(new Preset(Preset.NormalizeName(preset.Name), (preset.Settings ?? new CrosshairSettings()).Clamp())
            {
                Id = id,
                Hotkey = (preset.Hotkey ?? HotkeyBinding.None).Normalize(),
            });
        }

        if (presets.Count == 0)
        {
            presets.Add(new Preset("Standard", new CrosshairSettings()));
        }

        return this with
        {
            Presets = presets,
            ActivePresetIndex = Math.Clamp(ActivePresetIndex, 0, presets.Count - 1),
            MonitorDeviceName = string.IsNullOrWhiteSpace(MonitorDeviceName) ? null : MonitorDeviceName,
            OffsetX = Math.Clamp(OffsetX, -MaxOffset, MaxOffset),
            OffsetY = Math.Clamp(OffsetY, -MaxOffset, MaxOffset),
            Hotkey = (Hotkey ?? HotkeyBinding.Default).Normalize(),
            NextPresetHotkey = (NextPresetHotkey ?? HotkeyBinding.None).Normalize(),
            PreviousPresetHotkey = (PreviousPresetHotkey ?? HotkeyBinding.None).Normalize(),
            GameRules = NormalizeGameRules(presets),
            GameWindowTitles = null,
            AimHideButton = Enum.IsDefined(AimHideButton) ? AimHideButton : AimButton.None,
            AimHideMode = Enum.IsDefined(AimHideMode) ? AimHideMode : AimHideMode.Hold,
            Language = Enum.IsDefined(Language) ? Language : AppLanguage.System,
        };
    }

    private List<GameRule> NormalizeGameRules(IReadOnlyList<Preset> presets)
    {
        var presetIds = presets.Select(preset => preset.Id).ToHashSet();
        var source = GameWindowTitles is not null
            ? GameWindowTitles.Select(title => new GameRule(GameMatchKind.WindowTitle, title))
            : GameRules ?? [];

        return source
            .Where(rule => rule is not null)
            .Select(rule => rule.Normalize())
            .OfType<GameRule>()
            .Select(rule => rule.PresetId is { } presetId && !presetIds.Contains(presetId) ? rule with { PresetId = null } : rule)
            .DistinctBy(rule => (rule.Kind, rule.Pattern.ToUpperInvariant()))
            .Take(MaxGameRules)
            .ToList();
    }
}
