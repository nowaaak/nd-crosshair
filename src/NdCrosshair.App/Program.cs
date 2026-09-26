using NdCrosshair.App.Services;
using Velopack;

namespace NdCrosshair.App;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        VelopackApp.Build()
            .OnBeforeUninstallFastCallback(_ => AutostartService.TryDisable())
            .Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
