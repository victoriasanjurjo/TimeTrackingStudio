using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TimeTrackingStudio.WinUI.Models;
using TimeTrackingStudio.WinUI.ViewModels;

namespace TimeTrackingStudio.WinUI.Views;

public sealed partial class JobsPage : Page
{
    public JobsViewModel ViewModel { get; }

    public JobsPage()
    {
        this.InitializeComponent();
        ViewModel = new JobsViewModel();
    }

    private void OnStatusFilterChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FilterStatusComboBox.SelectedItem is ComboBoxItem item && ViewModel != null)
        {
            ViewModel.StatusFilter = item.Content.ToString() ?? "Todos";
        }
    }

    private async void OnNewJobClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new JobDialog
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.AddJob(dialog.JobModel);
        }
    }

    private async void OnEditJobClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Job job)
        {
            var dialog = new JobDialog(job)
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                ViewModel.UpdateJob(dialog.JobModel);
            }
        }
    }

    private async void OnDeleteJobClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Job job)
        {
            var confirmDialog = new ContentDialog
            {
                Title = "Eliminar Trabajo",
                Content = $"¿Estás seguro de que deseas eliminar el trabajo '{job.Title}' y sus registros de tiempo asociados?",
                PrimaryButtonText = "Eliminar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                ViewModel.DeleteJob(job);
            }
        }
    }
}
