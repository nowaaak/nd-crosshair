using NdCrosshair.App.Native;

namespace NdCrosshair.App.Services;

internal readonly record struct ForegroundInfo(string Title, bool IsOwnProcess);

internal static unsafe class ForegroundWindow
{
    private const int MaxTitleLength = 256;

    public static ForegroundInfo? Get()
    {
        var handle = User32.GetForegroundWindow();
        if (handle == 0)
        {
            return null;
        }

        uint processId;
        User32.GetWindowThreadProcessId(handle, &processId);

        var buffer = stackalloc char[MaxTitleLength];
        var length = User32.GetWindowText(handle, buffer, MaxTitleLength);
        var title = length > 0 ? new string(buffer, 0, length) : string.Empty;
        return new ForegroundInfo(title, processId == Environment.ProcessId);
    }

    public static bool Matches(ForegroundInfo? foreground, IReadOnlyList<string> titles)
    {
        if (foreground is not { } info)
        {
            return false;
        }

        return info.IsOwnProcess || titles.Any(title => info.Title.Contains(title, StringComparison.OrdinalIgnoreCase));
    }
}
