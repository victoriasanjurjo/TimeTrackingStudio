using Microsoft.UI.Xaml;

namespace TimeTrackingStudio.WinUI;

/// <summary>
/// Provee el comportamiento específico de la aplicación y punto de entrada.
/// </summary>
public partial class App : Application
{
    public static MainWindow? MainAppWindow { get; private set; }

    public App()
    {
        this.InitializeComponent();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        MainAppWindow = new MainWindow();
        MainAppWindow.Activate();
    }
}
