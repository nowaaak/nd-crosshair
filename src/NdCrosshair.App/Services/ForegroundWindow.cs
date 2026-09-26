using NdCrosshair.App.Native;
using NdCrosshair.Core;

namespace NdCrosshair.App.Services;

internal readonly record struct ForegroundInfo(string Title, string? ProcessName, bool IsOwnProcess);

internal static unsafe class ForegroundWindow
{
    private const int MaxTitleLength = 512;

    private static uint cachedProcessId;
    private static string? cachedProcessName;

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
        var isOwnProcess = processId == Environment.ProcessId;
        return new ForegroundInfo(title, isOwnProcess ? null : ProcessName(processId), isOwnProcess);
    }

    public static bool IsOwnProcessActive()
    {
        var handle = User32.GetForegroundWindow();
        if (handle == 0)
        {
            return false;
        }

        uint processId;
        User32.GetWindowThreadProcessId(handle, &processId);
        return processId == Environment.ProcessId;
    }

    public static GameRule? FindRule(ForegroundInfo? foreground, IReadOnlyList<GameRule> rules) =>
        foreground is { IsOwnProcess: false } info
            ? rules.FirstOrDefault(rule => rule.Matches(info.ProcessName, info.Title))
            : null;

    public static bool IsGameOrSettings(ForegroundInfo? foreground, IReadOnlyList<GameRule> rules) =>
        foreground is { IsOwnProcess: true } || FindRule(foreground, rules) is not null;

    private static string? ProcessName(uint processId)
    {
        if (processId == 0)
        {
            return null;
        }

        if (processId != cachedProcessId || cachedProcessName is null)
        {
            cachedProcessId = processId;
            cachedProcessName = FindProcessName(processId);
        }

        return cachedProcessName;
    }

    private static string? FindProcessName(uint processId)
    {
        var snapshot = Kernel32.CreateToolhelp32Snapshot(Kernel32.TH32CS_SNAPPROCESS, 0);
        if (snapshot == Kernel32.INVALID_HANDLE_VALUE)
        {
            return null;
        }

        try
        {
            var entry = new PROCESSENTRY32W { DwSize = (uint)sizeof(PROCESSENTRY32W) };
            for (var found = Kernel32.Process32First(snapshot, &entry); found; found = Kernel32.Process32Next(snapshot, &entry))
            {
                if (entry.Th32ProcessID == processId)
                {
                    return new string(entry.SzExeFile);
                }
            }

            return null;
        }
        finally
        {
            Kernel32.CloseHandle(snapshot);
        }
    }
}
