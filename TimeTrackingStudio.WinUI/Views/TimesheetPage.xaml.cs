using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TimeTrackingStudio.WinUI.Models;
using TimeTrackingStudio.WinUI.ViewModels;

namespace TimeTrackingStudio.WinUI.Views;

public sealed partial class TimesheetPage : Page
{
    public TimesheetViewModel ViewModel { get; }

    public TimesheetPage()
    {
        this.InitializeComponent();
        ViewModel = new TimesheetViewModel();
    }

    private async void OnNewEntryClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new TimeEntryDialog
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.AddManualLog(dialog.TimeLogModel);
        }
    }

    private async void OnDeleteEntryClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is TimeLog log)
        {
            var confirmDialog = new ContentDialog
            {
                Title = "Eliminar Registro",
                Content = $"¿Deseas eliminar este registro de {log.Hours}h del trabajo '{log.JobTitle}'?",
                PrimaryButtonText = "Eliminar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                ViewModel.DeleteLog(log);
            }
        }
    }
}
