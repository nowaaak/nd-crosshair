using System.IO;
using System.Windows;
using System.Windows.Threading;
using NdCrosshair.App.Localization;
using NdCrosshair.App.Services;
using NdCrosshair.Core;

namespace NdCrosshair.App;

internal sealed class AppController : IDisposable
{
    private static readonly TimeSpan SaveDelay = TimeSpan.FromMilliseconds(500);

    private readonly ConfigStore store;
    private readonly MainViewModel viewModel;
    private readonly OverlayController overlay;
    private readonly HotkeyService hotkeys;
    private readonly TrayIcon tray;
    private readonly ToastService toasts = new();
    private readonly DispatcherTimer saveTimer;
    private readonly ConfigLoadResult loadResult;
    private MainWindow? window;
    private bool saveFailureReported;

    public AppController(ConfigStore store)
    {
        this.store = store;
        loadResult = store.Load();
        Loc.Instance.SetLanguage(loadResult.Config.Language);

        viewModel = new MainViewModel(loadResult.Config, ReadAutostart());
        overlay = new OverlayController();
        hotkeys = new HotkeyService();
        tray = new TrayIcon();
        saveTimer = new DispatcherTimer(SaveDelay, DispatcherPriority.Background, (_, _) => Save(), Dispatcher.CurrentDispatcher);
        saveTimer.Stop();

        viewModel.Changed += OnViewModelChanged;
        hotkeys.Pressed += OnHotkeyPressed;
        overlay.StreamerModeUnavailable += (_, _) => toasts.Show(Loc.T("ToastStreamerUnavailable"));
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

        switch (loadResult.Status)
        {
            case ConfigLoadStatus.Created:
                Save();
                ShowSettings();
                return;
            case ConfigLoadStatus.RecoveredFromCorruptFile:
                toasts.Show(Loc.Format("ToastConfigRecovered", loadResult.CorruptFileBackupPath ?? string.Empty));
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
        }

        if (kind.HasFlag(ChangeKind.Hotkey))
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

    private void OnHotkeyPressed(object? sender, HotkeyAction action)
    {
        switch (action)
        {
            case HotkeyAction.ToggleOverlay:
                viewModel.OverlayVisible = !viewModel.OverlayVisible;
                break;
            case HotkeyAction.NextPreset:
            case HotkeyAction.PreviousPreset:
                viewModel.SelectRelativePreset(action == HotkeyAction.NextPreset ? 1 : -1);
                if (viewModel.ShowPresetNotifications)
                {
                    toasts.Show(Loc.Format("ToastPresetActive", viewModel.SelectedPreset.Name), TimeSpan.FromSeconds(1.5));
                }

                break;
        }
    }

    private void ApplyHotkeys()
    {
        if (viewModel.IsCapturingHotkey)
        {
            hotkeys.UnregisterAll();
            return;
        }

        viewModel.SetHotkeyStatus(HotkeyAction.ToggleOverlay, hotkeys.Register(HotkeyAction.ToggleOverlay, viewModel.ToggleHotkey));
        viewModel.SetHotkeyStatus(HotkeyAction.NextPreset, hotkeys.Register(HotkeyAction.NextPreset, viewModel.NextPresetHotkey));
        viewModel.SetHotkeyStatus(HotkeyAction.PreviousPreset, hotkeys.Register(HotkeyAction.PreviousPreset, viewModel.PreviousPresetHotkey));
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
