namespace NdCrosshair.Core;

public static class ColorMath
{
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

    public static double DeltaE(CrosshairColor first, CrosshairColor second)
    {
        var a = ToLab(first);
        var b = ToLab(second);
        var dl = a.L - b.L;
        var da = a.A - b.A;
        var db = a.B - b.B;
        return Math.Sqrt(dl * dl + da * da + db * db);
    }

    private static (double L, double A, double B) ToLab(CrosshairColor color)
    {
        var r = Linearize(color.R);
        var g = Linearize(color.G);
        var b = Linearize(color.B);

        var x = (r * 0.4124564 + g * 0.3575761 + b * 0.1804375) / 0.95047;
        var y = r * 0.2126729 + g * 0.7151522 + b * 0.0721750;
        var z = (r * 0.0193339 + g * 0.1191920 + b * 0.9503041) / 1.08883;

        var fx = LabCurve(x);
        var fy = LabCurve(y);
        var fz = LabCurve(z);
        return (116 * fy - 16, 500 * (fx - fy), 200 * (fy - fz));
    }

    private static double Linearize(byte channel)
    {
        var value = channel / 255.0;
        return value <= 0.04045 ? value / 12.92 : Math.Pow((value + 0.055) / 1.055, 2.4);
    }

    private static double LabCurve(double value) =>
        value > 216.0 / 24389 ? Math.Cbrt(value) : (24389.0 / 27 * value + 16) / 116;

    private static byte ToByte(double value) => (byte)Math.Clamp(Math.Round(value * 255), 0, 255);
}
