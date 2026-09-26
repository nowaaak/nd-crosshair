using NdCrosshair.Core;

namespace NdCrosshair.Core.Tests;

public sealed class ConfigStoreTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "NdCrosshairTests", Guid.NewGuid().ToString("N"));

    private string FilePath => Path.Combine(directory, "config.json");

    public void Dispose()
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void Load_WithoutFile_ReturnsDefaultsAsCreated()
    {
        var result = new ConfigStore(FilePath).Load();

        Assert.Equal(ConfigLoadStatus.Created, result.Status);
        Assert.Single(result.Config.Presets);
        Assert.Equal(HotkeyBinding.Default, result.Config.Hotkey);
        Assert.True(result.Config.OverlayVisible);
    }

    [Fact]
    public void SaveAndLoad_RoundTripsConfig()
    {
        var store = new ConfigStore(FilePath);
        var config = new AppConfig
        {
            Presets =
            [
                new Preset("Fortnite", new CrosshairSettings { Color = new CrosshairColor(0, 255, 255), Gap = 2 }),
                new Preset("Ärger ß", new CrosshairSettings { ShowDot = true, ShowOutline = false }),
            ],
            ActivePresetIndex = 1,
            MonitorDeviceName = @"\\.\DISPLAY2",
            OffsetX = -3,
            OffsetY = 7,
            OverlayVisible = false,
            Hotkey = new HotkeyBinding(HotkeyBinding.ModifierControl | HotkeyBinding.ModifierShift, 0x58),
        };

        store.Save(config);
        var result = store.Load();

        Assert.Equal(ConfigLoadStatus.Loaded, result.Status);
        Assert.Equal(config.Presets, result.Config.Presets);
        Assert.Equal(config.GameRules, result.Config.GameRules);
        Assert.Equal(config with { Presets = result.Config.Presets, GameRules = result.Config.GameRules }, result.Config);
        Assert.False(File.Exists(FilePath + ".tmp"));
    }

    [Fact]
    public void Load_WithCorruptFile_MovesItAsideAndReturnsDefaults()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(FilePath, "{ not json");

        var result = new ConfigStore(FilePath).Load();

        Assert.Equal(ConfigLoadStatus.RecoveredFromCorruptFile, result.Status);
        Assert.False(File.Exists(FilePath));
        Assert.NotNull(result.CorruptFileBackupPath);
        Assert.Equal("{ not json", File.ReadAllText(result.CorruptFileBackupPath));
        Assert.Single(result.Config.Presets);
    }

    [Fact]
    public void Load_WithInvalidColor_TreatsFileAsCorrupt()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(FilePath, """{ "presets": [ { "name": "x", "settings": { "color": "green" } } ] }""");

        var result = new ConfigStore(FilePath).Load();

        Assert.Equal(ConfigLoadStatus.RecoveredFromCorruptFile, result.Status);
    }

    [Fact]
    public void Load_NormalizesOutOfRangeValues()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(FilePath, """
            {
                "presets": [ { "name": "   ", "settings": { "lineThickness": 999, "opacity": 0 } } ],
                "activePresetIndex": 42,
                "offsetX": 100000,
                "hotkey": { "modifiers": 255, "virtualKey": 999 }
            }
            """);

        var config = new ConfigStore(FilePath).Load().Config;

        Assert.Equal(Preset.FallbackName, config.Presets[0].Name);
        Assert.Equal(CrosshairSettings.MaxLineThickness, config.Presets[0].Settings.LineThickness);
        Assert.Equal(CrosshairSettings.MinOpacity, config.Presets[0].Settings.Opacity);
        Assert.Equal(0, config.ActivePresetIndex);
        Assert.Equal(AppConfig.MaxOffset, config.OffsetX);
        Assert.True(config.Hotkey.IsNone);
    }

    [Fact]
    public void ExportAndImport_RoundTripsSystemSettings()
    {
        var path = Path.Combine(directory, "backup.json");
        var preset = new Preset("Rainbow", new CrosshairSettings { ColorMode = CrosshairColorMode.Rainbow, RainbowSpeed = 9 })
        {
            Hotkey = new HotkeyBinding(HotkeyBinding.ModifierControl, 0x31),
        };
        var config = new AppConfig
        {
            Presets = [preset],
            ShowOnlyOverGame = true,
            GameRules =
            [
                new GameRule(GameMatchKind.Process, "VALORANT-Win64-Shipping.exe") { PresetId = preset.Id },
                new GameRule(GameMatchKind.WindowTitle, "Fortnite"),
            ],
            AimHideButton = AimButton.Back,
            AimHideMode = AimHideMode.Toggle,
            PositionHotkeysEnabled = true,
            StreamerMode = true,
            StartMinimized = false,
            MinimizeToTray = false,
            CloseToTray = false,
            ShowPresetNotifications = false,
            Language = AppLanguage.English,
            NextPresetHotkey = new HotkeyBinding(HotkeyBinding.ModifierAlt, 0x78),
            PreviousPresetHotkey = new HotkeyBinding(0, 0x77),
        };

        ConfigStore.Export(config, path);
        var imported = ConfigStore.Import(path);

        Assert.Equal(config.Presets, imported.Presets);
        Assert.Equal(config.GameRules, imported.GameRules);
        Assert.Equal(config with { Presets = imported.Presets, GameRules = imported.GameRules }, imported);
        Assert.Contains("\"rainbow\"", File.ReadAllText(path), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Import_RejectsInvalidFile()
    {
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "broken.json");
        File.WriteAllText(path, "[1, 2");

        Assert.Throws<InvalidDataException>(() => ConfigStore.Import(path));
        Assert.Throws<FileNotFoundException>(() => ConfigStore.Import(Path.Combine(directory, "missing.json")));
    }

    [Fact]
    public void Load_MigratesLegacyGameWindowTitlesToTitleRules()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(FilePath, $$"""
            { "gameWindowTitles": [ "  Fortnite ", "", "fortnite", "{{new string('x', 300)}}", null ], "language": "Klingon" }
            """);

        var result = new ConfigStore(FilePath).Load();

        Assert.Equal(ConfigLoadStatus.RecoveredFromCorruptFile, result.Status);

        File.WriteAllText(FilePath, $$"""
            { "gameWindowTitles": [ "  Fortnite ", "", "fortnite", "{{new string('x', 300)}}", null ] }
            """);

        var store = new ConfigStore(FilePath);
        var config = store.Load().Config;

        Assert.Null(config.GameWindowTitles);
        Assert.Equal(2, config.GameRules.Count);
        Assert.All(config.GameRules, rule => Assert.Equal(GameMatchKind.WindowTitle, rule.Kind));
        Assert.Equal("Fortnite", config.GameRules[0].Pattern);
        Assert.Equal(GameRule.MaxPatternLength, config.GameRules[1].Pattern.Length);

        store.Save(config);
        Assert.DoesNotContain("gameWindowTitles", File.ReadAllText(FilePath), StringComparison.Ordinal);
    }

    [Fact]
    public void Load_WithoutGameSettings_UsesFortniteProcessRule()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(FilePath, "{}");

        var rule = Assert.Single(new ConfigStore(FilePath).Load().Config.GameRules);

        Assert.Equal(GameMatchKind.Process, rule.Kind);
        Assert.Equal(AppConfig.DefaultGameProcess, rule.Pattern);
    }

    [Fact]
    public void Normalize_RepairsPresetIdsAndDropsDanglingRuleLinks()
    {
        var shared = Guid.NewGuid();
        var config = new AppConfig
        {
            Presets =
            [
                new Preset("A", new CrosshairSettings()) { Id = shared },
                new Preset("B", new CrosshairSettings()) { Id = shared },
                new Preset("C", new CrosshairSettings()) { Id = Guid.Empty },
            ],
            GameRules =
            [
                new GameRule(GameMatchKind.Process, @"C:\Games\cs2.exe") { PresetId = shared },
                new GameRule(GameMatchKind.Process, "CS2.EXE"),
                new GameRule(GameMatchKind.WindowTitle, "Apex") { PresetId = Guid.NewGuid() },
            ],
            AimHideButton = (AimButton)99,
        }.Normalize();

        Assert.Equal(3, config.Presets.Select(preset => preset.Id).Distinct().Count());
        Assert.DoesNotContain(Guid.Empty, config.Presets.Select(preset => preset.Id));
        Assert.Equal(shared, config.Presets[0].Id);
        Assert.Equal(2, config.GameRules.Count);
        Assert.Equal("cs2.exe", config.GameRules[0].Pattern);
        Assert.Equal(shared, config.GameRules[0].PresetId);
        Assert.Null(config.GameRules[1].PresetId);
        Assert.Equal(AimButton.None, config.AimHideButton);
    }

    [Fact]
    public void Load_WithLockedFile_ReportsUnreadableWithoutTouchingIt()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(FilePath, "{}");

        ConfigLoadResult result;
        using (new FileStream(FilePath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            result = new ConfigStore(FilePath).Load();
        }

        Assert.Equal(ConfigLoadStatus.Unreadable, result.Status);
        Assert.False(string.IsNullOrEmpty(result.ErrorMessage));
        Assert.Equal("{}", File.ReadAllText(FilePath));
    }

    [Fact]
    public void Load_WithEmptyPresetList_AddsDefaultPreset()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(FilePath, """{ "presets": [] }""");

        var config = new ConfigStore(FilePath).Load().Config;

        Assert.Single(config.Presets);
    }
}
