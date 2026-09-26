using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using NdCrosshair.App.Native;

namespace NdCrosshair.App.Services;

internal unsafe partial class ToastWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const double ScreenMargin = 8;

    private string? monitorDeviceName;

    public ToastWindow()
    {
        InitializeComponent();
        SizeChanged += (_, _) => PlaceInCorner();
    }

    public event EventHandler? CloseRequested;

    public void Show(string message, string? targetMonitor)
    {
        MessageText.Text = message;
        monitorDeviceName = targetMonitor;
        if (!IsVisible)
        {
            Show();
        }

        PlaceInCorner();
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        var handle = new WindowInteropHelper(this).Handle;
        var style = User32.GetWindowLongPtr(handle, GWL_EXSTYLE);
        User32.SetWindowLongPtr(handle, GWL_EXSTYLE, style | User32.WS_EX_NOACTIVATE | User32.WS_EX_TOOLWINDOW);
    }

    private void PlaceInCorner()
    {
        var handle = new WindowInteropHelper(this).Handle;
        RECT bounds;
        if (handle == 0 || DisplayMonitors.Resolve(monitorDeviceName) is not { } monitor || !User32.GetWindowRect(handle, &bounds))
        {
            var area = SystemParameters.WorkArea;
            Left = area.Right - ActualWidth - ScreenMargin;
            Top = area.Bottom - ActualHeight - ScreenMargin;
            return;
        }

        var margin = (int)Math.Round(ScreenMargin * VisualTreeHelper.GetDpi(this).DpiScaleX);
        var work = monitor.WorkArea;
        User32.SetWindowPos(
            handle,
            0,
            work.X + work.Width - (bounds.Right - bounds.Left) - margin,
            work.Y + work.Height - (bounds.Bottom - bounds.Top) - margin,
            0,
            0,
            User32.SWP_NOSIZE | User32.SWP_NOZORDER | User32.SWP_NOACTIVATE);
    }

    private void OnClose(object sender, RoutedEventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);
}
