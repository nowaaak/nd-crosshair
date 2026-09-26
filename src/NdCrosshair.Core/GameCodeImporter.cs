namespace NdCrosshair.Core;

public enum GameCodeSource
{
    CounterStrike2,
    Valorant,
}

public enum GameCodeError
{
    None,
    Empty,
    Unrecognized,
    Invalid,
    ChecksumMismatch,
    OutdatedCs2Format,
}

public enum GameCodeNote
{
    DynamicShownStatic,
    OuterLinesIgnored,
    SeparateVerticalLengthIgnored,
    OutlineOpacityApproximated,
    DotOpacityApproximated,
    CircleApproximated,
    SquareApproximated,
    HalfOutlineApproximated,
    FollowRecoilIgnored,
    ValuesClamped,
}

public sealed record GameCodeImport(GameCodeSource Source, CrosshairSettings Settings, IReadOnlyList<GameCodeNote> Notes);

public static class GameCodeImporter
{
    public static bool IsGameCode(string? code)
    {
        var trimmed = code?.Trim() ?? string.Empty;
        return Cs2CrosshairCode.IsCandidate(trimmed) || ValorantCrosshairCode.IsCandidate(trimmed);
    }

    public static bool TryImport(string? code, int screenHeight, out GameCodeImport? result, out GameCodeError error)
    {
        result = null;
        var trimmed = code?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            error = GameCodeError.Empty;
            return false;
        }

        if (Cs2CrosshairCode.IsCandidate(trimmed))
        {
            if (!Cs2CrosshairCode.TryDecode(trimmed, out var crosshair, out error))
            {
                return false;
            }

            result = Finish(GameCodeSource.CounterStrike2, Cs2CrosshairCode.ToSettings(crosshair, screenHeight, out var notes), notes);
            return true;
        }

        if (ValorantCrosshairCode.IsCandidate(trimmed))
        {
            if (!ValorantCrosshairCode.TryParse(trimmed, out var profile))
            {
                error = GameCodeError.Invalid;
                return false;
            }

            result = Finish(GameCodeSource.Valorant, ValorantCrosshairCode.ToSettings(profile, out var notes), notes);
            error = GameCodeError.None;
            return true;
        }

        error = GameCodeError.Unrecognized;
        return false;
    }

    private static GameCodeImport Finish(GameCodeSource source, CrosshairSettings settings, List<GameCodeNote> notes)
    {
        var clamped = settings.Clamp();
        if (clamped != settings)
        {
            notes.Add(GameCodeNote.ValuesClamped);
        }

        return new GameCodeImport(source, clamped, notes.Distinct().ToList());
    }
}
