using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using NdCrosshair.App.Native;
using NdCrosshair.Core;

namespace NdCrosshair.App;

internal readonly record struct ScreenRect(int X, int Y, int Width, int Height);

internal sealed unsafe class OverlayWindow : IDisposable
{
    private const string ClassName = "NdCrosshairOverlay";
    private static readonly TimeSpan TopmostInterval = TimeSpan.FromSeconds(2);

    private readonly User32.WndProc wndProc;
    private readonly nint instance;
    private readonly nint classNamePointer;
    private readonly nint hwnd;
    private readonly Dispatcher dispatcher;
    private readonly DispatcherTimer topmostTimer;

    private DibSurface? surface;
    private CrosshairImage? image;
    private DisplayMonitor? monitor;
    private string? monitorDeviceName;
    private int offsetX;
    private int offsetY;
    private bool visible;
    private bool renderFailed;
    private bool disposed;

    public OverlayWindow()
    {
        dispatcher = Dispatcher.CurrentDispatcher;
        wndProc = WindowProc;
        instance = Kernel32.GetModuleHandle(0);
        classNamePointer = Marshal.StringToHGlobalUni(ClassName);

        var windowClass = new WNDCLASSEXW
        {
            CbSize = (uint)sizeof(WNDCLASSEXW),
            LpfnWndProc = Marshal.GetFunctionPointerForDelegate(wndProc),
            HInstance = instance,
            LpszClassName = classNamePointer,
        };

        if (User32.RegisterClassEx(&windowClass) == 0)
        {
            var error = Marshal.GetLastPInvokeError();
            Marshal.FreeHGlobal(classNamePointer);
            throw new Win32Exception(error);
        }

        const int exStyle = User32.WS_EX_LAYERED
            | User32.WS_EX_TRANSPARENT
            | User32.WS_EX_TOPMOST
            | User32.WS_EX_TOOLWINDOW
            | User32.WS_EX_NOACTIVATE;

        hwnd = User32.CreateWindowEx(exStyle, classNamePointer, 0, User32.WS_POPUP, 0, 0, 1, 1, 0, 0, instance, 0);
        if (hwnd == 0)
        {
            var error = Marshal.GetLastPInvokeError();
            User32.UnregisterClass(classNamePointer, instance);
            Marshal.FreeHGlobal(classNamePointer);
            throw new Win32Exception(error);
        }

        topmostTimer = new DispatcherTimer(TopmostInterval, DispatcherPriority.Background, (_, _) => EnsureTopmost(), dispatcher);
        topmostTimer.Stop();
    }

    public event EventHandler? RenderFailed;

    public ScreenRect? Bounds { get; private set; }

    public void SetPlacement(string? deviceName, int x, int y)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        monitorDeviceName = deviceName;
        offsetX = x;
        offsetY = y;
        monitor = DisplayMonitors.Resolve(monitorDeviceName);
        Redraw();
    }

    public void SetImage(CrosshairImage crosshair)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        image = crosshair;
        Redraw();
    }

    public void SetVisible(bool value)
    {
        ObjectDisposedException.ThrowIf(disposed, this);

        if (visible == value)
        {
            return;
        }

        visible = value;
        if (visible)
        {
            Redraw();
            User32.ShowWindow(hwnd, User32.SW_SHOWNOACTIVATE);
            EnsureTopmost();
            topmostTimer.Start();
        }
        else
        {
            topmostTimer.Stop();
            User32.ShowWindow(hwnd, User32.SW_HIDE);
        }
    }

    public bool SetExcludedFromCapture(bool excluded)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return User32.SetWindowDisplayAffinity(hwnd, excluded ? User32.WDA_EXCLUDEFROMCAPTURE : User32.WDA_NONE);
    }

    public void Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        topmostTimer.Stop();
        User32.DestroyWindow(hwnd);
        User32.UnregisterClass(classNamePointer, instance);
        Marshal.FreeHGlobal(classNamePointer);
        surface?.Dispose();
    }

    private void EnsureTopmost()
    {
        if (!visible || disposed)
        {
            return;
        }

        if (renderFailed)
        {
            Redraw();
        }

        User32.SetWindowPos(
            hwnd,
            User32.HWND_TOPMOST,
            0,
            0,
            0,
            0,
            User32.SWP_NOMOVE | User32.SWP_NOSIZE | User32.SWP_NOACTIVATE | User32.SWP_NOOWNERZORDER);
    }

    private void Redraw()
    {
        if (image is null || disposed)
        {
            return;
        }

        if (monitor is not null && TryPresent(image, monitor))
        {
            renderFailed = false;
            return;
        }

        surface?.Dispose();
        surface = null;
        monitor = DisplayMonitors.Resolve(monitorDeviceName);
        if (monitor is not null && TryPresent(image, monitor))
        {
            renderFailed = false;
            return;
        }

        if (!renderFailed)
        {
            renderFailed = true;
            RenderFailed?.Invoke(this, EventArgs.Empty);
        }
    }

    private bool TryPresent(CrosshairImage image, DisplayMonitor monitor)
    {
        var size = image.Size;
        if (surface is null || surface.Width != size)
        {
            surface?.Dispose();
            surface = new DibSurface(size, size);
        }

        Marshal.Copy(image.Pixels, 0, (nint)surface.Bits, image.Pixels.Length);

        var destination = new POINT
        {
            X = monitor.CenterX + offsetX - image.Center,
            Y = monitor.CenterY + offsetY - image.Center,
        };
        var extent = new SIZE { Cx = size, Cy = size };
        var source = new POINT();
        var blend = new BLENDFUNCTION
        {
            BlendOp = User32.AC_SRC_OVER,
            SourceConstantAlpha = 255,
            AlphaFormat = User32.AC_SRC_ALPHA,
        };

        if (!User32.UpdateLayeredWindow(hwnd, 0, &destination, &extent, surface.DeviceContext, &source, 0, &blend, User32.ULW_ALPHA))
        {
            return false;
        }

        Bounds = new ScreenRect(destination.X, destination.Y, size, size);
        return true;
    }

    private void RefreshMonitor()
    {
        if (disposed)
        {
            return;
        }

        monitor = DisplayMonitors.Resolve(monitorDeviceName);
        Redraw();
    }

    private nint WindowProc(nint window, uint message, nint wParam, nint lParam)
    {
        if (message is User32.WM_DISPLAYCHANGE or User32.WM_DPICHANGED)
        {
            dispatcher.BeginInvoke(DispatcherPriority.Background, RefreshMonitor);
        }

        return User32.DefWindowProc(window, message, wParam, lParam);
    }
}
