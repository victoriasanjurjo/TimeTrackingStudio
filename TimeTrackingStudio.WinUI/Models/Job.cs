using System;

namespace TimeTrackingStudio.WinUI.Models;

/// <summary>
/// Representa una tarea o proyecto asociado a un cliente.
/// Estados: Pendiente, En progreso, En revisión, Entregado, Facturado.
/// Prioridades: Baja, Media, Alta, Urgente.
/// </summary>
public class Job
{
    public int Id { get; set; }
    public int ClientId { get; set; }
    public string ClientName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Pendiente";
    public string Priority { get; set; } = "Media";
    public double? HourlyRate { get; set; }
    public double EstimatedHours { get; set; } = 0.0;
    public string? DueDate { get; set; }
    public string CreatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
    public string UpdatedAt { get; set; } = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");

    // Métricas enlazadas
    public double LoggedHours { get; set; } = 0.0;
    public double TotalEarned { get; set; } = 0.0;
    public double ProgressPercentage => EstimatedHours > 0 ? Math.Min(100.0, (LoggedHours / EstimatedHours) * 100.0) : 0.0;
}
