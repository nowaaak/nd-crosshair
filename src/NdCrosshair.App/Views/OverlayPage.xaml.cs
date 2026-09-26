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

    private void OnAddRule(object sender, RoutedEventArgs e) => PageActions.ViewModel(this).AddGameRule();

    private void OnCaptureWindow(object sender, RoutedEventArgs e) => PageActions.ViewModel(this).StartWindowCapture();

    private void OnRemoveRule(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: GameRuleItem rule })
        {
            PageActions.ViewModel(this).RemoveGameRule(rule);
        }
    }

    private void OnRuleKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            PageActions.ViewModel(this).AddGameRule();
            e.Handled = true;
        }
    }
}
