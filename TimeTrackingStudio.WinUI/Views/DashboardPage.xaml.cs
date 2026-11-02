using Microsoft.UI.Xaml.Controls;
using TimeTrackingStudio.WinUI.ViewModels;

namespace TimeTrackingStudio.WinUI.Views;

public sealed partial class DashboardPage : Page
{
    public DashboardViewModel ViewModel { get; }

    public DashboardPage()
    {
        this.InitializeComponent();
        ViewModel = new DashboardViewModel();
    }
}
