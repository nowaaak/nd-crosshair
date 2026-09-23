using NdCrosshair.App.Native;
using NdCrosshair.Core;

namespace NdCrosshair.App.Services;

internal sealed unsafe class ScreenSampler : IDisposable
{
    private const int Margin = 12;

    private DibSurface? surface;

    public CrosshairColor? SampleAround(ScreenRect overlay)
    {
        var width = overlay.Width + Margin * 2;
        var height = overlay.Height + Margin * 2;
        if (surface is null || surface.Width != width || surface.Height != height)
        {
            surface?.Dispose();
            surface = new DibSurface(width, height);
        }

        var screenDc = User32.GetDC(0);
        try
        {
            if (!Gdi32.BitBlt(surface.DeviceContext, 0, 0, width, height, screenDc, overlay.X - Margin, overlay.Y - Margin, Gdi32.SRCCOPY))
            {
                return null;
            }
        }
        finally
        {
            User32.ReleaseDC(0, screenDc);
        }

        long red = 0;
        long green = 0;
        long blue = 0;
        long count = 0;
        var innerRight = Margin + overlay.Width;
        var innerBottom = Margin + overlay.Height;

        for (var y = 0; y < height; y++)
        {
            var row = surface.Bits + y * surface.Stride;
            var insideRows = y >= Margin && y < innerBottom;
            for (var x = 0; x < width; x++)
            {
                if (insideRows && x >= Margin && x < innerRight)
                {
                    x = innerRight - 1;
                    continue;
                }

                var pixel = row + x * 4;
                blue += pixel[0];
                green += pixel[1];
                red += pixel[2];
                count++;
            }
        }

        return count == 0
            ? null
            : new CrosshairColor((byte)(red / count), (byte)(green / count), (byte)(blue / count));
    }

    public void Dispose()
    {
        surface?.Dispose();
        surface = null;
    }
}
