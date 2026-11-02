using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeTrackingStudio.WinUI.Models;
using TimeTrackingStudio.WinUI.Services;

namespace TimeTrackingStudio.WinUI.ViewModels;

/// <summary>
/// ViewModel para la visualización cronológica, registro manual y filtrado de horas de trabajo.
/// </summary>
public partial class TimesheetViewModel : ObservableObject
{
    private readonly TimeService _timeService;

    [ObservableProperty]
    private ObservableCollection<TimeLog> _logs = new();

    [ObservableProperty]
    private DateTimeOffset? _fromDate;

    [ObservableProperty]
    private DateTimeOffset? _toDate;

    [ObservableProperty]
    private double _totalHours;

    [ObservableProperty]
    private double _totalAmount;

    public TimesheetViewModel(TimeService? timeService = null)
    {
        _timeService = timeService ?? TimeService.Instance;
        LoadLogs();
    }

    [RelayCommand]
    public void LoadLogs()
    {
        var from = FromDate?.DateTime;
        var to = ToDate?.DateTime;
        var list = _timeService.GetAllLogs(fromDate: from, toDate: to);

        Logs.Clear();
        foreach (var l in list)
        {
            Logs.Add(l);
        }

        TotalHours = Math.Round(Logs.Sum(x => x.Hours), 2);
        TotalAmount = Math.Round(Logs.Sum(x => x.Amount), 2);
    }

    partial void OnFromDateChanged(DateTimeOffset? value) => LoadLogs();
    partial void OnToDateChanged(DateTimeOffset? value) => LoadLogs();

    public void AddManualLog(TimeLog log)
    {
        _timeService.AddLog(log);
        LoadLogs();
    }

    [RelayCommand]
    public void DeleteLog(TimeLog? log)
    {
        if (log != null)
        {
            _timeService.DeleteLog(log.Id);
            LoadLogs();
        }
    }
}
