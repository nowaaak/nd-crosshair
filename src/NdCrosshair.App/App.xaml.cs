using System.Windows;
using System.Windows.Threading;
using NdCrosshair.App.Localization;
using NdCrosshair.App.Services;
using NdCrosshair.Core;

namespace NdCrosshair.App;

public partial class App : Application
{
    private const string AppName = "ND Crosshair";
    private const string InstanceMutexName = @"Local\NdCrosshair.Instance";
    private const string ShowSettingsEventName = @"Local\NdCrosshair.ShowSettings";

    private Mutex? instanceMutex;
    private EventWaitHandle? showSettingsEvent;
    private RegisteredWaitHandle? showSettingsWait;
    private AppController? controller;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        instanceMutex = new Mutex(true, InstanceMutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            SignalRunningInstance();
            instanceMutex.Dispose();
            instanceMutex = null;
            Shutdown();
            return;
        }

        DispatcherUnhandledException += OnDispatcherUnhandledException;
        System.Windows.Forms.Application.EnableVisualStyles();

        try
        {
            controller = new AppController(new ConfigStore(ConfigStore.DefaultFilePath));
            controller.Start(e.Args.Contains(AutostartService.TrayArgument, StringComparer.OrdinalIgnoreCase));
        }
        catch (Exception exception)
        {
            MessageBox.Show(Loc.Format("StartupFailed", exception.Message), AppName, MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown(1);
            return;
        }

        showSettingsEvent = new EventWaitHandle(false, EventResetMode.AutoReset, ShowSettingsEventName);
        showSettingsWait = ThreadPool.RegisterWaitForSingleObject(
            showSettingsEvent,
            (_, _) => Dispatcher.BeginInvoke(() => controller?.ShowSettings()),
            null,
            Timeout.Infinite,
            executeOnlyOnce: false);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        showSettingsWait?.Unregister(null);
        showSettingsEvent?.Dispose();
        controller?.Dispose();
        instanceMutex?.ReleaseMutex();
        instanceMutex?.Dispose();
        base.OnExit(e);
    }

    private static void SignalRunningInstance()
    {
        if (EventWaitHandle.TryOpenExisting(ShowSettingsEventName, out var runningInstanceEvent))
        {
            using (runningInstanceEvent)
            {
                runningInstanceEvent.Set();
            }
        }
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ToastService.ShowGlobal(Loc.Format("UnexpectedError", e.Exception.Message));
        e.Handled = true;
    }
}
