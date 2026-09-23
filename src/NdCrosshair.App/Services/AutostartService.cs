using Microsoft.Win32;

namespace NdCrosshair.App.Services;

internal static class AutostartService
{
    public const string TrayArgument = "--tray";

    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "NdCrosshair";

    private static string Command => $"\"{Environment.ProcessPath}\" {TrayArgument}";

    public static bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        return key?.GetValue(ValueName) is string value
            && string.Equals(value, Command, StringComparison.OrdinalIgnoreCase);
    }

    public static void SetEnabled(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true);
        if (enabled)
        {
            key.SetValue(ValueName, Command, RegistryValueKind.String);
        }
        else if (key.GetValue(ValueName) is not null)
        {
            key.DeleteValue(ValueName);
        }
    }
}
