using System.ComponentModel;
using System.Runtime.InteropServices;

namespace NdCrosshair.App.Native;

internal sealed unsafe class DibSurface : IDisposable
{
    private readonly nint memoryDc;
    private readonly nint bitmap;
    private readonly nint previousBitmap;

    public DibSurface(int width, int height)
    {
        Width = width;
        Height = height;

        var header = new BITMAPINFOHEADER
        {
            BiSize = (uint)sizeof(BITMAPINFOHEADER),
            BiWidth = width,
            BiHeight = -height,
            BiPlanes = 1,
            BiBitCount = 32,
            BiCompression = Gdi32.BI_RGB,
        };

        var screenDc = User32.GetDC(0);
        try
        {
            memoryDc = Gdi32.CreateCompatibleDC(screenDc);
            void* bits;
            bitmap = Gdi32.CreateDIBSection(screenDc, &header, Gdi32.DIB_RGB_COLORS, &bits, 0, 0);
            if (memoryDc == 0 || bitmap == 0)
            {
                var error = Marshal.GetLastPInvokeError();
                ReleaseHandles();
                throw new Win32Exception(error);
            }

            Bits = (byte*)bits;
            previousBitmap = Gdi32.SelectObject(memoryDc, bitmap);
        }
        finally
        {
            User32.ReleaseDC(0, screenDc);
        }
    }

    public int Width { get; }

    public int Height { get; }

    public nint DeviceContext => memoryDc;

    public byte* Bits { get; }

    public int Stride => Width * 4;

    public void Dispose()
    {
        Gdi32.SelectObject(memoryDc, previousBitmap);
        ReleaseHandles();
    }

    private void ReleaseHandles()
    {
        if (bitmap != 0)
        {
            Gdi32.DeleteObject(bitmap);
        }

        if (memoryDc != 0)
        {
            Gdi32.DeleteDC(memoryDc);
        }
    }
}
