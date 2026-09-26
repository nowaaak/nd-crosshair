namespace NdCrosshair.Core;

public static class ColorMath
{
    public static CrosshairColor Rainbow(CrosshairColor baseColor, int speed, double seconds)
    {
        var secondsPerCycle = CrosshairSettings.MaxRainbowSpeed + 1 - Math.Clamp(speed, CrosshairSettings.MinRainbowSpeed, CrosshairSettings.MaxRainbowSpeed);
        return FromHsv(ToHsv(baseColor).Hue + seconds / secondsPerCycle * 360, 1, 1);
    }

    public static CrosshairColor FromHsv(double hue, double saturation, double value)
    {
        hue = ((hue % 360) + 360) % 360;
        saturation = Math.Clamp(saturation, 0, 1);
        value = Math.Clamp(value, 0, 1);

        var chroma = value * saturation;
        var x = chroma * (1 - Math.Abs(hue / 60 % 2 - 1));
        var m = value - chroma;

        var (r, g, b) = (int)(hue / 60) switch
        {
            0 => (chroma, x, 0.0),
            1 => (x, chroma, 0.0),
            2 => (0.0, chroma, x),
            3 => (0.0, x, chroma),
            4 => (x, 0.0, chroma),
            _ => (chroma, 0.0, x),
        };

        return new CrosshairColor(ToByte(r + m), ToByte(g + m), ToByte(b + m));
    }

    public static (double Hue, double Saturation, double Value) ToHsv(CrosshairColor color)
    {
        var r = color.R / 255.0;
        var g = color.G / 255.0;
        var b = color.B / 255.0;
        var max = Math.Max(r, Math.Max(g, b));
        var min = Math.Min(r, Math.Min(g, b));
        var delta = max - min;

        double hue;
        if (delta == 0)
        {
            hue = 0;
        }
        else if (max == r)
        {
            hue = 60 * ((g - b) / delta % 6);
        }
        else if (max == g)
        {
            hue = 60 * ((b - r) / delta + 2);
        }
        else
        {
            hue = 60 * ((r - g) / delta + 4);
        }

        if (hue < 0)
        {
            hue += 360;
        }

        var saturation = max == 0 ? 0 : delta / max;
        return (hue, saturation, max);
    }

    private static byte ToByte(double value) => (byte)Math.Clamp(Math.Round(value * 255), 0, 255);
}
