using System.Globalization;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using NdCrosshair.Core;

namespace NdCrosshair.App.Services;

internal sealed class WpfLayerContent : ILayerContentProvider
{
    private const int MaxCachedTexts = 64;
    private const string FallbackFonts = ", Segoe UI, Segoe UI Emoji, Segoe UI Symbol";

    private const int MaxCachedFrames = 512;

    private readonly Dictionary<TextKey, CoverageMask?> textCache = [];
    private readonly Dictionary<string, DecodedImage?> images = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<FrameKey, ImageFrame> frameCache = [];

    public static WpfLayerContent Instance { get; } = new();

    public bool IsAnimated(ImageLayer layer) => Decode(layer.FileName)?.IsAnimated == true;

    public ImageFrame? RenderImage(ImageLayer layer, double scale, TimeSpan? time)
    {
        if (Decode(layer.FileName) is not { } image)
        {
            return null;
        }

        var index = time is { } elapsed ? image.FrameAt(elapsed) : 0;
        var key = new FrameKey(layer.FileName, layer.Width, layer.Rotation, scale, index);
        if (frameCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        if (frameCache.Count >= MaxCachedFrames)
        {
            frameCache.Clear();
        }

        var frame = RasterizeImage(image.Frames[index], layer, scale);
        frameCache[key] = frame;
        return frame;
    }

    private DecodedImage? Decode(string fileName)
    {
        if (!images.TryGetValue(fileName, out var image))
        {
            image = ImageStore.PathFor(fileName) is { } path ? DecodedImage.Load(path) : null;
            images[fileName] = image;
        }

        return image;
    }

    private static ImageFrame RasterizeImage(BitmapSource source, ImageLayer layer, double scale)
    {
        var width = Math.Max(1, (int)Math.Round(layer.Width * scale));
        var height = Math.Max(1, (int)Math.Round(width * (double)source.PixelHeight / source.PixelWidth));
        var rotation = new RotateTransform(layer.Rotation, width / 2.0, height / 2.0);
        var bounds = rotation.TransformBounds(new Rect(0, 0, width, height));
        var targetWidth = Math.Max(1, (int)Math.Ceiling(bounds.Width));
        var targetHeight = Math.Max(1, (int)Math.Ceiling(bounds.Height));

        var visual = new DrawingVisual();
        RenderOptions.SetBitmapScalingMode(visual, BitmapScalingMode.HighQuality);
        using (var context = visual.RenderOpen())
        {
            context.PushTransform(new TranslateTransform((targetWidth - bounds.Width) / 2 - bounds.X, (targetHeight - bounds.Height) / 2 - bounds.Y));
            context.PushTransform(rotation);
            context.DrawImage(source, new Rect(0, 0, width, height));
        }

        var bitmap = new RenderTargetBitmap(targetWidth, targetHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var pixels = new byte[targetWidth * targetHeight * 4];
        bitmap.CopyPixels(pixels, targetWidth * 4, 0);
        return new ImageFrame(targetWidth, targetHeight, pixels);
    }

    public CoverageMask? RenderText(TextLayer layer, double scale)
    {
        var key = new TextKey(layer.Text, layer.FontFamily, layer.FontSize, layer.Bold, layer.Rotation, scale);
        if (textCache.TryGetValue(key, out var cached))
        {
            return cached;
        }

        if (textCache.Count >= MaxCachedTexts)
        {
            textCache.Clear();
        }

        var mask = string.IsNullOrWhiteSpace(layer.Text) ? null : RasterizeText(layer, scale);
        textCache[key] = mask;
        return mask;
    }

    private static CoverageMask? RasterizeText(TextLayer layer, double scale)
    {
        var typeface = new Typeface(
            new FontFamily(layer.FontFamily + FallbackFonts),
            FontStyles.Normal,
            layer.Bold ? FontWeights.Bold : FontWeights.Normal,
            FontStretches.Normal);
        var text = new FormattedText(
            layer.Text,
            CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight,
            typeface,
            layer.FontSize * scale,
            Brushes.White,
            1.0);

        var geometry = text.BuildGeometry(new Point(0, 0));
        if (geometry.Bounds.IsEmpty)
        {
            return null;
        }

        var center = new Point(geometry.Bounds.X + geometry.Bounds.Width / 2, geometry.Bounds.Y + geometry.Bounds.Height / 2);
        var rotated = geometry.Clone();
        rotated.Transform = new RotateTransform(layer.Rotation, center.X, center.Y);
        var bounds = rotated.Bounds;
        var width = (int)Math.Ceiling(bounds.Width) + 2;
        var height = (int)Math.Ceiling(bounds.Height) + 2;

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            context.PushTransform(new TranslateTransform(1 - bounds.X, 1 - bounds.Y));
            context.DrawGeometry(Brushes.White, null, rotated);
        }

        var bitmap = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var pixels = new byte[width * height * 4];
        bitmap.CopyPixels(pixels, width * 4, 0);

        var coverage = new double[width * height];
        for (var i = 0; i < coverage.Length; i++)
        {
            coverage[i] = pixels[i * 4 + 3] / 255.0;
        }

        return new CoverageMask(width, height, coverage);
    }

    private readonly record struct FrameKey(string FileName, int Width, int Rotation, double Scale, int Frame);

    private readonly record struct TextKey(string Text, string FontFamily, int FontSize, bool Bold, int Rotation, double Scale);
}
