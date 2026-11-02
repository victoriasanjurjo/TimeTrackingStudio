using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeTrackingStudio.WinUI.Models;
using TimeTrackingStudio.WinUI.Services;

namespace TimeTrackingStudio.WinUI.ViewModels;

/// <summary>
/// ViewModel para el panel de resumen, indicadores clave y gráficos de rendimiento semanal.
/// </summary>
public partial class DashboardViewModel : ObservableObject
{
    private readonly TimeService _timeService;
    private readonly JobService _jobService;

    [ObservableProperty]
    private DashboardMetrics _metrics = new();

    [ObservableProperty]
    private ObservableCollection<Job> _activeJobs = new();

    [ObservableProperty]
    private ObservableCollection<TimeLog> _recentLogs = new();

    public DashboardViewModel(TimeService? timeService = null, JobService? jobService = null)
    {
        _timeService = timeService ?? TimeService.Instance;
        _jobService = jobService ?? new JobService();
        RefreshData();
    }

    [RelayCommand]
    public void RefreshData()
    {
        Metrics = _timeService.GetDashboardMetrics();

        ActiveJobs.Clear();
        var jobs = _jobService.GetAllJobs();
        foreach (var j in jobs)
        {
            if (j.Status != "Entregado" && j.Status != "Facturado")
            {
                ActiveJobs.Add(j);
            }
        }

        RecentLogs.Clear();
        var logs = _timeService.GetAllLogs();
        foreach (var l in logs)
        {
            if (RecentLogs.Count < 5)
            {
                RecentLogs.Add(l);
            }
        }
    }
}
