using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeTrackingStudio.WinUI.Models;
using TimeTrackingStudio.WinUI.Services;

namespace TimeTrackingStudio.WinUI.ViewModels;

/// <summary>
/// ViewModel principal que orquesta el cronómetro persistente global y el estado general.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    private readonly TimeService _timeService;
    private readonly ClientService _clientService;
    private readonly JobService _jobService;

    [ObservableProperty]
    private string _timerDisplay = "00:00:00";

    [ObservableProperty]
    private bool _isTimerRunning;

    [ObservableProperty]
    private bool _isTimerPaused;

    [ObservableProperty]
    private bool _isTimerStopped = true;

    [ObservableProperty]
    private string _timerDescription = string.Empty;

    [ObservableProperty]
    private ObservableCollection<Client> _clients = new();

    [ObservableProperty]
    private ObservableCollection<Job> _availableJobs = new();

    [ObservableProperty]
    private Client? _selectedClient;

    [ObservableProperty]
    private Job? _selectedJob;

    [ObservableProperty]
    private string _currentRunningInfo = string.Empty;

    public event Action<string, string>? NotificationRequested;

    public MainViewModel(TimeService? timeService = null, ClientService? clientService = null, JobService? jobService = null)
    {
        _timeService = timeService ?? TimeService.Instance;
        _clientService = clientService ?? new ClientService();
        _jobService = jobService ?? new JobService();

        _timeService.Tick += OnTimerTick;
        _timeService.StateChanged += OnTimerStateChanged;

        LoadClientsAndJobs();
    }

    public void LoadClientsAndJobs()
    {
        var clientsList = _clientService.GetAllClients(onlyActive: true);
        Clients.Clear();
        foreach (var c in clientsList)
        {
            Clients.Add(c);
        }

        if (Clients.Any() && SelectedClient == null)
        {
            SelectedClient = Clients.First();
        }
    }

    partial void OnSelectedClientChanged(Client? value)
    {
        AvailableJobs.Clear();
        if (value != null)
        {
            var jobs = _jobService.GetAllJobs(clientId: value.Id);
            foreach (var j in jobs.Where(x => x.Status != "Facturado"))
            {
                AvailableJobs.Add(j);
            }
            SelectedJob = AvailableJobs.FirstOrDefault();
        }
    }

    private void OnTimerTick(int seconds)
    {
        var ts = TimeSpan.FromSeconds(seconds);
        TimerDisplay = $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
    }

    private void OnTimerStateChanged(TimerState state)
    {
        IsTimerRunning = state == TimerState.Running;
        IsTimerPaused = state == TimerState.Paused;
        IsTimerStopped = state == TimerState.Stopped;

        if (state == TimerState.Running)
        {
            CurrentRunningInfo = $"{_timeService.CurrentJobTitle} ({_timeService.CurrentClientName})";
        }
        else if (state == TimerState.Stopped)
        {
            CurrentRunningInfo = string.Empty;
            TimerDisplay = "00:00:00";
        }
    }

    [RelayCommand]
    public void StartTimer()
    {
        if (SelectedJob == null)
        {
            NotificationRequested?.Invoke("Atención", "Por favor selecciona un trabajo o proyecto para iniciar el cronómetro.");
            return;
        }

        var rate = SelectedJob.HourlyRate ?? SelectedClient?.HourlyRate ?? 0.0;
        _timeService.StartTimer(SelectedJob.Id, SelectedJob.Title, SelectedClient?.Name ?? "Cliente", rate, TimerDescription);
    }

    [RelayCommand]
    public void PauseTimer()
    {
        _timeService.PauseTimer();
    }

    [RelayCommand]
    public void ResumeTimer()
    {
        _timeService.ResumeTimer();
    }

    [RelayCommand]
    public void StopTimer()
    {
        var savedLog = _timeService.StopAndSave();
        if (savedLog != null)
        {
            TimerDescription = string.Empty;
            NotificationRequested?.Invoke("Sesión Guardada", $"Se registraron {savedLog.Hours:F2} horas trabajadas en '{savedLog.JobTitle}'.");
        }
    }

    [RelayCommand]
    public void DiscardTimer()
    {
        _timeService.Discard();
        TimerDescription = string.Empty;
        NotificationRequested?.Invoke("Cronómetro Descartado", "La sesión actual fue cancelada sin guardar.");
    }
}
