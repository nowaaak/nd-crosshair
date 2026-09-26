using System.IO;
using System.Windows;
using System.Windows.Threading;
using NdCrosshair.App.Localization;
using NdCrosshair.App.Services;
using NdCrosshair.Core;

namespace NdCrosshair.App;

internal sealed class AppController : IDisposable
{
    private const int PresetHotkeyIdBase = 100;
    private const int PositionModifiers = HotkeyBinding.ModifierAlt | HotkeyBinding.ModifierShift;
    private const int VirtualKeyHome = 0x24;
    private const int VirtualKeyLeft = 0x25;
    private const int VirtualKeyUp = 0x26;
    private const int VirtualKeyRight = 0x27;
    private const int VirtualKeyDown = 0x28;

    private static readonly TimeSpan SaveDelay = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan PresetToastDuration = TimeSpan.FromSeconds(1.5);
    private static readonly TimeSpan UnreadableToastDuration = TimeSpan.FromSeconds(12);
    private static readonly TimeSpan FirstUpdateCheckDelay = TimeSpan.FromSeconds(15);
    private static readonly TimeSpan UpdateCheckInterval = TimeSpan.FromHours(12);

    private static readonly (HotkeyAction Action, int VirtualKey)[] PositionHotkeys =
    [
        (HotkeyAction.MoveUp, VirtualKeyUp),
        (HotkeyAction.MoveDown, VirtualKeyDown),
        (HotkeyAction.MoveLeft, VirtualKeyLeft),
        (HotkeyAction.MoveRight, VirtualKeyRight),
        (HotkeyAction.ResetPosition, VirtualKeyHome),
    ];

    private readonly ConfigStore store;
    private readonly MainViewModel viewModel;
    private readonly OverlayController overlay;
    private readonly HotkeyService hotkeys;
    private readonly TrayIcon tray;
    private readonly ToastService toasts = new();
    private readonly DispatcherTimer saveTimer;
    private readonly DispatcherTimer updateTimer;
    private readonly UpdateService updateService = new();
    private readonly UpdateViewModel updates;
    private readonly ConfigLoadResult loadResult;
    private readonly string? autostartRepairError;
    private readonly bool savingBlocked;
    private MainWindow? window;
    private bool saveFailureReported;

    public AppController(ConfigStore store)
    {
        this.store = store;
        loadResult = store.Load();
        savingBlocked = loadResult.Status == ConfigLoadStatus.Unreadable;
        Loc.Instance.SetLanguage(loadResult.Config.Language);

        if (updateService.IsInstalled)
        {
            autostartRepairError = RepairAutostartPath();
        }

        updates = new UpdateViewModel(updateService);
        viewModel = new MainViewModel(loadResult.Config, ReadAutostart(), updates);
        overlay = new OverlayController();
        hotkeys = new HotkeyService();
        tray = new TrayIcon();
        saveTimer = new DispatcherTimer(SaveDelay, DispatcherPriority.Background, (_, _) => Save(), Dispatcher.CurrentDispatcher);
        saveTimer.Stop();
        updateTimer = new DispatcherTimer(FirstUpdateCheckDelay, DispatcherPriority.Background, OnUpdateTimer, Dispatcher.CurrentDispatcher);
        updateTimer.Stop();
        toasts.MonitorDeviceName = viewModel.MonitorDeviceName;

        viewModel.Changed += OnViewModelChanged;
        hotkeys.Pressed += OnHotkeyPressed;
        overlay.StreamerModeUnavailable += (_, _) => toasts.Show(Loc.T("ToastStreamerUnavailable"));
        overlay.RenderFailed += (_, _) => toasts.Show(Loc.T("ToastRenderFailed"));
        overlay.GameDetected += OnGameDetected;
        updates.UpdateFound += (_, version) => toasts.Show(Loc.Format("ToastUpdateAvailable", version));
        updates.RestartRequested += (_, _) => Application.Current.Shutdown();
        tray.OpenSettingsRequested += (_, _) => ShowSettings();
        tray.ToggleRequested += (_, _) => viewModel.OverlayVisible = !viewModel.OverlayVisible;
        tray.PresetSelected += (_, index) => viewModel.SelectPreset(index);
        tray.ExitRequested += (_, _) => Application.Current.Shutdown();
    }

