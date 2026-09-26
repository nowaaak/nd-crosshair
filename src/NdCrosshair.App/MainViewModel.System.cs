using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows.Threading;
using NdCrosshair.App.Localization;
using NdCrosshair.App.Services;
using NdCrosshair.Core;

namespace NdCrosshair.App;

internal sealed partial class MainViewModel
{
    private const int CaptureDelaySeconds = 3;

    private string? monitorDeviceName;
    private int offsetX;
    private int offsetY;
    private bool overlayVisible;
    private HotkeyBinding toggleHotkey = HotkeyBinding.Default;
    private HotkeyBinding nextPresetHotkey = HotkeyBinding.None;
    private HotkeyBinding previousPresetHotkey = HotkeyBinding.None;
    private bool isCapturingHotkey;
    private string toggleHotkeyStatus = string.Empty;
    private string nextHotkeyStatus = string.Empty;
    private string previousHotkeyStatus = string.Empty;
    private bool positionHotkeysEnabled;
    private string positionHotkeysStatus = string.Empty;
    private bool showOnlyOverGame;
    private string newGameTitle = string.Empty;
    private string captureStatus = string.Empty;
    private DispatcherTimer? captureTimer;
    private int captureRemaining;
    private AimButton aimHideButton;
    private AimHideMode aimHideMode;
    private bool streamerMode;
    private bool startWithWindows;
    private bool startMinimized;
    private bool minimizeToTray;
    private bool closeToTray;
    private bool showPresetNotifications;
    private bool checkForUpdates;
    private AppLanguage language;

    public ObservableCollection<MonitorOption> Monitors { get; } = [];

    public ObservableCollection<GameRuleItem> GameRules { get; } = [];

    public ObservableCollection<PresetChoice> PresetChoices { get; } = [];

    public bool HasGameRules => GameRules.Count > 0;

    public AimButton AimHideButton
    {
        get => aimHideButton;
        set
        {
            if (Enum.IsDefined(value) && SetField(ref aimHideButton, value))
            {
                OnPropertyChanged(nameof(IsAimHideEnabled));
                Changed?.Invoke(this, ChangeKind.Overlay);
            }
        }
    }

    public bool IsAimHideEnabled => aimHideButton != AimButton.None;

    public AimHideMode AimHideMode
    {
        get => aimHideMode;
        set
        {
            if (Enum.IsDefined(value))
            {
                SetOverlayField(ref aimHideMode, value);
            }
        }
    }

    public bool PositionHotkeysEnabled
    {
        get => positionHotkeysEnabled;
        set
        {
            if (SetField(ref positionHotkeysEnabled, value))
            {
                Changed?.Invoke(this, ChangeKind.Hotkey);
            }
        }
    }

    public string PositionHotkeysStatus
    {
        get => positionHotkeysStatus;
        set => SetField(ref positionHotkeysStatus, value);
    }

    public MonitorOption? SelectedMonitor
    {
        get => Monitors.FirstOrDefault(option => option.DeviceName == monitorDeviceName) ?? Monitors.FirstOrDefault();
        set
        {
            if (value is null || value.DeviceName == monitorDeviceName)
            {
                return;
            }

            monitorDeviceName = value.DeviceName;
            OnPropertyChanged();
            Changed?.Invoke(this, ChangeKind.Overlay);
        }
    }

    public string? MonitorDeviceName => monitorDeviceName;

    public int OffsetX
    {
        get => offsetX;
        set => SetOverlayField(ref offsetX, Math.Clamp(value, -AppConfig.MaxOffset, AppConfig.MaxOffset));
    }

    public int OffsetY
    {
        get => offsetY;
        set => SetOverlayField(ref offsetY, Math.Clamp(value, -AppConfig.MaxOffset, AppConfig.MaxOffset));
    }

    public int MinOffset => -AppConfig.MaxOffset;

    public int MaxOffset => AppConfig.MaxOffset;

    public bool OverlayVisible
    {
        get => overlayVisible;
        set
        {
            if (SetField(ref overlayVisible, value))
            {
                Changed?.Invoke(this, ChangeKind.Overlay | ChangeKind.Presets);
            }
        }
    }

    public bool ShowOnlyOverGame
    {
        get => showOnlyOverGame;
        set => SetOverlayField(ref showOnlyOverGame, value);
    }

