using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Threading;
using NdCrosshair.App.Localization;
using NdCrosshair.App.Services;
using Velopack;

namespace NdCrosshair.App;

internal enum UpdateState
{
    NotInstalled,
    Idle,
    Checking,
    UpToDate,
    Available,
    Downloading,
    Failed,
}

internal sealed class UpdateViewModel : INotifyPropertyChanged
{
    private readonly UpdateService service;
    private readonly Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
    private UpdateState state;
    private UpdateInfo? pending;
    private string? notifiedVersion;
    private int progress;
    private string error = string.Empty;

    public UpdateViewModel(UpdateService service)
    {
        this.service = service;
        state = service.IsInstalled ? UpdateState.Idle : UpdateState.NotInstalled;
        Loc.Instance.LanguageChanged += (_, _) => OnPropertyChanged(string.Empty);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public event EventHandler<string>? UpdateFound;

    public event EventHandler? RestartRequested;

    public bool IsSupported => state != UpdateState.NotInstalled;

    public bool CanCheck => state is UpdateState.Idle or UpdateState.UpToDate or UpdateState.Available or UpdateState.Failed;

    public bool CanInstall => state == UpdateState.Available;

    public string StatusText => state switch
    {
        UpdateState.NotInstalled => Loc.T("UpdateNotInstalled"),
        UpdateState.Checking => Loc.T("UpdateChecking"),
        UpdateState.UpToDate => Loc.Format("UpdateUpToDate", service.CurrentVersion),
        UpdateState.Available => Loc.Format("UpdateAvailable", PendingVersion),
        UpdateState.Downloading => Loc.Format("UpdateDownloading", progress),
        UpdateState.Failed => Loc.Format("UpdateFailed", error),
        _ => Loc.Format("UpdateInstalledVersion", service.CurrentVersion),
    };

    private string PendingVersion => pending?.TargetFullRelease.Version.ToString() ?? string.Empty;

    public async Task CheckAsync()
    {
        if (!CanCheck)
        {
            return;
        }

        SetState(UpdateState.Checking);
        try
        {
            pending = await service.CheckAsync();
        }
        catch (Exception exception)
        {
            Fail(exception);
            return;
        }

        if (pending is null)
        {
            SetState(UpdateState.UpToDate);
            return;
        }

        SetState(UpdateState.Available);
        if (PendingVersion != notifiedVersion)
        {
            notifiedVersion = PendingVersion;
            UpdateFound?.Invoke(this, PendingVersion);
        }
    }

    public async Task InstallAsync()
    {
        if (!CanInstall || pending is not { } update)
        {
            return;
        }

        progress = 0;
        SetState(UpdateState.Downloading);
        try
        {
            await service.DownloadAsync(update, value => dispatcher.BeginInvoke(() =>
            {
                progress = value;
                OnPropertyChanged(nameof(StatusText));
            }));
            service.ApplyAfterExit(update);
        }
        catch (Exception exception)
        {
            Fail(exception);
            return;
        }

        RestartRequested?.Invoke(this, EventArgs.Empty);
    }

    private void Fail(Exception exception)
    {
        error = exception.Message;
        SetState(UpdateState.Failed);
    }

    private void SetState(UpdateState value)
    {
        state = value;
        OnPropertyChanged(string.Empty);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
