using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using NdCrosshair.App.Localization;
using NdCrosshair.Core;

namespace NdCrosshair.App.Views;

public partial class SystemPage : UserControl
{
    private const string FileFilterKey = "BackupFileFilter";

    public SystemPage()
    {
        InitializeComponent();
    }

    private void OnExport(object sender, RoutedEventArgs e)
    {
        var dialog = new SaveFileDialog
        {
            FileName = $"nd-crosshair-{DateTime.Now:yyyy-MM-dd}.json",
            Filter = Loc.T(FileFilterKey),
            AddExtension = true,
            DefaultExt = ".json",
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) != true)
        {
            return;
        }

        try
        {
            ConfigStore.Export(PageActions.ViewModel(this).ToConfig(), dialog.FileName);
            StatusText.Text = Loc.Format("ExportDone", dialog.FileName);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            StatusText.Text = Loc.Format("ExportFailed", exception.Message);
        }
    }

    private async void OnImport(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Filter = Loc.T(FileFilterKey),
            CheckFileExists = true,
        };

        if (dialog.ShowDialog(Window.GetWindow(this)) != true)
        {
            return;
        }

        AppConfig config;
        try
        {
            config = ConfigStore.Import(dialog.FileName);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or InvalidDataException)
        {
            StatusText.Text = Loc.Format("ImportFailed", exception.Message);
            return;
        }

        var confirmed = await PageActions.ConfirmAsync(
            this,
            Loc.T("ImportConfirmTitle"),
            Loc.T("ImportConfirmMessage"),
            Loc.T("ImportFile"),
            destructive: false);

        if (confirmed)
        {
            PageActions.ViewModel(this).ApplyConfig(config);
            StatusText.Text = Loc.T("ImportDone");
        }
    }

    private void OnOpenFolder(object sender, RoutedEventArgs e)
    {
        var path = PageActions.ViewModel(this).ConfigFilePath;
        var arguments = File.Exists(path)
            ? $"/select,\"{path}\""
            : $"\"{Path.GetDirectoryName(path)}\"";
        Process.Start(new ProcessStartInfo("explorer.exe", arguments) { UseShellExecute = true });
    }

    private async void OnReset(object sender, RoutedEventArgs e)
    {
        var confirmed = await PageActions.ConfirmAsync(
            this,
            Loc.T("ResetConfirmTitle"),
            Loc.T("ResetConfirmMessage"),
            Loc.T("Reset"),
            destructive: true);

        if (confirmed)
        {
            var viewModel = PageActions.ViewModel(this);
            viewModel.ApplyConfig(new AppConfig());
            viewModel.CurrentPage = AppPage.System;
            StatusText.Text = Loc.T("ResetDone");
        }
    }
}