    public string NewGameTitle
    {
        get => newGameTitle;
        set => SetField(ref newGameTitle, value);
    }

    public string CaptureStatus
    {
        get => captureStatus;
        private set => SetField(ref captureStatus, value);
    }

    public bool IsCapturingWindow => captureTimer is not null;

    public bool StreamerMode
    {
        get => streamerMode;
        set => SetOverlayField(ref streamerMode, value);
    }

    public HotkeyBinding ToggleHotkey
    {
        get => toggleHotkey;
        set => SetHotkey(ref toggleHotkey, value);
    }

    public HotkeyBinding NextPresetHotkey
    {
        get => nextPresetHotkey;
        set => SetHotkey(ref nextPresetHotkey, value);
    }

    public HotkeyBinding PreviousPresetHotkey
    {
        get => previousPresetHotkey;
        set => SetHotkey(ref previousPresetHotkey, value);
    }

    public string ToggleHotkeyText => HotkeyFormatter.Format(toggleHotkey);

    public bool IsCapturingHotkey
    {
        get => isCapturingHotkey;
        set
        {
            if (SetField(ref isCapturingHotkey, value))
            {
                Changed?.Invoke(this, ChangeKind.Hotkey);
            }
        }
    }

    public string ToggleHotkeyStatus
    {
        get => toggleHotkeyStatus;
        set => SetField(ref toggleHotkeyStatus, value);
    }

    public string NextHotkeyStatus
    {
        get => nextHotkeyStatus;
        set => SetField(ref nextHotkeyStatus, value);
    }

    public string PreviousHotkeyStatus
    {
        get => previousHotkeyStatus;
        set => SetField(ref previousHotkeyStatus, value);
    }

    public bool ShowPresetNotifications
    {
        get => showPresetNotifications;
        set => SetSystemField(ref showPresetNotifications, value);
    }

    public UpdateViewModel Updates { get; }

    public bool CheckForUpdates
    {
        get => checkForUpdates;
        set => SetSystemField(ref checkForUpdates, value);
    }

    public bool StartWithWindows
    {
        get => startWithWindows;
        set
        {
            if (SetField(ref startWithWindows, value))
            {
                Changed?.Invoke(this, ChangeKind.Autostart);
            }
        }
    }

    public bool StartMinimized
    {
        get => startMinimized;
        set => SetSystemField(ref startMinimized, value);
    }

    public bool MinimizeToTray
    {
        get => minimizeToTray;
        set => SetSystemField(ref minimizeToTray, value);
    }

    public bool CloseToTray
    {
        get => closeToTray;
        set => SetSystemField(ref closeToTray, value);
    }

    public AppLanguage Language
    {
        get => language;
        set
        {
            if (SetField(ref language, value))
            {
                Changed?.Invoke(this, ChangeKind.Language);
            }
        }
    }

    public string ConfigFilePath => AppIdentity.ConfigFilePath;

    public string VersionText => Loc.Format(
        "AboutVersion",
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0");

    public void SetStartWithWindowsSilently(bool value)
    {
        startWithWindows = value;
        OnPropertyChanged(nameof(StartWithWindows));
    }

    public void RefreshMonitors()
    {
        Monitors.Clear();
        Monitors.Add(new MonitorOption(null, Loc.T("MonitorPrimary")));

        var monitors = DisplayMonitors.GetAll();
        for (var i = 0; i < monitors.Count; i++)
        {
            var monitor = monitors[i];
            var label = monitor.IsPrimary
                ? Loc.Format("MonitorLabelPrimary", i + 1, monitor.Width, monitor.Height)
                : Loc.Format("MonitorLabel", i + 1, monitor.Width, monitor.Height);
            Monitors.Add(new MonitorOption(monitor.DeviceName, label));
        }

        OnPropertyChanged(nameof(SelectedMonitor));
    }

    public void ResetOffset()
    {
        OffsetX = 0;
        OffsetY = 0;
    }

    public void AddGameRule()
    {
        if (AddGameRule(GameRule.FromInput(NewGameTitle)))
        {
            NewGameTitle = string.Empty;
        }
    }

    public void RemoveGameRule(GameRuleItem rule)
    {
        rule.PropertyChanged -= OnGameRuleChanged;
        if (GameRules.Remove(rule))
        {
            OnPropertyChanged(nameof(HasGameRules));
            Changed?.Invoke(this, ChangeKind.Overlay);
        }
    }

    public void StartWindowCapture()
    {
        if (captureTimer is not null)
        {
            return;
        }

        captureRemaining = CaptureDelaySeconds;
        CaptureStatus = Loc.Format("CaptureCountdown", captureRemaining);
        captureTimer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, OnCaptureTick, Dispatcher.CurrentDispatcher);
        OnPropertyChanged(nameof(IsCapturingWindow));
    }

