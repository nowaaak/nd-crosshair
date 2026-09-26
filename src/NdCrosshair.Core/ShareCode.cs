namespace NdCrosshair.Core;

public enum ShareCodeError
{
    None,
    Empty,
    UnknownPrefix,
    InvalidEncoding,
    InvalidLength,
    ChecksumMismatch,
    UnknownFlags,
}

public static class ShareCode
{
    public const string Prefix = "NDX3-";
    public const string Version2Prefix = "NDX2-";
    public const string LegacyPrefix = "NDX1-";

    private const int LegacyPayloadLength = 13;
    private const int Version2PayloadLength = 22;
    private const int PayloadLength = 24;
    private const int MaxEncodedLength = 64;

    private const byte FlagTop = 1 << 0;
    private const byte FlagBottom = 1 << 1;
    private const byte FlagLeft = 1 << 2;
    private const byte FlagRight = 1 << 3;
    private const byte FlagDot = 1 << 4;
    private const byte FlagOutline = 1 << 5;
    private const byte FlagRing = 1 << 6;
    private const byte FlagShadow = 1 << 7;
    private const byte LegacyKnownFlags = FlagTop | FlagBottom | FlagLeft | FlagRight | FlagDot | FlagOutline;

    private const byte ExtraFlagRoundDot = 1 << 0;
    private const byte KnownExtraFlags = ExtraFlagRoundDot;
    private const byte RemovedContrastMode = 2;
    private const int LegacyMaxRotation = 90;

    public static string Encode(CrosshairSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        var s = settings.Clamp();
        Span<byte> data = stackalloc byte[PayloadLength + 1];

        data[0] = (byte)(
            (s.ShowTop ? FlagTop : 0)
            | (s.ShowBottom ? FlagBottom : 0)
            | (s.ShowLeft ? FlagLeft : 0)
            | (s.ShowRight ? FlagRight : 0)
            | (s.ShowDot ? FlagDot : 0)
            | (s.ShowOutline ? FlagOutline : 0)
            | (s.ShowRing ? FlagRing : 0)
            | (s.ShowShadow ? FlagShadow : 0));
        data[1] = s.RoundDot ? ExtraFlagRoundDot : (byte)0;
        data[2] = (byte)s.LineLength;
        data[3] = (byte)s.LineThickness;
        data[4] = (byte)s.Gap;
        data[5] = (byte)s.DotSize;
        data[6] = (byte)s.OutlineThickness;
        data[7] = (byte)s.Opacity;
        data[8] = (byte)Math.Min(s.Rotation, LegacyMaxRotation);
        data[9] = (byte)s.RingRadius;
        data[10] = (byte)s.RingThickness;
        data[11] = (byte)s.ShadowSize;
        data[12] = (byte)s.ShadowOpacity;
        WriteColor(data[13..], s.Color);
        WriteColor(data[16..], s.OutlineColor);
        WriteColor(data[19..], s.ShadowColor);
        data[22] = (byte)s.ColorMode;
        data[23] = (byte)s.RainbowSpeed;
        data[PayloadLength] = Crc8(data[..PayloadLength]);

        return Prefix + Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static bool TryDecode(string? code, out CrosshairSettings settings, out ShareCodeError error)
    {
        settings = new CrosshairSettings();

        if (string.IsNullOrWhiteSpace(code))
        {
            error = ShareCodeError.Empty;
            return false;
        }

        var trimmed = code.Trim();
        int payloadLength;
        if (trimmed.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            payloadLength = PayloadLength;
        }
        else if (trimmed.StartsWith(Version2Prefix, StringComparison.OrdinalIgnoreCase))
        {
            payloadLength = Version2PayloadLength;
        }
        else if (trimmed.StartsWith(LegacyPrefix, StringComparison.OrdinalIgnoreCase))
        {
            payloadLength = LegacyPayloadLength;
        }
        else
        {
            error = ShareCodeError.UnknownPrefix;
            return false;
        }

        var body = trimmed[Prefix.Length..];
        if (body.Length == 0 || body.Length > MaxEncodedLength)
        {
            error = ShareCodeError.InvalidLength;
            return false;
        }

        var base64 = body.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');

        Span<byte> data = stackalloc byte[MaxEncodedLength];
        if (!Convert.TryFromBase64String(base64, data, out var written))
        {
            error = ShareCodeError.InvalidEncoding;
            return false;
        }

        if (written != payloadLength + 1)
        {
            error = ShareCodeError.InvalidLength;
            return false;
        }

        if (Crc8(data[..payloadLength]) != data[payloadLength])
        {
            error = ShareCodeError.ChecksumMismatch;
            return false;
        }

        return payloadLength != LegacyPayloadLength
            ? TryReadCurrent(data[..payloadLength], out settings, out error)
            : TryReadLegacy(data, out settings, out error);
    }

    internal static byte Crc8(ReadOnlySpan<byte> data)
    {
        byte crc = 0;
        foreach (var value in data)
        {
            crc ^= value;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 0x80) != 0 ? (byte)((crc << 1) ^ 0x07) : (byte)(crc << 1);
            }
        }

