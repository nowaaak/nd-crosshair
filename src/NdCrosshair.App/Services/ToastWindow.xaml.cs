using System.Windows;
using System.Windows.Interop;
using NdCrosshair.App.Native;

namespace NdCrosshair.App.Services;

internal partial class ToastWindow : Window
{
    private const int GWL_EXSTYLE = -20;
    private const double ScreenMargin = 8;

    public ToastWindow()
    {
        InitializeComponent();
        SizeChanged += (_, _) => PlaceInCorner();
    }

    public event EventHandler? CloseRequested;

    public void Show(string message)
    {
        MessageText.Text = message;
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
        var area = SystemParameters.WorkArea;
        Left = area.Right - ActualWidth - ScreenMargin;
        Top = area.Bottom - ActualHeight - ScreenMargin;
    }

    private void OnClose(object sender, RoutedEventArgs e) => CloseRequested?.Invoke(this, EventArgs.Empty);
}
