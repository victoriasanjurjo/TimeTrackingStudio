using System;

namespace TimeTrackingStudio.WinUI.Models;

/// <summary>
/// Representa una sesión o entrada de tiempo invertido en un trabajo.
/// </summary>
public class TimeLog
{
    public int Id { get; set; }
    public int JobId { get; set; }
    public string JobTitle { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string StartTime { get; set; } = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
    public string? EndTime { get; set; }
    public int DurationSeconds { get; set; } = 0;
    public string Description { get; set; } = string.Empty;
    public bool IsBillable { get; set; } = true;
    public double HourlyRate { get; set; } = 0.0;
    public string CreatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

    public double Hours => Math.Round(DurationSeconds / 3600.0, 2);
    public double Amount => IsBillable ? Math.Round((DurationSeconds / 3600.0) * HourlyRate, 2) : 0.0;
    public string FormattedDuration
    {
        get
        {
            var ts = TimeSpan.FromSeconds(DurationSeconds);
            return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}:{ts.Seconds:D2}";
        }
    }
}
