using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NdCrosshair.Core;

namespace NdCrosshair.App;

internal static class CrosshairBitmaps
{
    public static BitmapSource ToBitmapSource(CrosshairImage image)
    {
        var bitmap = new WriteableBitmap(image.Size, image.Size, 96, 96, PixelFormats.Pbgra32, null);
        bitmap.WritePixels(new Int32Rect(0, 0, image.Size, image.Size), image.Pixels, image.Stride, 0);
        bitmap.Freeze();
        return bitmap;
    }
}
