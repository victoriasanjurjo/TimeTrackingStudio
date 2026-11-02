using System;

namespace TimeTrackingStudio.WinUI.Models;

/// <summary>
/// Representa a un cliente con sus datos de contacto, tarifa base y divisa.
/// </summary>
public class Client
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public double HourlyRate { get; set; } = 0.0;
    public string Currency { get; set; } = "USD";
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public string CreatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

    // Propiedades calculadas para la interfaz
    public double TotalEarned { get; set; } = 0.0;
    public int ActiveJobsCount { get; set; } = 0;
    public string DisplayRate => $"{HourlyRate:F2} {Currency}/h";
}
