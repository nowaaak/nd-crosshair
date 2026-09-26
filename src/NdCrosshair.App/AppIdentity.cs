using System.IO;

namespace NdCrosshair.App;

internal static class AppIdentity
{
#if DEBUG
    public const string Id = "NdCrosshair.Dev";
    public const string DisplayName = "ND Crosshair Dev";
#else
    public const string Id = "NdCrosshair";
    public const string DisplayName = "ND Crosshair";
#endif

    public const string InstanceMutexName = @"Local\" + Id + ".Instance";
    public const string ShowSettingsEventName = @"Local\" + Id + ".ShowSettings";

    public static string ConfigFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        Id,
        "config.json");
}
