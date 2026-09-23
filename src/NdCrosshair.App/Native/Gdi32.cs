using System.Runtime.InteropServices;

namespace NdCrosshair.App.Native;

[StructLayout(LayoutKind.Sequential)]
internal struct BITMAPINFOHEADER
{
    public uint BiSize;
    public int BiWidth;
    public int BiHeight;
    public ushort BiPlanes;
    public ushort BiBitCount;
    public uint BiCompression;
    public uint BiSizeImage;
    public int BiXPelsPerMeter;
    public int BiYPelsPerMeter;
    public uint BiClrUsed;
    public uint BiClrImportant;
}

internal static unsafe partial class Gdi32
{
    public const uint BI_RGB = 0;
    public const uint DIB_RGB_COLORS = 0;

    [LibraryImport("gdi32.dll", SetLastError = true)]
    public static partial nint CreateCompatibleDC(nint hdc);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteDC(nint hdc);

    [LibraryImport("gdi32.dll")]
    public static partial nint SelectObject(nint hdc, nint h);

    [LibraryImport("gdi32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool DeleteObject(nint ho);

    public const uint SRCCOPY = 0x00CC0020;

    [LibraryImport("gdi32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool BitBlt(nint hdc, int x, int y, int cx, int cy, nint hdcSrc, int x1, int y1, uint rop);

    [LibraryImport("gdi32.dll", SetLastError = true)]
    public static partial nint CreateDIBSection(
        nint hdc,
        BITMAPINFOHEADER* pbmi,
        uint usage,
        void** ppvBits,
        nint hSection,
        uint offset);
}

internal static partial class Kernel32
{
    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW")]
    public static partial nint GetModuleHandle(nint lpModuleName);
}
