namespace NdCrosshair.Core;

public enum GameMatchKind
{
    Process,
    WindowTitle,
}

public sealed record GameRule(GameMatchKind Kind, string Pattern)
{
    public const int MaxPatternLength = 100;
    public const string ProcessExtension = ".exe";

    public Guid? PresetId { get; init; }

    public static GameRule? FromInput(string? input)
    {
        var trimmed = input?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return null;
        }

        var kind = trimmed.EndsWith(ProcessExtension, StringComparison.OrdinalIgnoreCase)
            ? GameMatchKind.Process
            : GameMatchKind.WindowTitle;
        return new GameRule(kind, trimmed).Normalize();
    }

    public bool Matches(string? processName, string? windowTitle) => Kind switch
    {
        GameMatchKind.Process => processName is not null
            && string.Equals(Path.GetFileName(processName), Pattern, StringComparison.OrdinalIgnoreCase),
        _ => windowTitle is not null && windowTitle.Contains(Pattern, StringComparison.OrdinalIgnoreCase),
    };

    public GameRule? Normalize()
    {
        if (!Enum.IsDefined(Kind))
        {
            return null;
        }

        var pattern = Pattern?.Trim() ?? string.Empty;
        if (Kind == GameMatchKind.Process)
        {
            pattern = Path.GetFileName(pattern);
        }

        if (pattern.Length == 0)
        {
            return null;
        }

        if (pattern.Length > MaxPatternLength)
        {
            pattern = pattern[..MaxPatternLength];
        }

        return this with { Pattern = pattern, PresetId = PresetId == Guid.Empty ? null : PresetId };
    }
}
