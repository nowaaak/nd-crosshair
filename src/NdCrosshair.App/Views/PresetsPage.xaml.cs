using System.Windows;
using System.Windows.Controls;
using NdCrosshair.App.Localization;

namespace NdCrosshair.App.Views;

public partial class PresetsPage : UserControl
{
    public PresetsPage()
    {
        InitializeComponent();
    }

    private void OnAdd(object sender, RoutedEventArgs e) => PageActions.ViewModel(this).AddPreset();

    private void OnDuplicate(object sender, RoutedEventArgs e) => PageActions.ViewModel(this).DuplicatePreset();

    private void OnEdit(object sender, RoutedEventArgs e) => PageActions.ViewModel(this).CurrentPage = AppPage.Crosshair;

    private void OnCopy(object sender, RoutedEventArgs e) => PageActions.CopyShareCode(PageActions.ViewModel(this));

    private void OnImport(object sender, RoutedEventArgs e) => PageActions.ViewModel(this).ImportShareCode();

    private async void OnDelete(object sender, RoutedEventArgs e)
    {
        var viewModel = PageActions.ViewModel(this);
        var confirmed = await PageActions.ConfirmAsync(
            this,
            Loc.T("DeletePresetTitle"),
            Loc.Format("DeletePresetMessage", viewModel.SelectedPreset.Name),
            Loc.T("Delete"),
            destructive: true);

        if (confirmed)
        {
            viewModel.DeletePreset();
        }
    }
}
