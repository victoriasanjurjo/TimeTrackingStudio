using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeTrackingStudio.WinUI.Models;
using TimeTrackingStudio.WinUI.Services;

namespace TimeTrackingStudio.WinUI.ViewModels;

/// <summary>
/// ViewModel para la administración, filtrado y actualización de estado de trabajos y tareas.
/// </summary>
public partial class JobsViewModel : ObservableObject
{
    private readonly JobService _jobService;

    [ObservableProperty]
    private ObservableCollection<Job> _jobs = new();

    [ObservableProperty]
    private Job? _selectedJob;

    [ObservableProperty]
    private string _statusFilter = "Todos";

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    public JobsViewModel(JobService? jobService = null)
    {
        _jobService = jobService ?? new JobService();
        LoadJobs();
    }

    [RelayCommand]
    public void LoadJobs()
    {
        var list = _jobService.GetAllJobs(StatusFilter, searchQuery: SearchQuery);
        Jobs.Clear();
        foreach (var j in list)
        {
            Jobs.Add(j);
        }
    }

    partial void OnStatusFilterChanged(string value)
    {
        LoadJobs();
    }

    partial void OnSearchQueryChanged(string value)
    {
        LoadJobs();
    }

    public void AddJob(Job job)
    {
        _jobService.AddJob(job);
        LoadJobs();
    }

    public void UpdateJob(Job job)
    {
        _jobService.UpdateJob(job);
        LoadJobs();
    }

    [RelayCommand]
    public void ChangeStatus((Job Job, string NewStatus) args)
    {
        if (args.Job != null && !string.IsNullOrEmpty(args.NewStatus))
        {
            _jobService.UpdateStatus(args.Job.Id, args.NewStatus);
            LoadJobs();
        }
    }

    [RelayCommand]
    public void DeleteJob(Job? job)
    {
        if (job != null)
        {
            _jobService.DeleteJob(job.Id);
            LoadJobs();
        }
    }
}
