using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TimeTrackingStudio.WinUI.Models;

namespace TimeTrackingStudio.WinUI.Services;

/// <summary>
/// Servicio de generación de reportes y exportación local a formatos CSV y HTML.
/// Guarda los archivos de salida en la carpeta outputs/.
/// </summary>
public class ExportService
{
    private readonly string _outputDirectory;

    public ExportService()
    {
        // Buscar o crear la carpeta outputs relativa o en AppData
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var projectDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
        var candidateOutputs = Path.Combine(projectDir, "outputs");

        if (Directory.Exists(candidateOutputs))
        {
            _outputDirectory = candidateOutputs;
        }
        else
        {
            _outputDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TimeTrackingStudio", "outputs");
            Directory.CreateDirectory(_outputDirectory);
        }
    }

    public string ExportToCsv(List<TimeLog> logs, string filenamePrefix = "reporte_tiempos")
    {
        var filename = $"{filenamePrefix}_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        var fullPath = Path.Combine(_outputDirectory, filename);

        var sb = new StringBuilder();
        sb.AppendLine("ID,Cliente,Trabajo,Fecha Inicio,Fecha Fin,Duración (s),Horas,Tarifa,Facturable,Total");

        foreach (var log in logs)
        {
            sb.AppendLine($"\"{log.Id}\",\"{log.ClientName}\",\"{log.JobTitle}\",\"{log.StartTime}\",\"{log.EndTime}\",\"{log.DurationSeconds}\",\"{log.Hours:F2}\",\"{log.HourlyRate:F2}\",\"{(log.IsBillable ? "Sí" : "No")}\",\"{log.Amount:F2}\"");
        }

        File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
        return fullPath;
    }

    public string ExportToHtmlReport(List<TimeLog> logs, string periodName = "Histórico Completo")
    {
        var filename = $"informe_ejecutivo_{DateTime.Now:yyyyMMdd_HHmmss}.html";
        var fullPath = Path.Combine(_outputDirectory, filename);

        double totalHours = 0;
        double totalBillable = 0;

        foreach (var l in logs)
        {
            totalHours += l.Hours;
            totalBillable += l.Amount;
        }

        var sb = new StringBuilder();
        sb.Append($@"<!DOCTYPE html>
<html lang=""es"">
<head>
    <meta charset=""UTF-8"">
    <title>Reporte Ejecutivo - Time Tracking Studio</title>
    <style>
        body {{ font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; margin: 40px; color: #222; background: #fdfdfd; }}
        .header {{ display: flex; justify-content: space-between; align-items: center; border-bottom: 2px solid #0078d4; padding-bottom: 15px; margin-bottom: 25px; }}
        .header h1 {{ margin: 0; color: #0078d4; font-size: 24px; }}
        .meta {{ font-size: 13px; color: #666; }}
        .kpi-container {{ display: flex; gap: 20px; margin-bottom: 30px; }}
        .kpi-card {{ flex: 1; background: #fff; border: 1px solid #e1dfdd; border-radius: 8px; padding: 18px; box-shadow: 0 2px 4px rgba(0,0,0,0.05); }}
        .kpi-title {{ font-size: 12px; color: #605e5c; text-transform: uppercase; font-weight: 600; margin-bottom: 6px; }}
        .kpi-value {{ font-size: 26px; font-weight: 700; color: #201f1e; }}
        table {{ width: 100%; border-collapse: collapse; margin-top: 15px; background: #fff; border-radius: 6px; overflow: hidden; box-shadow: 0 1px 3px rgba(0,0,0,0.05); }}
        th, td {{ padding: 10px 14px; text-align: left; font-size: 13px; border-bottom: 1px solid #edebe9; }}
        th {{ background: #f3f2f1; color: #323130; font-weight: 600; }}
        tr:hover {{ background: #faf9f8; }}
        .tag-billable {{ background: #dff6dd; color: #107c41; padding: 3px 8px; border-radius: 12px; font-size: 11px; font-weight: 600; }}
        .footer {{ margin-top: 40px; text-align: center; font-size: 11px; color: #a19f9d; }}
    </style>
</head>
<body>
    <div class=""header"">
        <div>
            <h1>⏱️ Time Tracking Studio</h1>
            <div class=""meta"">Informe de Tiempos y Facturación | Período: {periodName}</div>
        </div>
        <div class=""meta"" style=""text-align: right;"">
            <strong>Generado:</strong> {DateTime.Now:dd/MM/yyyy HH:mm}<br/>
            <strong>Estado:</strong> 100% Local y Privado
        </div>
    </div>

    <div class=""kpi-container"">
        <div class=""kpi-card"">
            <div class=""kpi-title"">Horas Totales</div>
            <div class=""kpi-value"">{totalHours:F2} h</div>
        </div>
        <div class=""kpi-card"">
            <div class=""kpi-title"">Monto Facturable Estimado</div>
            <div class=""kpi-value"">${totalBillable:N2}</div>
        </div>
        <div class=""kpi-card"">
            <div class=""kpi-title"">Registros Analizados</div>
            <div class=""kpi-value"">{logs.Count}</div>
        </div>
    </div>

    <h3>Detalle de Entradas de Tiempo</h3>
    <table>
        <thead>
            <tr>
                <th>Cliente</th>
                <th>Trabajo / Tarea</th>
                <th>Fecha Inicio</th>
                <th>Duración</th>
                <th>Horas</th>
                <th>Tarifa</th>
                <th>Facturable</th>
                <th>Total</th>
            </tr>
        </thead>
        <tbody>");

        foreach (var l in logs)
        {
            sb.Append($@"
            <tr>
                <td><strong>{l.ClientName}</strong></td>
                <td>{l.JobTitle}</td>
                <td>{l.StartTime}</td>
                <td>{l.FormattedDuration}</td>
                <td>{l.Hours:F2} h</td>
                <td>${l.HourlyRate:F2}/h</td>
                <td><span class=""tag-billable"">{(l.IsBillable ? "Sí" : "No")}</span></td>
                <td><strong>${l.Amount:F2}</strong></td>
            </tr>");
        }

        sb.Append($@"
        </tbody>
    </table>

    <div class=""footer"">
        Generado por Time Tracking Studio para Windows | Arquitectura WinUI 3 y MVVM
    </div>
</body>
</html>");

        File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
        return fullPath;
    }
}
