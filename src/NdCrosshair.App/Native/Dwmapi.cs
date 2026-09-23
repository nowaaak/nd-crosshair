using System.Runtime.InteropServices;

namespace NdCrosshair.App.Native;

internal static unsafe partial class Dwmapi
{
    public const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
    public const int DWMWA_BORDER_COLOR = 34;
    public const int DWMWA_CAPTION_COLOR = 35;

    [LibraryImport("dwmapi.dll")]
    public static partial int DwmSetWindowAttribute(nint hwnd, int attribute, void* value, int size);

    public static void SetInt(nint hwnd, int attribute, int value) =>
        DwmSetWindowAttribute(hwnd, attribute, &value, sizeof(int));
}
