using System.Collections.Generic;

namespace TimeTrackingStudio.WinUI.Models;

/// <summary>
/// Modelo contenedor de métricas agregadas para el panel de control.
/// </summary>
public class DashboardMetrics
{
    public double HoursToday { get; set; } = 0.0;
    public double BillableToday { get; set; } = 0.0;
    public double HoursThisMonth { get; set; } = 0.0;
    public double BillableThisMonth { get; set; } = 0.0;
    public int ActiveJobsCount { get; set; } = 0;
    public int TotalClientsCount { get; set; } = 0;

    public List<DailyHourStat> Last7Days { get; set; } = new();
}

public class DailyHourStat
{
    public string DayName { get; set; } = string.Empty;
    public double Hours { get; set; } = 0.0;
    public string FormattedHours => $"{Hours:F1} h";
    public double MaxHeight { get; set; } = 150.0;
    public double BarHeight => Math.Min(MaxHeight, Math.Max(10.0, Hours * 20.0));
}
