using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TimeTrackingStudio.WinUI.Models;
using TimeTrackingStudio.WinUI.ViewModels;

namespace TimeTrackingStudio.WinUI.Views;

public sealed partial class ClientsPage : Page
{
    public ClientsViewModel ViewModel { get; }

    public ClientsPage()
    {
        this.InitializeComponent();
        ViewModel = new ClientsViewModel();
    }

    private async void OnNewClientClicked(object sender, RoutedEventArgs e)
    {
        var dialog = new ClientDialog
        {
            XamlRoot = this.XamlRoot
        };

        var result = await dialog.ShowAsync();
        if (result == ContentDialogResult.Primary)
        {
            ViewModel.AddClient(dialog.ClientModel);
        }
    }

    private async void OnEditClientClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Client client)
        {
            var dialog = new ClientDialog(client)
            {
                XamlRoot = this.XamlRoot
            };

            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                ViewModel.UpdateClient(dialog.ClientModel);
            }
        }
    }

    private async void OnDeleteClientClicked(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is Client client)
        {
            var confirmDialog = new ContentDialog
            {
                Title = "Eliminar Cliente",
                Content = $"¿Estás seguro de que deseas eliminar al cliente '{client.Name}' y todos sus proyectos asociados? Esta acción no se puede deshacer.",
                PrimaryButtonText = "Eliminar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = this.XamlRoot
            };

            var result = await confirmDialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                ViewModel.DeleteClient(client);
            }
        }
    }
}