    public void Start(bool startInTray)
    {
        overlay.Apply(viewModel.ToOverlayOptions());
        ApplyHotkeys();
        UpdateTray();

        if (updates.IsSupported)
        {
            updateTimer.Start();
        }

        if (autostartRepairError is not null)
        {
            toasts.Show(Loc.Format("ToastAutostartFailed", autostartRepairError));
        }

        switch (loadResult.Status)
        {
            case ConfigLoadStatus.Created:
                Save();
                ShowSettings();
                return;
            case ConfigLoadStatus.RecoveredFromCorruptFile:
                toasts.Show(Loc.Format("ToastConfigRecovered", loadResult.CorruptFileBackupPath ?? string.Empty));
                break;
            case ConfigLoadStatus.Unreadable:
                toasts.Show(Loc.Format("ToastConfigUnreadable", loadResult.ErrorMessage ?? string.Empty), UnreadableToastDuration);
                break;
        }

        if (!startInTray && !viewModel.StartMinimized)
        {
            ShowSettings();
        }
    }

    public void ShowSettings()
    {
        if (window is null)
        {
            viewModel.RefreshMonitors();
            window = new MainWindow(viewModel);
            window.Closed += OnWindowClosed;
        }

        if (!window.IsVisible)
        {
            window.Show();
        }

        if (window.WindowState == WindowState.Minimized)
        {
            window.WindowState = WindowState.Normal;
        }

        window.Activate();
    }

    public void Dispose()
    {
        updateTimer.Stop();
        if (saveTimer.IsEnabled)
        {
            Save();
        }

        if (window is not null)
        {
            window.Closed -= OnWindowClosed;
            window.Close();
        }

        toasts.Dispose();
        tray.Dispose();
        hotkeys.Dispose();
        overlay.Dispose();
    }

    private static string? RepairAutostartPath()
    {
        try
        {
            AutostartService.PointToCurrentExecutable();
            return null;
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            return exception.Message;
        }
    }

    private async void OnUpdateTimer(object? sender, EventArgs e)
    {
        updateTimer.Interval = UpdateCheckInterval;
        if (viewModel.CheckForUpdates)
        {
            await updates.CheckAsync();
        }
    }

    private static bool ReadAutostart()
    {
        try
        {
            return AutostartService.IsEnabled();
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            return false;
        }
    }

    private void OnWindowClosed(object? sender, EventArgs e)
    {
        viewModel.IsCapturingHotkey = false;
        window = null;
        if (!viewModel.CloseToTray)
        {
            Application.Current.Shutdown();
        }
    }

    private void OnViewModelChanged(object? sender, ChangeKind kind)
    {
        if (kind.HasFlag(ChangeKind.Language))
        {
            Loc.Instance.SetLanguage(viewModel.Language);
        }

        if (kind.HasFlag(ChangeKind.Overlay))
        {
            overlay.Apply(viewModel.ToOverlayOptions());
            toasts.MonitorDeviceName = viewModel.MonitorDeviceName;
        }

        if ((kind & (ChangeKind.Hotkey | ChangeKind.Language)) != 0)
        {
            ApplyHotkeys();
        }

        if (kind.HasFlag(ChangeKind.Autostart))
        {
            ApplyAutostart();
        }

        if ((kind & (ChangeKind.Presets | ChangeKind.Hotkey | ChangeKind.Language | ChangeKind.Overlay)) != 0)
        {
            UpdateTray();
        }

        saveTimer.Stop();
        saveTimer.Start();
    }

    private void OnHotkeyPressed(object? sender, int id)
    {
        if (id >= PresetHotkeyIdBase)
        {
            ActivatePreset(id - PresetHotkeyIdBase);
            return;
        }

        switch ((HotkeyAction)id)
        {
            case HotkeyAction.ToggleOverlay:
                viewModel.OverlayVisible = !viewModel.OverlayVisible;
                break;
            case HotkeyAction.NextPreset:
                viewModel.SelectRelativePreset(1);
                ShowPresetToast();
                break;
            case HotkeyAction.PreviousPreset:
                viewModel.SelectRelativePreset(-1);
                ShowPresetToast();
                break;
            case HotkeyAction.MoveUp:
                viewModel.OffsetY--;
                break;
            case HotkeyAction.MoveDown:
                viewModel.OffsetY++;
                break;
            case HotkeyAction.MoveLeft:
                viewModel.OffsetX--;
                break;
            case HotkeyAction.MoveRight:
                viewModel.OffsetX++;
                break;
            case HotkeyAction.ResetPosition:
                viewModel.ResetOffset();
                break;
        }
    }