        return crc;
    }

    private static bool TryReadCurrent(ReadOnlySpan<byte> data, out CrosshairSettings settings, out ShareCodeError error)
    {
        settings = new CrosshairSettings();
        var flags = data[0];
        var extraFlags = data[1];
        if ((extraFlags & ~KnownExtraFlags) != 0)
        {
            error = ShareCodeError.UnknownFlags;
            return false;
        }

        var colorMode = CrosshairColorMode.Static;
        var rainbowSpeed = new CrosshairSettings().RainbowSpeed;
        if (data.Length == PayloadLength)
        {
            colorMode = data[22] == RemovedContrastMode ? CrosshairColorMode.Static : (CrosshairColorMode)data[22];
            rainbowSpeed = data[23];
            if (!Enum.IsDefined(colorMode))
            {
                error = ShareCodeError.UnknownFlags;
                return false;
            }
        }

        settings = new CrosshairSettings
        {
            ShowTop = (flags & FlagTop) != 0,
            ShowBottom = (flags & FlagBottom) != 0,
            ShowLeft = (flags & FlagLeft) != 0,
            ShowRight = (flags & FlagRight) != 0,
            ShowDot = (flags & FlagDot) != 0,
            ShowOutline = (flags & FlagOutline) != 0,
            ShowRing = (flags & FlagRing) != 0,
            ShowShadow = (flags & FlagShadow) != 0,
            RoundDot = (extraFlags & ExtraFlagRoundDot) != 0,
            LineLength = data[2],
            LineThickness = data[3],
            Gap = data[4],
            DotSize = data[5],
            OutlineThickness = data[6],
            Opacity = data[7],
            Rotation = Math.Min((int)data[8], LegacyMaxRotation),
            RingRadius = data[9],
            RingThickness = data[10],
            ShadowSize = data[11],
            ShadowOpacity = data[12],
            Color = ReadColor(data[13..]),
            OutlineColor = ReadColor(data[16..]),
            ShadowColor = ReadColor(data[19..]),
            ColorMode = colorMode,
            RainbowSpeed = rainbowSpeed,
        }.Clamp();

        error = ShareCodeError.None;
        return true;
    }

    private static bool TryReadLegacy(ReadOnlySpan<byte> data, out CrosshairSettings settings, out ShareCodeError error)
    {
        settings = new CrosshairSettings();
        var flags = data[0];
        if ((flags & ~LegacyKnownFlags) != 0)
        {
            error = ShareCodeError.UnknownFlags;
            return false;
        }

        settings = new CrosshairSettings
        {
            ShowTop = (flags & FlagTop) != 0,
            ShowBottom = (flags & FlagBottom) != 0,
            ShowLeft = (flags & FlagLeft) != 0,
            ShowRight = (flags & FlagRight) != 0,
            ShowDot = (flags & FlagDot) != 0,
            ShowOutline = (flags & FlagOutline) != 0,
            LineLength = data[1],
            LineThickness = data[2],
            Gap = data[3],
            DotSize = data[4],
            OutlineThickness = data[5],
            Opacity = data[6],
            Color = ReadColor(data[7..]),
            OutlineColor = ReadColor(data[10..]),
        }.Clamp();

        error = ShareCodeError.None;
        return true;
    }

    private static void WriteColor(Span<byte> target, CrosshairColor color)
    {
        target[0] = color.R;
        target[1] = color.G;
        target[2] = color.B;
    }

    private static CrosshairColor ReadColor(ReadOnlySpan<byte> source) => new(source[0], source[1], source[2]);
}
