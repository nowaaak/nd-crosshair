using System.Windows.Threading;

namespace NdCrosshair.App.Services;

internal sealed class ToastService : IDisposable
{
    private static readonly TimeSpan DefaultDuration = TimeSpan.FromSeconds(6);

    private static ToastService? current;

    private readonly DispatcherTimer hideTimer;
    private ToastWindow? window;

    public ToastService()
    {
        hideTimer = new DispatcherTimer(DispatcherPriority.Background, Dispatcher.CurrentDispatcher);
        hideTimer.Tick += (_, _) => Hide();
        current = this;
    }

    public string? MonitorDeviceName { get; set; }

    public static void ShowGlobal(string message) => current?.Show(message);

    public void Show(string message, TimeSpan? duration = null)
    {
        window ??= CreateWindow();
        window.Show(message, MonitorDeviceName);

        hideTimer.Stop();
        hideTimer.Interval = duration ?? DefaultDuration;
        hideTimer.Start();
    }

    public void Dispose()
    {
        hideTimer.Stop();
        window?.Close();
        window = null;
        if (ReferenceEquals(current, this))
        {
            current = null;
        }
    }

    private ToastWindow CreateWindow()
    {
        var toast = new ToastWindow();
        toast.CloseRequested += (_, _) => Hide();
        return toast;
    }

    private void Hide()
    {
        hideTimer.Stop();
        window?.Hide();
    }
}
