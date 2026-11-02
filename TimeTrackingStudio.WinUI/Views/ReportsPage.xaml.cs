using Microsoft.UI.Xaml.Controls;
using TimeTrackingStudio.WinUI.ViewModels;

namespace TimeTrackingStudio.WinUI.Views;

public sealed partial class ReportsPage : Page
{
    public ReportsViewModel ViewModel { get; }

    public ReportsPage()
    {
        this.InitializeComponent();
        ViewModel = new ReportsViewModel();
        ViewModel.NotificationRequested += (title, message) =>
        {
            NotificationInfoBar.Title = title;
            NotificationInfoBar.Message = message;
            NotificationInfoBar.IsOpen = true;
        };
    }
}
