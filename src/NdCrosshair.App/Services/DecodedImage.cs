using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace NdCrosshair.App.Services;

internal sealed class DecodedImage
{
    private const int DisposeToBackground = 2;
    private const int DisposeToPrevious = 3;
    private static readonly TimeSpan DefaultDelay = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan MinimumDelay = TimeSpan.FromMilliseconds(20);

    private DecodedImage(IReadOnlyList<BitmapSource> frames, IReadOnlyList<TimeSpan> delays)
    {
        Frames = frames;
        Delays = delays;
        Duration = delays.Aggregate(TimeSpan.Zero, (sum, delay) => sum + delay);
    }

    public IReadOnlyList<BitmapSource> Frames { get; }

    public IReadOnlyList<TimeSpan> Delays { get; }

    public TimeSpan Duration { get; }

    public bool IsAnimated => Frames.Count > 1 && Duration > TimeSpan.Zero;

    public int FrameAt(TimeSpan time)
    {
        if (!IsAnimated)
        {
            return 0;
        }

        var position = TimeSpan.FromTicks(time.Ticks % Duration.Ticks);
        for (var i = 0; i < Delays.Count; i++)
        {
            if (position < Delays[i])
            {
                return i;
            }

            position -= Delays[i];
        }

        return Frames.Count - 1;
    }

    public static DecodedImage? Load(string path)
    {
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.Length > ImageStore.MaxFileBytes)
            {
                return null;
            }

            var bytes = File.ReadAllBytes(path);
            if (!ImageStore.IsValidImage(bytes))
            {
                return null;
            }

            using var stream = new MemoryStream(bytes);
            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            return decoder is GifBitmapDecoder && decoder.Frames.Count > 1
                ? ComposeGif(decoder)
                : new DecodedImage([Freeze(ToPbgra(decoder.Frames[0]))], [TimeSpan.Zero]);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or NotSupportedException
            or FileFormatException or ArgumentException or InvalidOperationException or OverflowException or COMException)
        {
            return null;
        }
    }

    private static DecodedImage ComposeGif(BitmapDecoder decoder)
    {
        var first = decoder.Frames[0];
        var width = Query<ushort?>(decoder.Metadata, "/logscrdesc/Width") ?? (ushort)first.PixelWidth;
        var height = Query<ushort?>(decoder.Metadata, "/logscrdesc/Height") ?? (ushort)first.PixelHeight;
        width = (ushort)Math.Clamp((int)width, 1, ImageStore.MaxPixels);
        height = (ushort)Math.Clamp((int)height, 1, ImageStore.MaxPixels);

        var stride = width * 4;
        var canvas = new byte[stride * height];
        var frames = new List<BitmapSource>();
        var delays = new List<TimeSpan>();

        foreach (var frame in decoder.Frames)
        {
            var metadata = frame.Metadata as BitmapMetadata;
            var left = Query<ushort?>(metadata, "/imgdesc/Left") ?? 0;
            var top = Query<ushort?>(metadata, "/imgdesc/Top") ?? 0;
            var delay = TimeSpan.FromMilliseconds((Query<ushort?>(metadata, "/grctlext/Delay") ?? 0) * 10);
            var disposal = Query<byte?>(metadata, "/grctlext/Disposal") ?? 0;
            var previous = disposal == DisposeToPrevious ? (byte[])canvas.Clone() : null;

            var source = ToPbgra(frame);
            var frameStride = source.PixelWidth * 4;
            var pixels = new byte[frameStride * source.PixelHeight];
            source.CopyPixels(pixels, frameStride, 0);
            BlendOver(canvas, width, height, pixels, source.PixelWidth, source.PixelHeight, left, top);

            frames.Add(Freeze(BitmapSource.Create(width, height, 96, 96, PixelFormats.Pbgra32, null, (byte[])canvas.Clone(), stride)));
            delays.Add(delay < MinimumDelay ? DefaultDelay : delay);

            if (disposal == DisposeToBackground)
            {
                Clear(canvas, width, height, left, top, source.PixelWidth, source.PixelHeight);
            }
            else if (previous is not null)
            {
                canvas = previous;
            }
        }

        return new DecodedImage(frames, delays);
    }

    private static void BlendOver(byte[] canvas, int width, int height, byte[] pixels, int frameWidth, int frameHeight, int left, int top)
    {
        for (var y = 0; y < frameHeight; y++)
        {
            var cy = top + y;
            if (cy < 0 || cy >= height)
            {
                continue;
            }

            for (var x = 0; x < frameWidth; x++)
            {
                var cx = left + x;
                if (cx < 0 || cx >= width)
                {
                    continue;
                }

                var source = (y * frameWidth + x) * 4;
                var alpha = pixels[source + 3];
                if (alpha == 0)
                {
                    continue;
                }

                var target = (cy * width + cx) * 4;
                var keep = 255 - alpha;
                for (var channel = 0; channel < 4; channel++)
                {
                    canvas[target + channel] = (byte)(pixels[source + channel] + canvas[target + channel] * keep / 255);
                }
            }
        }
    }

    private static void Clear(byte[] canvas, int width, int height, int left, int top, int frameWidth, int frameHeight)
    {
        for (var y = Math.Max(0, top); y < Math.Min(height, top + frameHeight); y++)
        {
            var start = (y * width + Math.Max(0, left)) * 4;
            var length = (Math.Min(width, left + frameWidth) - Math.Max(0, left)) * 4;
            if (length > 0)
            {
                Array.Clear(canvas, start, length);
            }
        }
    }

    private static BitmapSource ToPbgra(BitmapSource source) =>
        source.Format == PixelFormats.Pbgra32 ? source : new FormatConvertedBitmap(source, PixelFormats.Pbgra32, null, 0);

    private static BitmapSource Freeze(BitmapSource source)
    {
        if (source.CanFreeze)
        {
            source.Freeze();
        }

        return source;
    }

    private static T? Query<T>(ImageMetadata? metadata, string query)
    {
        try
        {
            return metadata is BitmapMetadata bitmapMetadata && bitmapMetadata.GetQuery(query) is { } value
                ? (T)Convert.ChangeType(value, Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T), System.Globalization.CultureInfo.InvariantCulture)
                : default;
        }
        catch (Exception exception) when (exception is NotSupportedException or InvalidOperationException or ArgumentException
            or InvalidCastException or FormatException or OverflowException or COMException)
        {
            return default;
        }
    }
}
