using System.Diagnostics;
using System.Windows.Threading;
using NdCrosshair.App.Services;
using NdCrosshair.Core;

namespace NdCrosshair.App;

internal sealed record OverlayOptions(
    CrosshairSettings Settings,
    string? MonitorDeviceName,
    int OffsetX,
    int OffsetY,
    bool Visible,
    bool ShowOnlyOverGame,
    IReadOnlyList<GameRule> GameRules,
    AimButton AimHideButton,
    AimHideMode AimHideMode,
    bool StreamerMode);

internal sealed class OverlayController : IDisposable
{
    private static readonly TimeSpan RainbowInterval = TimeSpan.FromMilliseconds(33);
    private static readonly TimeSpan ForegroundInterval = TimeSpan.FromMilliseconds(250);
    private static readonly TimeSpan AimInterval = TimeSpan.FromMilliseconds(10);

    private readonly OverlayWindow window = new();
    private readonly DispatcherTimer rainbowTimer;
    private readonly DispatcherTimer foregroundTimer;
    private readonly DispatcherTimer aimTimer;
    private readonly Stopwatch clock = Stopwatch.StartNew();

    private OverlayOptions? options;
    private CrosshairSettings? rasterizedSettings;
    private CrosshairLayers? layers;
    private CrosshairColor? currentFill;
    private bool gameInForeground = true;
    private GameRule? detectedRule;
    private bool aimHidden;
    private bool aimButtonDown;
    private bool? streamerMode;

    public OverlayController()
    {
        var dispatcher = Dispatcher.CurrentDispatcher;
        rainbowTimer = new DispatcherTimer(RainbowInterval, DispatcherPriority.Render, (_, _) => OnRainbowTick(), dispatcher);
        foregroundTimer = new DispatcherTimer(ForegroundInterval, DispatcherPriority.Background, (_, _) => OnForegroundTick(), dispatcher);
        aimTimer = new DispatcherTimer(AimInterval, DispatcherPriority.Render, (_, _) => OnAimTick(), dispatcher);
        rainbowTimer.Stop();
        foregroundTimer.Stop();
        aimTimer.Stop();
        window.RenderFailed += (_, _) => RenderFailed?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? StreamerModeUnavailable;

    public event EventHandler? RenderFailed;

    public event EventHandler<GameRule>? GameDetected;

    public void Apply(OverlayOptions next)
    {
        var previous = options;
        options = next;
        var settings = next.Settings.Clamp();

        if (layers is null || rasterizedSettings != settings)
        {
            layers = CrosshairRenderer.Rasterize(settings);
            rasterizedSettings = settings;
            currentFill = null;
        }

        if (previous is null
            || previous.MonitorDeviceName != next.MonitorDeviceName
            || previous.OffsetX != next.OffsetX
            || previous.OffsetY != next.OffsetY)
        {
            window.SetPlacement(next.MonitorDeviceName, next.OffsetX, next.OffsetY);
        }

        if (streamerMode != next.StreamerMode)
        {
            streamerMode = next.StreamerMode;
            if (!window.SetExcludedFromCapture(next.StreamerMode) && next.StreamerMode)
            {
                StreamerModeUnavailable?.Invoke(this, EventArgs.Empty);
            }
        }

        if (previous is null || !previous.GameRules.SequenceEqual(next.GameRules))
        {
            detectedRule = null;
        }

        if (previous?.AimHideButton != next.AimHideButton || previous?.AimHideMode != next.AimHideMode)
        {
            ResetAim();
        }

        if (next.ShowOnlyOverGame)
        {
            gameInForeground = ForegroundWindow.IsGameOrSettings(ForegroundWindow.Get(), next.GameRules);
        }

        UpdateFill(force: true);
        UpdateVisibility();
    }

    public void Dispose()
    {
        rainbowTimer.Stop();
        foregroundTimer.Stop();
        aimTimer.Stop();
        window.Dispose();
    }

    private void UpdateVisibility()
    {
        if (options is null)
        {
            return;
        }

        var active = options.Visible && (!options.ShowOnlyOverGame || gameInForeground);
        var aimWatched = active && options.AimHideButton != AimButton.None;
        if (!aimWatched)
        {
            ResetAim();
        }

        var shown = active && !aimHidden;
        window.SetVisible(shown);

        var mode = rasterizedSettings?.ColorMode ?? CrosshairColorMode.Static;
        SetTimer(rainbowTimer, shown && mode == CrosshairColorMode.Rainbow);
        SetTimer(aimTimer, aimWatched);
        SetTimer(foregroundTimer, (options.Visible && options.ShowOnlyOverGame) || options.GameRules.Any(rule => rule.PresetId is not null));
    }

    private void UpdateFill(bool force)
    {
        if (layers is null || rasterizedSettings is null)
        {
            return;
        }

        var fill = rasterizedSettings.ColorMode switch
        {
            CrosshairColorMode.Rainbow => RainbowColor(rasterizedSettings),
            _ => rasterizedSettings.Color,
        };

        if (!force && fill == currentFill)
        {
            return;
        }

        currentFill = fill;
        window.SetImage(layers.Compose(fill));
    }

    private CrosshairColor RainbowColor(CrosshairSettings settings)
    {
        var secondsPerCycle = CrosshairSettings.MaxRainbowSpeed + 1 - settings.RainbowSpeed;
        var baseHue = ColorMath.ToHsv(settings.Color).Hue;
        var hue = baseHue + clock.Elapsed.TotalSeconds / secondsPerCycle * 360;
        return ColorMath.FromHsv(hue, 1, 1);
    }

    private void OnRainbowTick() => UpdateFill(force: false);

    private void OnForegroundTick()
    {
        if (options is null)
        {
            return;
        }

        var foreground = ForegroundWindow.Get();
        var rule = ForegroundWindow.FindRule(foreground, options.GameRules);
        if (rule is not null)
        {
            var isNewGame = rule != detectedRule;
            detectedRule = rule;
            if (isNewGame && rule.PresetId is not null)
            {
                GameDetected?.Invoke(this, rule);
            }
        }
        else if (foreground is not { IsOwnProcess: true })
        {
            detectedRule = null;
        }

        var matches = foreground is { IsOwnProcess: true } || rule is not null;
        if (options.ShowOnlyOverGame && matches != gameInForeground)
        {
            gameInForeground = matches;
            UpdateVisibility();
        }
    }

    private void OnAimTick()
    {
        if (options is null)
        {
            return;
        }

        var pressed = MouseButtonState.IsPressed(options.AimHideButton);
        var hidden = options.AimHideMode == AimHideMode.Hold
            ? pressed
            : pressed && !aimButtonDown ? !aimHidden : aimHidden;
        aimButtonDown = pressed;

        if (hidden != aimHidden)
        {
            aimHidden = hidden;
            UpdateVisibility();
        }
    }

    private void ResetAim()
    {
        aimHidden = false;
        aimButtonDown = false;
    }

    private static void SetTimer(DispatcherTimer timer, bool enabled)
    {
        if (enabled && !timer.IsEnabled)
        {
            timer.Start();
        }
        else if (!enabled && timer.IsEnabled)
        {
            timer.Stop();
        }
    }
}
