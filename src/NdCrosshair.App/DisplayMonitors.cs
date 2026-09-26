using System.Runtime.InteropServices;
using NdCrosshair.App.Native;

namespace NdCrosshair.App;

internal sealed record DisplayMonitor(string DeviceName, int Left, int Top, int Width, int Height, bool IsPrimary, ScreenRect WorkArea)
{
    public int CenterX => Left + Width / 2;

    public int CenterY => Top + Height / 2;
}

internal static unsafe class DisplayMonitors
{
    public static IReadOnlyList<DisplayMonitor> GetAll()
    {
        var monitors = new List<DisplayMonitor>();
        var handle = GCHandle.Alloc(monitors);
        try
        {
            User32.EnumDisplayMonitors(0, 0, &OnMonitor, GCHandle.ToIntPtr(handle));
        }
        finally
        {
            handle.Free();
        }

        return monitors
            .OrderByDescending(monitor => monitor.IsPrimary)
            .ThenBy(monitor => monitor.Left)
            .ThenBy(monitor => monitor.Top)
            .ToList();
    }

    public static DisplayMonitor? Resolve(string? deviceName)
    {
        var monitors = GetAll();
        return monitors.FirstOrDefault(monitor => monitor.DeviceName == deviceName)
            ?? monitors.FirstOrDefault(monitor => monitor.IsPrimary)
            ?? monitors.FirstOrDefault();
    }

    [UnmanagedCallersOnly]
    private static int OnMonitor(nint monitor, nint hdc, RECT* clip, nint data)
    {
        var info = new MONITORINFOEXW { CbSize = (uint)sizeof(MONITORINFOEXW) };
        if (User32.GetMonitorInfo(monitor, &info) && GCHandle.FromIntPtr(data).Target is List<DisplayMonitor> monitors)
        {
            var rect = info.RcMonitor;
            var work = info.RcWork;
            monitors.Add(new DisplayMonitor(
                new string(info.SzDevice),
                rect.Left,
                rect.Top,
                rect.Right - rect.Left,
                rect.Bottom - rect.Top,
                (info.DwFlags & User32.MONITORINFOF_PRIMARY) != 0,
                new ScreenRect(work.Left, work.Top, work.Right - work.Left, work.Bottom - work.Top)));
        }

        return 1;
    }
}
