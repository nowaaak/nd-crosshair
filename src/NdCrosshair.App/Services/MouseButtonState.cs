using NdCrosshair.App.Native;
using NdCrosshair.Core;

namespace NdCrosshair.App.Services;

internal static class MouseButtonState
{
    private const int PressedMask = 0x8000;

    public static bool IsPressed(AimButton button)
    {
        var virtualKey = button switch
        {
            AimButton.Right => User32.GetSystemMetrics(User32.SM_SWAPBUTTON) != 0 ? User32.VK_LBUTTON : User32.VK_RBUTTON,
            AimButton.Middle => User32.VK_MBUTTON,
            AimButton.Back => User32.VK_XBUTTON1,
            AimButton.Forward => User32.VK_XBUTTON2,
            _ => 0,
        };

        return virtualKey != 0 && (User32.GetAsyncKeyState(virtualKey) & PressedMask) != 0;
    }
}
