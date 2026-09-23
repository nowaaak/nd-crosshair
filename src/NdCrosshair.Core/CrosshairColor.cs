using System.Globalization;

namespace NdCrosshair.Core;

public readonly record struct CrosshairColor(byte R, byte G, byte B)
{
    public static CrosshairColor Black => new(0, 0, 0);

    public static CrosshairColor Green => new(0, 255, 0);

    public string ToHex() => $"#{R:X2}{G:X2}{B:X2}";

    public static bool TryParseHex(string? text, out CrosshairColor color)
    {
        color = default;
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var span = text.AsSpan().Trim();
        if (span.StartsWith("#"))
        {
            span = span[1..];
        }

        if (span.Length != 6 || !uint.TryParse(span, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var value))
        {
            return false;
        }

        color = new CrosshairColor((byte)(value >> 16), (byte)(value >> 8), (byte)value);
        return true;
    }
}
