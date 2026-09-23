using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace NdCrosshair.App.Views;

public partial class OverlayPage : UserControl
{
    public OverlayPage()
    {
        InitializeComponent();
    }

    private void OnResetOffset(object sender, RoutedEventArgs e) => PageActions.ViewModel(this).ResetOffset();

    private void OnAddTitle(object sender, RoutedEventArgs e) => PageActions.ViewModel(this).AddGameTitle();

    private void OnCaptureWindow(object sender, RoutedEventArgs e) => PageActions.ViewModel(this).StartWindowCapture();

    private void OnRemoveTitle(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string title })
        {
            PageActions.ViewModel(this).RemoveGameTitle(title);
        }
    }

    private void OnTitleKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            PageActions.ViewModel(this).AddGameTitle();
            e.Handled = true;
        }
    }
}
