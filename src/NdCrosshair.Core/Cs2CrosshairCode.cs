using System.Numerics;
using System.Text.RegularExpressions;

namespace NdCrosshair.Core;

internal sealed record Cs2Crosshair(
    int Version,
    int Style,
    bool FollowRecoil,
    bool CenterDot,
    bool TStyle,
    CrosshairColor Color,
    int Alpha,
    int Gap,
    int Length,
    int Thickness,
    int OutlineMode,
    int ScreenHeight);

internal static partial class Cs2CrosshairCode
{
    private const string Prefix = "CSGO";
    private const string Dictionary = "ABCDEFGHJKLMNOPQRSTUVWXYZabcdefhijkmnopqrstuvwxyz23456789";
    private const int ByteCount = 18;

    private const int StyleDynamicCross = 0;
    private const int StyleDynamicCircle = 1;
    private const int StyleDynamicCrossLegacy = 2;
    private const int StyleStaticCircle = 3;
    private const int StyleStaticCross = 4;
    private const int StyleShotFeedback = 5;
    private const int StyleDotOnly = 6;
    private const int StyleDynamicQuad = 7;
    private const int StyleStaticSquare = 8;

    private const int OutlineNone = 0;
    private const int OutlineHalf = 2;

    public static bool IsCandidate(string code) => code.StartsWith(Prefix, StringComparison.Ordinal);

    public static bool TryDecode(string code, out Cs2Crosshair crosshair, out GameCodeError error)
    {
        crosshair = null!;
        if (!SharePattern().IsMatch(code))
        {
            error = GameCodeError.Invalid;
            return false;
        }

        var characters = code[Prefix.Length..].Replace("-", string.Empty, StringComparison.Ordinal);
        var value = BigInteger.Zero;
        for (var i = characters.Length - 1; i >= 0; i--)
        {
            var index = Dictionary.IndexOf(characters[i], StringComparison.Ordinal);
            if (index < 0)
            {
                error = GameCodeError.Invalid;
                return false;
            }

            value = value * Dictionary.Length + index;
        }

        var raw = value.ToByteArray(isUnsigned: true, isBigEndian: true);
        if (raw.Length > ByteCount)
        {
            error = GameCodeError.Invalid;
            return false;
        }

        var bytes = new byte[ByteCount];
        raw.CopyTo(bytes, ByteCount - raw.Length);

        var checksum = 0;
        for (var i = 1; i < ByteCount; i++)
        {
            checksum += bytes[i];
        }

        if ((checksum & 0xFF) != bytes[0])
        {
            error = GameCodeError.ChecksumMismatch;
            return false;
        }

        switch (bytes[1])
        {
            case 1:
                error = GameCodeError.OutdatedCs2Format;
                return false;
            case 3:
            case 4:
                break;
            default:
                error = GameCodeError.Invalid;
                return false;
        }

        var version = bytes[1];
        var style = bytes[2] & 0xF;
        if (style > (version == 3 ? StyleDynamicQuad : StyleStaticSquare))
        {
            error = GameCodeError.Invalid;
            return false;
        }

        var bits = bytes[10] | (bytes[11] << 8) | (bytes[12] << 16) | (bytes[13] << 24);
        var outlineMode = version == 3
            ? ((bytes[2] & 0x20) != 0 ? 1 : OutlineNone)
            : (bytes[13] >> 4) & 3;

        crosshair = new Cs2Crosshair(
            version,
            style,
            FollowRecoil: (bytes[2] & 0x10) != 0,
            CenterDot: (bytes[2] & 0x40) != 0,
            TStyle: (bytes[2] & 0x80) != 0,
            new CrosshairColor(bytes[3], bytes[4], bytes[5]),
            Alpha: bytes[6],
            Gap: bytes[7],
            Length: bytes[8],
            Thickness: (bits >> 23) & 0x1F,
            outlineMode,
            ScreenHeight: bytes[14] | (bytes[15] << 8));
        error = GameCodeError.None;
        return true;
    }

    public static CrosshairSettings ToSettings(Cs2Crosshair crosshair, int screenHeight, out List<GameCodeNote> notes)
    {
        notes = [];
        var scale = crosshair.ScreenHeight > 0 && screenHeight > 0 ? (double)screenHeight / crosshair.ScreenHeight : 1;
        int Pixels(int value) => (int)Math.Round(value * scale, MidpointRounding.AwayFromZero);

        var thickness = Math.Max(1, Pixels(crosshair.Thickness));
        var gap = Pixels(crosshair.Gap);
        var length = Pixels(crosshair.Length);
        var lines = crosshair.Style is not (StyleDotOnly or StyleDynamicCircle or StyleStaticCircle);
        var ring = crosshair.Style is StyleDynamicCircle or StyleStaticCircle;

        if (crosshair.Style is StyleDynamicCross or StyleDynamicCircle or StyleDynamicCrossLegacy or StyleShotFeedback or StyleDynamicQuad)
        {
            notes.Add(GameCodeNote.DynamicShownStatic);
        }

        if (ring)
        {
            notes.Add(GameCodeNote.CircleApproximated);
        }

        if (crosshair.Style == StyleStaticSquare)
        {
            notes.Add(GameCodeNote.SquareApproximated);
        }

        if (crosshair.OutlineMode == OutlineHalf)
        {
            notes.Add(GameCodeNote.HalfOutlineApproximated);
        }

        if (crosshair.FollowRecoil)
        {
            notes.Add(GameCodeNote.FollowRecoilIgnored);
        }

        return new CrosshairSettings
        {
            ShowTop = lines && !crosshair.TStyle,
            ShowBottom = lines,
            ShowLeft = lines,
            ShowRight = lines,
            LineLength = length,
            LineThickness = thickness,
            Gap = gap,
            ShowDot = crosshair.CenterDot || crosshair.Style == StyleDotOnly,
            DotSize = thickness,
            ShowRing = ring,
            RingRadius = gap + length,
            RingThickness = thickness,
            ShowOutline = crosshair.OutlineMode != OutlineNone,
            OutlineThickness = 1,
            OutlineColor = CrosshairColor.Black,
            Color = crosshair.Color,
            Opacity = (int)Math.Round(crosshair.Alpha * 100 / 255.0, MidpointRounding.AwayFromZero),
        };
    }

    [GeneratedRegex("^CSGO(-?[A-Za-z0-9]{5}){5}$")]
    private static partial Regex SharePattern();
}
