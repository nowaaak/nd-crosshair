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

    private readonly Dictionary<TextKey, CoverageMask?> textCache = [];

    public static WpfLayerContent Instance { get; } = new();

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

    private readonly record struct TextKey(string Text, string FontFamily, int FontSize, bool Bold, int Rotation, double Scale);
}
