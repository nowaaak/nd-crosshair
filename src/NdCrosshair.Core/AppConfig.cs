using System.Text.Json.Serialization;

namespace NdCrosshair.Core;

public sealed record Preset(string Name, CrosshairSettings Settings)
{
    public const int MaxNameLength = 40;
    public const string FallbackName = "Preset";

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

public enum AppLanguage
{
    System,
    German,
    English,
}

public sealed record AppConfig
{
    public const int MaxOffset = 500;
    public const int MaxGameWindowTitles = 10;
    public const int MaxGameWindowTitleLength = 100;

    public IReadOnlyList<Preset> Presets { get; init; } = [new Preset("Standard", new CrosshairSettings())];

    public int ActivePresetIndex { get; init; }

    public string? MonitorDeviceName { get; init; }

    public int OffsetX { get; init; }

    public int OffsetY { get; init; }

    public bool OverlayVisible { get; init; } = true;

    public HotkeyBinding Hotkey { get; init; } = HotkeyBinding.Default;

    public HotkeyBinding NextPresetHotkey { get; init; } = HotkeyBinding.None;

    public HotkeyBinding PreviousPresetHotkey { get; init; } = HotkeyBinding.None;

    public bool ShowOnlyOverGame { get; init; }

    public IReadOnlyList<string> GameWindowTitles { get; init; } = ["Fortnite"];

    public bool StreamerMode { get; init; }

    public bool StartMinimized { get; init; } = true;

    public bool MinimizeToTray { get; init; } = true;

    public bool CloseToTray { get; init; } = true;

    public bool ShowPresetNotifications { get; init; } = true;

    public AppLanguage Language { get; init; }

    public AppConfig Normalize()
    {
        var presets = (Presets ?? [])
            .Where(preset => preset is not null)
            .Select(preset => new Preset(Preset.NormalizeName(preset.Name), (preset.Settings ?? new CrosshairSettings()).Clamp()))
            .ToList();

        if (presets.Count == 0)
        {
            presets.Add(new Preset("Standard", new CrosshairSettings()));
        }

        var titles = (GameWindowTitles ?? [])
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .Select(title => title.Trim())
            .Select(title => title.Length > MaxGameWindowTitleLength ? title[..MaxGameWindowTitleLength] : title)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(MaxGameWindowTitles)
            .ToList();

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
            GameWindowTitles = titles,
            Language = Enum.IsDefined(Language) ? Language : AppLanguage.System,
        };
    }
}
