using Velopack;
using Velopack.Sources;

namespace NdCrosshair.App.Services;

internal sealed class UpdateService
{
    private const string RepositoryUrl = "https://github.com/nowaaak/nd-crosshair";

    private readonly UpdateManager manager = new(new GithubSource(RepositoryUrl, null, false));

    public bool IsInstalled => manager.IsInstalled;

    public string CurrentVersion => manager.CurrentVersion?.ToString() ?? string.Empty;

    public Task<UpdateInfo?> CheckAsync() => manager.CheckForUpdatesAsync();

    public Task DownloadAsync(UpdateInfo update, Action<int> progress) => manager.DownloadUpdatesAsync(update, progress);

    public void ApplyAfterExit(UpdateInfo update) =>
        manager.WaitExitThenApplyUpdates(update.TargetFullRelease, silent: false, restart: true);
}
