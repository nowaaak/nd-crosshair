using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Microsoft.Win32;
using NdCrosshair.App.Localization;

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

    private void OnAddLayer(object sender, RoutedEventArgs e)
    {
        var viewModel = PageActions.ViewModel(this);
        var menu = new ContextMenu { PlacementTarget = (UIElement)sender, Placement = PlacementMode.Bottom };
        menu.Items.Add(CreateMenuItem(Loc.T("LayerClassic"), viewModel.AddClassicLayer));
        menu.Items.Add(CreateMenuItem(Loc.T("LayerShape"), viewModel.AddShapeLayer));
        menu.Items.Add(CreateMenuItem(Loc.T("LayerText"), viewModel.AddTextLayer));
        menu.Items.Add(CreateMenuItem(Loc.T("LayerImage"), () =>
        {
            if (PickImage() is { } path)
            {
                viewModel.AddImageLayer(path);
            }
        }));
        menu.IsOpen = true;
    }

    private void OnLayerForward(object sender, RoutedEventArgs e) => PageActions.ViewModel(this).MoveSelectedLayer(1);

    private void OnLayerBackward(object sender, RoutedEventArgs e) => PageActions.ViewModel(this).MoveSelectedLayer(-1);

    private async void OnRemoveLayer(object sender, RoutedEventArgs e)
    {
        var viewModel = PageActions.ViewModel(this);
        var confirmed = await PageActions.ConfirmAsync(
            this,
            Loc.T("LayerRemoveTitle"),
            Loc.Format("LayerRemoveMessage", viewModel.SelectedLayerItem?.Title ?? string.Empty),
            Loc.T("Delete"),
            destructive: true);

        if (confirmed)
        {
            viewModel.RemoveSelectedLayer();
        }
    }

    private void OnReplaceImage(object sender, RoutedEventArgs e)
    {
        if (PickImage() is { } path)
        {
            PageActions.ViewModel(this).ReplaceImage(path);
        }
    }

    private string? PickImage()
    {
        var dialog = new OpenFileDialog
        {
            Filter = Loc.T("ImageFileFilter"),
            CheckFileExists = true,
        };

        return dialog.ShowDialog(Window.GetWindow(this)) == true ? dialog.FileName : null;
    }

    private static MenuItem CreateMenuItem(string header, Action onClick)
    {
        var item = new MenuItem { Header = header };
        item.Click += (_, _) => onClick();
        return item;
    }
}