    public OverlayOptions ToOverlayOptions() => new(
        Current,
        monitorDeviceName,
        offsetX,
        offsetY,
        overlayVisible,
        showOnlyOverGame,
        GameRules.Select(rule => rule.ToRule()).ToList(),
        aimHideButton,
        aimHideMode,
        streamerMode);

    private void OnCaptureTick(object? sender, EventArgs e)
    {
        captureRemaining--;
        if (captureRemaining > 0)
        {
            CaptureStatus = Loc.Format("CaptureCountdown", captureRemaining);
            return;
        }

        captureTimer?.Stop();
        captureTimer = null;
        OnPropertyChanged(nameof(IsCapturingWindow));

        var foreground = ForegroundWindow.Get();
        var rule = foreground is { IsOwnProcess: false } info
            ? GameRule.FromInput(info.ProcessName) ?? GameRule.FromInput(info.Title)
            : null;

        if (rule is null)
        {
            CaptureStatus = Loc.T("CaptureFailed");
        }
        else if (AddGameRule(rule))
        {
            CaptureStatus = Loc.Format("CaptureAdded", rule.Pattern);
        }
        else
        {
            CaptureStatus = Loc.Format("CaptureAlreadyListed", rule.Pattern);
        }
    }

    private bool AddGameRule(GameRule? rule)
    {
        if (rule is null
            || GameRules.Count >= AppConfig.MaxGameRules
            || GameRules.Any(item => item.Kind == rule.Kind && string.Equals(item.Pattern, rule.Pattern, StringComparison.OrdinalIgnoreCase)))
        {
            return false;
        }

        AddGameRuleItem(new GameRuleItem(rule));
        Changed?.Invoke(this, ChangeKind.Overlay);
        return true;
    }

    private void AddGameRuleItem(GameRuleItem item)
    {
        item.PropertyChanged += OnGameRuleChanged;
        GameRules.Add(item);
        OnPropertyChanged(nameof(HasGameRules));
    }

    private void OnGameRuleChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e) =>
        Changed?.Invoke(this, ChangeKind.Overlay);

    private void SyncPresetChoices()
    {
        if (PresetChoices.Count == 0 || PresetChoices[0].Id is not null)
        {
            PresetChoices.Insert(0, new PresetChoice(null, Loc.T("RuleKeepPreset")));
        }

        PresetChoices[0].Name = Loc.T("RuleKeepPreset");

        for (var i = 0; i < Presets.Count; i++)
        {
            var preset = Presets[i];
            var index = PresetChoices.Select(choice => choice.Id).ToList().IndexOf(preset.Id);
            if (index < 0)
            {
                PresetChoices.Insert(i + 1, new PresetChoice(preset.Id, preset.Name));
                continue;
            }

            PresetChoices[index].Name = preset.Name;
            if (index != i + 1)
            {
                PresetChoices.Move(index, i + 1);
            }
        }

        while (PresetChoices.Count > Presets.Count + 1)
        {
            PresetChoices.RemoveAt(PresetChoices.Count - 1);
        }
    }

    private void SetHotkey(ref HotkeyBinding field, HotkeyBinding value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (SetField(ref field, value.Normalize(), propertyName))
        {
            OnPropertyChanged(nameof(ToggleHotkeyText));
            Changed?.Invoke(this, ChangeKind.Hotkey);
        }
    }

    private void SetOverlayField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (SetField(ref field, value, propertyName))
        {
            Changed?.Invoke(this, ChangeKind.Overlay);
        }
    }

    private void SetSystemField<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
    {
        if (SetField(ref field, value, propertyName))
        {
            Changed?.Invoke(this, ChangeKind.System);
        }
    }
}
