using System.Windows;
using System.Windows.Controls;

namespace NdCrosshair.App.Views;

public partial class CrosshairPage : UserControl
{
    public CrosshairPage()
    {
        InitializeComponent();
    }

    private void OnSwatch(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { Tag: string hex })
        {
            PageActions.ViewModel(this).ColorHex = hex;
        }
    }

    private void OnCopyShareCode(object sender, RoutedEventArgs e) => PageActions.CopyShareCode(PageActions.ViewModel(this));
}
