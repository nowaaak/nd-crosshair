using System.Globalization;
using System.Text.RegularExpressions;

namespace NdCrosshair.Core;

internal static partial class ValorantCrosshairCode
{
    private const string PrimarySection = "P";
    private const int CustomColorIndex = 8;
    private const double OpacityTolerance = 0.05;

    private static readonly CrosshairColor[] PresetColors =
    [
        new(0xFF, 0xFF, 0xFF),
        new(0x00, 0xFF, 0x00),
        new(0x7F, 0xFF, 0x00),
        new(0xDF, 0xFF, 0x00),
        new(0xFF, 0xFF, 0x00),
        new(0x00, 0xFF, 0xFF),
        new(0xFF, 0x00, 0xFF),
        new(0xFF, 0x00, 0x00),
    ];

    private static readonly IReadOnlyDictionary<string, double> Defaults = new Dictionary<string, double>
    {
        ["c"] = 0,
        ["h"] = 1,
        ["t"] = 1,
        ["o"] = 0.5,
        ["d"] = 0,
        ["z"] = 2,
        ["a"] = 1,
        ["0b"] = 1,
        ["0t"] = 2,
        ["0l"] = 6,
        ["0v"] = 6,
        ["0g"] = 0,
        ["0o"] = 3,
        ["0a"] = 0.8,
        ["0m"] = 0,
        ["0f"] = 1,
        ["1b"] = 1,
        ["1l"] = 2,
        ["1a"] = 0.35,
    };

    public static bool IsCandidate(string code) => CandidatePattern().IsMatch(code);

    public static bool TryParse(string code, out IReadOnlyDictionary<string, string> primary)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        primary = values;

        var tokens = code.TrimEnd(';').Split(';');
        if (!int.TryParse(tokens[0], NumberStyles.None, CultureInfo.InvariantCulture, out _))
        {
            return false;
        }

        var section = string.Empty;
        var index = 1;
        while (index < tokens.Length)
        {
            var token = tokens[index].Trim();
            if (SectionPattern().IsMatch(token))
            {
                section = token;
                index++;
                continue;
            }

            if (token.Length == 0 || index + 1 >= tokens.Length)
            {
                return false;
            }

            if (section == PrimarySection)
            {
                values[token] = tokens[index + 1].Trim();
            }

            index += 2;
        }

        foreach (var (key, value) in values)
        {
            if (key == "u")
            {
                if (!TryParseColor(value, out _))
                {
                    return false;
                }
            }
            else if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number) || !double.IsFinite(number))
            {
                return false;
            }
        }

        var colorIndex = Number(values, "c");
        return colorIndex is >= 0 and <= CustomColorIndex && colorIndex == Math.Floor(colorIndex)
            && (colorIndex != CustomColorIndex || values.ContainsKey("u"));
    }

    public static CrosshairSettings ToSettings(IReadOnlyDictionary<string, string> primary, out List<GameCodeNote> notes)
    {
        notes = [];
        var colorIndex = (int)Number(primary, "c");
        var color = PresetColors[Math.Min(colorIndex, PresetColors.Length - 1)];
        if (colorIndex == CustomColorIndex && TryParseColor(primary["u"], out var custom))
        {
            color = custom;
        }

        var innerLines = Flag(primary, "0b") && Number(primary, "0l") > 0 && Number(primary, "0a") > 0;
        var dot = Flag(primary, "d") && Number(primary, "a") > 0;
        var outline = Flag(primary, "h") && Number(primary, "o") > 0;
        var lineOpacity = Number(primary, "0a");
        var dotOpacity = Number(primary, "a");

        if (innerLines && (Flag(primary, "0f") || Flag(primary, "0m")))
        {
            notes.Add(GameCodeNote.DynamicShownStatic);
        }

        if (Flag(primary, "1b") && Number(primary, "1l") > 0 && Number(primary, "1a") > 0)
        {
            notes.Add(GameCodeNote.OuterLinesIgnored);
        }

        if (innerLines && Flag(primary, "0g") && Number(primary, "0v") != Number(primary, "0l"))
        {
            notes.Add(GameCodeNote.SeparateVerticalLengthIgnored);
        }

        if (outline && Number(primary, "o") < 1 - OpacityTolerance)
        {
            notes.Add(GameCodeNote.OutlineOpacityApproximated);
        }

        if (innerLines && dot && Math.Abs(lineOpacity - dotOpacity) > OpacityTolerance)
        {
            notes.Add(GameCodeNote.DotOpacityApproximated);
        }

        var opacity = innerLines ? lineOpacity : dot ? dotOpacity : 1;
        return new CrosshairSettings
        {
            ShowTop = innerLines,
            ShowBottom = innerLines,
            ShowLeft = innerLines,
            ShowRight = innerLines,
            LineLength = Pixels(Number(primary, "0l")),
            LineThickness = Math.Max(1, Pixels(Number(primary, "0t"))),
            Gap = Pixels(Number(primary, "0o")),
            ShowDot = dot,
            DotSize = Math.Max(1, Pixels(Number(primary, "z"))),
            ShowOutline = outline,
            OutlineThickness = Math.Max(1, Pixels(Number(primary, "t"))),
            OutlineColor = CrosshairColor.Black,
            Color = color,
            Opacity = (int)Math.Round(opacity * 100, MidpointRounding.AwayFromZero),
        };
    }

    private static int Pixels(double value) => (int)Math.Round(value, MidpointRounding.AwayFromZero);

    private static bool Flag(IReadOnlyDictionary<string, string> values, string key) => Number(values, key) != 0;

    private static double Number(IReadOnlyDictionary<string, string> values, string key) =>
        values.TryGetValue(key, out var text) && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
            ? number
            : Defaults.GetValueOrDefault(key);

    private static bool TryParseColor(string text, out CrosshairColor color)
    {
        color = CrosshairColor.Black;
        var hex = text.TrimStart('#');
        if (hex.Length is not (6 or 8) || !uint.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _))
        {
            return false;
        }

        return CrosshairColor.TryParseHex("#" + hex[..6], out color);
    }

    [GeneratedRegex(@"^\d+(;|$)")]
    private static partial Regex CandidatePattern();

    [GeneratedRegex("^[A-Z]+$")]
    private static partial Regex SectionPattern();
}