    private void OnGameDetected(object? sender, GameRule rule)
    {
        if (rule.PresetId is { } presetId)
        {
            ActivatePreset(viewModel.IndexOfPreset(presetId));
        }
    }

    private void ActivatePreset(int index)
    {
        if (index < 0 || index >= viewModel.Presets.Count || index == viewModel.SelectedPresetIndex)
        {
            return;
        }

        viewModel.SelectPreset(index);
        ShowPresetToast();
    }

    private void ShowPresetToast()
    {
        if (viewModel.ShowPresetNotifications)
        {
            toasts.Show(Loc.Format("ToastPresetActive", viewModel.SelectedPreset.Name), PresetToastDuration);
        }
    }

    private void ApplyHotkeys()
    {
        hotkeys.UnregisterAll();
        if (viewModel.IsCapturingHotkey)
        {
            return;
        }

        var entries = new List<(int Id, HotkeyBinding Binding, bool Repeat, Action<string> Report)>
        {
            ((int)HotkeyAction.ToggleOverlay, viewModel.ToggleHotkey, false, status => viewModel.ToggleHotkeyStatus = status),
            ((int)HotkeyAction.NextPreset, viewModel.NextPresetHotkey, false, status => viewModel.NextHotkeyStatus = status),
            ((int)HotkeyAction.PreviousPreset, viewModel.PreviousPresetHotkey, false, status => viewModel.PreviousHotkeyStatus = status),
        };

        var positionStatuses = new List<string>();
        if (viewModel.PositionHotkeysEnabled)
        {
            foreach (var (action, virtualKey) in PositionHotkeys)
            {
                var binding = new HotkeyBinding(PositionModifiers, virtualKey);
                entries.Add(((int)action, binding, action != HotkeyAction.ResetPosition, positionStatuses.Add));
            }
        }

        for (var i = 0; i < viewModel.Presets.Count; i++)
        {
            var preset = viewModel.Presets[i];
            entries.Add((PresetHotkeyIdBase + i, preset.Hotkey, false, status => preset.HotkeyStatus = status));
        }

        var duplicates = HotkeyConflicts.FindDuplicates(entries.Select(entry => entry.Binding).ToList());
        for (var i = 0; i < entries.Count; i++)
        {
            var (id, binding, repeat, report) = entries[i];
            report(duplicates.Contains(i)
                ? Loc.T("HotkeyDuplicate")
                : hotkeys.Register(id, binding, repeat) ? string.Empty : Loc.T("HotkeyTaken"));
        }

        viewModel.PositionHotkeysStatus = positionStatuses.FirstOrDefault(status => status.Length > 0) ?? string.Empty;
    }

    private void ApplyAutostart()
    {
        try
        {
            AutostartService.SetEnabled(viewModel.StartWithWindows);
        }
        catch (Exception exception) when (exception is UnauthorizedAccessException or System.Security.SecurityException or IOException)
        {
            viewModel.SetStartWithWindowsSilently(ReadAutostart());
            toasts.Show(Loc.Format("ToastAutostartFailed", exception.Message));
        }
    }

    private void UpdateTray() =>
        tray.Update(
            viewModel.OverlayVisible,
            viewModel.ToggleHotkey.IsNone ? string.Empty : viewModel.ToggleHotkeyText,
            viewModel.Presets.Select(item => item.Name).ToList(),
            viewModel.SelectedPresetIndex);

    private void Save()
    {
        saveTimer.Stop();
        if (savingBlocked)
        {
            return;
        }

        try
        {
            store.Save(viewModel.ToConfig());
            saveFailureReported = false;
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            if (!saveFailureReported)
            {
                saveFailureReported = true;
                toasts.Show(Loc.Format("ToastSaveFailed", exception.Message));
            }
        }
    }
}
