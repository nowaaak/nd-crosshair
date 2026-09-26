using System.IO;
using System.Windows.Media;

namespace NdCrosshair.App;

internal static class AppIdentity
{
#if DEBUG
    public const string Id = "NdCrosshair.Dev";
    public const string DisplayName = "ND Crosshair Dev";
    public const string IconFile = "app-dev.ico";
    public static readonly Color LogoBackground = Color.FromRgb(22, 163, 74);
#else
    public const string Id = "NdCrosshair";
    public const string DisplayName = "ND Crosshair";
    public const string IconFile = "app.ico";
    public static readonly Color LogoBackground = Colors.Black;
#endif

    public const string InstanceMutexName = @"Local\" + Id + ".Instance";
    public const string ShowSettingsEventName = @"Local\" + Id + ".ShowSettings";

    public static Uri IconUri => new($"pack://application:,,,/Assets/{IconFile}");

    public static string ConfigFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        Id,
        "config.json");
}
