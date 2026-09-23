using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using NdCrosshair.App.Localization;
using NdCrosshair.App.Native;
using Drawing = System.Drawing;
using Forms = System.Windows.Forms;

namespace NdCrosshair.App;

internal sealed class TrayIcon : IDisposable
{
    private const string AppName = "ND Crosshair";
    private const string SettingsGlyph = "";
    private const string ExitGlyph = "";
    private const string PresetGlyph = "";

    private readonly Forms.NotifyIcon notifyIcon;
    private readonly Drawing.Icon icon;
    private readonly ContextMenu menu;
    private readonly MenuItem settingsItem;
    private readonly MenuItem toggleItem;
    private readonly MenuItem presetsItem;
    private readonly MenuItem exitItem;

    public TrayIcon()
    {
        icon = LoadIcon();

        settingsItem = CreateItem(SettingsGlyph, () => OpenSettingsRequested?.Invoke(this, EventArgs.Empty));
        toggleItem = CreateItem(null, () => ToggleRequested?.Invoke(this, EventArgs.Empty));
        presetsItem = CreateItem(PresetGlyph, null);
        exitItem = CreateItem(ExitGlyph, () => ExitRequested?.Invoke(this, EventArgs.Empty));
        menu = new ContextMenu
        {
            Placement = PlacementMode.MousePoint,
            Items = { settingsItem, toggleItem, presetsItem, new Separator(), exitItem },
        };

        notifyIcon = new Forms.NotifyIcon
        {
            Icon = icon,
            Text = AppName,
            Visible = true,
        };
        notifyIcon.MouseUp += OnNotifyIconMouseUp;
        notifyIcon.DoubleClick += (_, _) => OpenSettingsRequested?.Invoke(this, EventArgs.Empty);
    }

    public event EventHandler? OpenSettingsRequested;

    public event EventHandler? ToggleRequested;

    public event EventHandler? ExitRequested;

    public event EventHandler<int>? PresetSelected;

    public void Update(bool overlayVisible, string hotkeyText, IReadOnlyList<string> presetNames, int activeIndex)
    {
        settingsItem.Header = Loc.T("TrayOpenSettings");
        toggleItem.Header = Loc.T("TrayShowOverlay");
        presetsItem.Header = Loc.T("TrayPreset");
        exitItem.Header = Loc.T("TrayExit");
        toggleItem.IsChecked = overlayVisible;
        toggleItem.InputGestureText = hotkeyText;

        presetsItem.Items.Clear();
        for (var i = 0; i < presetNames.Count; i++)
        {
            var index = i;
            var item = CreateItem(null, () => PresetSelected?.Invoke(this, index));
            item.Header = presetNames[i];
            item.IsChecked = i == activeIndex;
            presetsItem.Items.Add(item);
        }
    }

    public void Dispose()
    {
        menu.IsOpen = false;
        notifyIcon.Visible = false;
        notifyIcon.Dispose();
        icon.Dispose();
    }

    private static Drawing.Icon LoadIcon()
    {
        var resource = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/app.ico"))
            ?? throw new InvalidOperationException("Application icon resource is missing.");
        using var stream = resource.Stream;
        return new Drawing.Icon(stream, Forms.SystemInformation.SmallIconSize);
    }

    private static MenuItem CreateItem(string? glyph, Action? onClick)
    {
        var item = new MenuItem();
        if (glyph is not null)
        {
            item.Icon = new TextBlock { Text = glyph, Style = (Style)Application.Current.FindResource("MenuGlyph") };
        }

        if (onClick is not null)
        {
            item.Click += (_, _) => onClick();
        }

        return item;
    }

    private void OnNotifyIconMouseUp(object? sender, Forms.MouseEventArgs e)
    {
        if (e.Button != Forms.MouseButtons.Right)
        {
            return;
        }

        menu.IsOpen = true;
        if (PresentationSource.FromVisual(menu) is HwndSource source)
        {
            User32.SetForegroundWindow(source.Handle);
        }

        menu.Focus();
    }
}
