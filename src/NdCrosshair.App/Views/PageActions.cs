using System.Runtime.InteropServices;
using System.Windows;
using NdCrosshair.App.Localization;

namespace NdCrosshair.App.Views;

internal static class PageActions
{
    public static MainViewModel ViewModel(FrameworkElement element) => (MainViewModel)element.DataContext;

    public static Task<bool> ConfirmAsync(FrameworkElement element, string title, string message, string confirmText, bool destructive) =>
        Window.GetWindow(element) is MainWindow window
            ? window.ConfirmAsync(title, message, confirmText, destructive)
            : Task.FromResult(false);

    public static void CopyShareCode(MainViewModel viewModel)
    {
        try
        {
            Clipboard.SetText(viewModel.ShareCodeText);
            viewModel.ShareStatus = Loc.T("ShareCopied");
        }
        catch (COMException)
        {
            viewModel.ShareStatus = Loc.T("ClipboardBusy");
        }
    }
}
