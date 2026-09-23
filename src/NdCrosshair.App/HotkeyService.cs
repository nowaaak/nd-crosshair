using System.Windows.Interop;
using NdCrosshair.App.Native;
using NdCrosshair.Core;

namespace NdCrosshair.App;

internal enum HotkeyAction
{
    ToggleOverlay = 1,
    NextPreset = 2,
    PreviousPreset = 3,
}

internal sealed class HotkeyService : IDisposable
{
    private const int IdBase = 0x4E40;

    private readonly HwndSource source;
    private readonly HashSet<HotkeyAction> registered = [];

    public HotkeyService()
    {
        source = new HwndSource(new HwndSourceParameters("NdCrosshairHotkey")
        {
            ParentWindow = User32.HWND_MESSAGE,
            WindowStyle = 0,
        });
        source.AddHook(OnMessage);
    }

    public event EventHandler<HotkeyAction>? Pressed;

    public bool Register(HotkeyAction action, HotkeyBinding binding)
    {
        Unregister(action);
        if (binding.IsNone)
        {
            return true;
        }

        var success = User32.RegisterHotKey(
            source.Handle,
            IdBase + (int)action,
            (uint)binding.Modifiers | User32.MOD_NOREPEAT,
            (uint)binding.VirtualKey);
        if (success)
        {
            registered.Add(action);
        }

        return success;
    }

    public void UnregisterAll()
    {
        foreach (var action in registered.ToList())
        {
            Unregister(action);
        }
    }

    public void Dispose()
    {
        UnregisterAll();
        source.RemoveHook(OnMessage);
        source.Dispose();
    }

    private void Unregister(HotkeyAction action)
    {
        if (registered.Remove(action))
        {
            User32.UnregisterHotKey(source.Handle, IdBase + (int)action);
        }
    }

    private nint OnMessage(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message == User32.WM_HOTKEY)
        {
            var action = (HotkeyAction)((int)wParam - IdBase);
            if (Enum.IsDefined(action))
            {
                handled = true;
                Pressed?.Invoke(this, action);
            }
        }

        return 0;
    }
}
