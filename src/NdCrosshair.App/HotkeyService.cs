using System.Windows.Interop;
using NdCrosshair.App.Native;
using NdCrosshair.Core;

namespace NdCrosshair.App;

internal enum HotkeyAction
{
    ToggleOverlay = 1,
    NextPreset = 2,
    PreviousPreset = 3,
    MoveUp = 4,
    MoveDown = 5,
    MoveLeft = 6,
    MoveRight = 7,
    ResetPosition = 8,
}

internal sealed class HotkeyService : IDisposable
{
    private const int IdBase = 0x4E40;

    private readonly HwndSource source;
    private readonly HashSet<int> registered = [];

    public HotkeyService()
    {
        source = new HwndSource(new HwndSourceParameters("NdCrosshairHotkey")
        {
            ParentWindow = User32.HWND_MESSAGE,
            WindowStyle = 0,
        });
        source.AddHook(OnMessage);
    }

    public event EventHandler<int>? Pressed;

    public bool Register(int id, HotkeyBinding binding, bool allowRepeat = false)
    {
        Unregister(id);
        if (binding.IsNone)
        {
            return true;
        }

        var success = User32.RegisterHotKey(
            source.Handle,
            IdBase + id,
            (uint)binding.Modifiers | (allowRepeat ? 0 : User32.MOD_NOREPEAT),
            (uint)binding.VirtualKey);
        if (success)
        {
            registered.Add(id);
        }

        return success;
    }

    public void UnregisterAll()
    {
        foreach (var id in registered.ToList())
        {
            Unregister(id);
        }
    }

    public void Dispose()
    {
        UnregisterAll();
        source.RemoveHook(OnMessage);
        source.Dispose();
    }

    private void Unregister(int id)
    {
        if (registered.Remove(id))
        {
            User32.UnregisterHotKey(source.Handle, IdBase + id);
        }
    }

    private nint OnMessage(nint hwnd, int message, nint wParam, nint lParam, ref bool handled)
    {
        if (message == User32.WM_HOTKEY)
        {
            var id = (int)wParam - IdBase;
            if (registered.Contains(id))
            {
                handled = true;
                Pressed?.Invoke(this, id);
            }
        }

        return 0;
    }
}
