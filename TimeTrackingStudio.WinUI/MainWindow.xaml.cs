using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using TimeTrackingStudio.WinUI.Services;
using TimeTrackingStudio.WinUI.ViewModels;
using TimeTrackingStudio.WinUI.Views;

namespace TimeTrackingStudio.WinUI;

public sealed partial class MainWindow : Window
{
    public MainViewModel ViewModel { get; }

    public MainWindow()
    {
        this.InitializeComponent();

        Title = "Time Tracking Studio - Gestión Profesional de Horas";
        
        // Inicializar el despachador del cronómetro en el hilo de la UI
        TimeService.Instance.InitializeDispatcher(this.DispatcherQueue);

        ViewModel = new MainViewModel();
        ViewModel.NotificationRequested += OnNotificationRequested;
        ViewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(ViewModel.IsTimerRunning) ||
                e.PropertyName == nameof(ViewModel.IsTimerPaused) ||
                e.PropertyName == nameof(ViewModel.IsTimerStopped))
            {
                UpdateTimerButtonStates();
            }
        };

        UpdateTimerButtonStates();

        // Navegar por defecto al Dashboard
        NavView.SelectedItem = NavView.MenuItems[0];
        ContentFrame.Navigate(typeof(DashboardPage));
    }

    private void UpdateTimerButtonStates()
    {
        StartButton.Visibility = ViewModel.IsTimerStopped ? Visibility.Visible : Visibility.Collapsed;
        PauseButton.Visibility = ViewModel.IsTimerRunning ? Visibility.Visible : Visibility.Collapsed;
        ResumeButton.Visibility = ViewModel.IsTimerPaused ? Visibility.Visible : Visibility.Collapsed;
        StopButton.Visibility = !ViewModel.IsTimerStopped ? Visibility.Visible : Visibility.Collapsed;
        DiscardButton.Visibility = !ViewModel.IsTimerStopped ? Visibility.Visible : Visibility.Collapsed;
    }

    private void OnNavSelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item)
        {
            var tag = item.Tag?.ToString();
            Type pageType = tag switch
            {
                "Dashboard" => typeof(DashboardPage),
                "Clients" => typeof(ClientsPage),
                "Jobs" => typeof(JobsPage),
                "Timesheet" => typeof(TimesheetPage),
                "Reports" => typeof(ReportsPage),
                _ => typeof(DashboardPage)
            };

            if (ContentFrame.CurrentSourcePageType != pageType)
            {
                ContentFrame.Navigate(pageType);
            }

            // Recargar listas de clientes y trabajos en la barra superior al navegar
            ViewModel.LoadClientsAndJobs();
        }
    }

    private async void OnNotificationRequested(string title, string message)
    {
        if (ContentFrame.Content is Page activePage && activePage.XamlRoot != null)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = message,
                CloseButtonText = "Entendido",
                DefaultButton = ContentDialogButton.Close,
                XamlRoot = activePage.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}
