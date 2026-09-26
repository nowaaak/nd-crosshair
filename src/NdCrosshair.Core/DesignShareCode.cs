using System.IO.Compression;
using System.Text.Json;

namespace NdCrosshair.Core;

public static class DesignShareCode
{
    public const string Prefix = "NDX4-";

    private const int MaxEncodedLength = 8192;
    private const int MaxJsonBytes = 64 * 1024;
    private const int MaxClassicRotation = 90;

    public static string Encode(CrosshairDesign design)
    {
        ArgumentNullException.ThrowIfNull(design);

        var normalized = (design with { Layers = design.Layers.Where(layer => layer is not ImageLayer).ToList() }).Normalize();
        if (normalized.TryGetClassicOnly(out var classic) && FitsClassicCode(classic))
        {
            return ShareCode.Encode(classic);
        }

        var json = JsonSerializer.SerializeToUtf8Bytes(normalized, JsonDefaults.Compact);
        using var buffer = new MemoryStream();
        using (var deflate = new DeflateStream(buffer, CompressionLevel.SmallestSize, leaveOpen: true))
        {
            deflate.Write(json);
        }

        var compressed = buffer.ToArray();
        var payload = new byte[compressed.Length + 1];
        compressed.CopyTo(payload, 0);
        payload[^1] = ShareCode.Crc8(compressed);
        return Prefix + Convert.ToBase64String(payload).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    public static bool OmitsLayers(CrosshairDesign design) => design.Layers.Any(layer => layer is ImageLayer);

    public static bool TryDecode(string? code, out CrosshairDesign design, out ShareCodeError error)
    {
        design = new CrosshairDesign();
        var trimmed = code?.Trim() ?? string.Empty;
        if (!trimmed.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
            if (!ShareCode.TryDecode(trimmed, out var settings, out error))
            {
                return false;
            }

            design = CrosshairDesign.FromClassic(settings);
            return true;
        }

        var body = trimmed[Prefix.Length..];
        if (body.Length == 0 || body.Length > MaxEncodedLength)
        {
            error = ShareCodeError.InvalidLength;
            return false;
        }

        var base64 = body.Replace('-', '+').Replace('_', '/');
        base64 = base64.PadRight(base64.Length + (4 - base64.Length % 4) % 4, '=');
        byte[] payload;
        try
        {
            payload = Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            error = ShareCodeError.InvalidEncoding;
            return false;
        }

        if (payload.Length < 2)
        {
            error = ShareCodeError.InvalidLength;
            return false;
        }

        var compressed = payload.AsSpan(0, payload.Length - 1);
        if (ShareCode.Crc8(compressed) != payload[^1])
        {
            error = ShareCodeError.ChecksumMismatch;
            return false;
        }

        if (!TryInflate(compressed.ToArray(), out var json))
        {
            error = ShareCodeError.InvalidLength;
            return false;
        }

        try
        {
            design = (JsonSerializer.Deserialize<CrosshairDesign>(json, JsonDefaults.Compact)
                ?? throw new JsonException("Empty design.")).Normalize();
        }
        catch (Exception exception) when (exception is JsonException or NotSupportedException or InvalidDataException)
        {
            design = new CrosshairDesign();
            error = ShareCodeError.InvalidEncoding;
            return false;
        }

        error = ShareCodeError.None;
        return true;
    }

    private static bool FitsClassicCode(CrosshairSettings settings) =>
        settings.Rotation <= MaxClassicRotation && settings.ArmCount == CrosshairSettings.DefaultArmCount;

    private static bool TryInflate(byte[] compressed, out byte[] json)
    {
        json = [];
        try
        {
            using var input = new MemoryStream(compressed);
            using var inflate = new DeflateStream(input, CompressionMode.Decompress);
            var buffer = new byte[MaxJsonBytes + 1];
            var total = 0;
            int read;
            while (total < buffer.Length && (read = inflate.Read(buffer, total, buffer.Length - total)) > 0)
            {
                total += read;
            }

            if (total > MaxJsonBytes)
            {
                return false;
            }

            json = buffer[..total];
            return true;
        }
        catch (InvalidDataException)
        {
            return false;
        }
    }
}
