using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using TimeTrackingStudio.WinUI.Models;
using TimeTrackingStudio.WinUI.Services;

namespace TimeTrackingStudio.WinUI.ViewModels;

/// <summary>
/// ViewModel para el análisis y exportación de reportes ejecutivos a CSV y HTML.
/// </summary>
public partial class ReportsViewModel : ObservableObject
{
    private readonly TimeService _timeService;
    private readonly ExportService _exportService;

    [ObservableProperty]
    private ObservableCollection<TimeLog> _reportLogs = new();

    [ObservableProperty]
    private double _totalHours;

    [ObservableProperty]
    private double _totalEarnings;

    [ObservableProperty]
    private int _totalEntries;

    [ObservableProperty]
    private string _exportMessage = string.Empty;

    public event Action<string, string>? NotificationRequested;

    public ReportsViewModel(TimeService? timeService = null, ExportService? exportService = null)
    {
        _timeService = timeService ?? TimeService.Instance;
        _exportService = exportService ?? new ExportService();
        GenerateReport();
    }

    [RelayCommand]
    public void GenerateReport()
    {
        var logs = _timeService.GetAllLogs();
        ReportLogs.Clear();
        foreach (var l in logs)
        {
            ReportLogs.Add(l);
        }

        TotalHours = Math.Round(ReportLogs.Sum(x => x.Hours), 2);
        TotalEarnings = Math.Round(ReportLogs.Sum(x => x.Amount), 2);
        TotalEntries = ReportLogs.Count;
    }

    [RelayCommand]
    public void ExportCsv()
    {
        try
        {
            var logs = ReportLogs.ToList();
            var path = _exportService.ExportToCsv(logs);
            ExportMessage = $"Archivo CSV guardado en: {path}";
            NotificationRequested?.Invoke("Exportación Exitosa", $"Se generó el archivo CSV en:\n{path}");
        }
        catch (Exception ex)
        {
            NotificationRequested?.Invoke("Error al Exportar", ex.Message);
        }
    }

    [RelayCommand]
    public void ExportHtml()
    {
        try
        {
            var logs = ReportLogs.ToList();
            var path = _exportService.ExportToHtmlReport(logs);
            ExportMessage = $"Informe HTML generado en: {path}";
            NotificationRequested?.Invoke("Informe Generado", $"Se generó el informe ejecutivo HTML en:\n{path}");
        }
        catch (Exception ex)
        {
            NotificationRequested?.Invoke("Error al Exportar", ex.Message);
        }
    }
}
