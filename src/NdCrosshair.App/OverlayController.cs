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
    bool StreamerMode);

internal sealed class OverlayController : IDisposable
{
    private static readonly TimeSpan RainbowInterval = TimeSpan.FromMilliseconds(33);
    private static readonly TimeSpan AdaptiveInterval = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan ForegroundInterval = TimeSpan.FromMilliseconds(250);

    private readonly OverlayWindow window = new();
    private readonly ScreenSampler sampler = new();
    private readonly DispatcherTimer rainbowTimer;
    private readonly DispatcherTimer adaptiveTimer;
    private readonly DispatcherTimer foregroundTimer;
    private readonly Stopwatch clock = Stopwatch.StartNew();

    private OverlayOptions? options;
    private CrosshairSettings? rasterizedSettings;
    private CrosshairLayers? layers;
    private AdaptiveColorSelector? adaptive;
    private CrosshairColor? currentFill;
    private bool gameInForeground = true;
    private GameRule? detectedRule;
    private bool? streamerMode;

    public OverlayController()
    {
        var dispatcher = Dispatcher.CurrentDispatcher;
        rainbowTimer = new DispatcherTimer(RainbowInterval, DispatcherPriority.Render, (_, _) => OnRainbowTick(), dispatcher);
        adaptiveTimer = new DispatcherTimer(AdaptiveInterval, DispatcherPriority.Background, (_, _) => OnAdaptiveTick(), dispatcher);
        foregroundTimer = new DispatcherTimer(ForegroundInterval, DispatcherPriority.Background, (_, _) => OnForegroundTick(), dispatcher);
        rainbowTimer.Stop();
        adaptiveTimer.Stop();
        foregroundTimer.Stop();
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
            if (rasterizedSettings?.Color != settings.Color || rasterizedSettings?.ColorMode != settings.ColorMode)
            {
                adaptive = new AdaptiveColorSelector(settings.Color);
            }

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
        adaptiveTimer.Stop();
        foregroundTimer.Stop();
        window.Dispose();
        sampler.Dispose();
    }

    private void UpdateVisibility()
    {
        if (options is null)
        {
            return;
        }

        var shown = options.Visible && (!options.ShowOnlyOverGame || gameInForeground);
        window.SetVisible(shown);

        var mode = rasterizedSettings?.ColorMode ?? CrosshairColorMode.Static;
        SetTimer(rainbowTimer, shown && mode == CrosshairColorMode.Rainbow);
        SetTimer(adaptiveTimer, shown && mode == CrosshairColorMode.Adaptive);
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
            CrosshairColorMode.Adaptive => adaptive?.Current ?? rasterizedSettings.Color,
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

    private void OnAdaptiveTick()
    {
        if (adaptive is null || window.Bounds is not { } bounds)
        {
            return;
        }

        if (sampler.SampleAround(bounds) is { } background)
        {
            adaptive.Update(background);
            UpdateFill(force: false);
        }
    }

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
