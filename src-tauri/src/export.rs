use std::fs;
use std::path::PathBuf;
use chrono::Local;
use crate::models::TimeLog;

pub struct ExportService {
    output_dir: PathBuf,
}

impl ExportService {
    pub fn new() -> Self {
        // Buscar o crear la carpeta outputs relativa al proyecto o en AppData
        let current_dir = std::env::current_dir().unwrap_or_else(|_| PathBuf::from("."));
        let candidate = current_dir.join("outputs");

        let output_dir = if candidate.exists() {
            candidate
        } else {
            let parent_candidate = current_dir.parent().map(|p| p.join("outputs")).unwrap_or_default();
            if parent_candidate.exists() {
                parent_candidate
            } else {
                let local_app_data = dirs::data_local_dir()
                    .unwrap_or_else(|| PathBuf::from("."))
                    .join("TimeTrackingStudio")
                    .join("outputs");
                let _ = fs::create_dir_all(&local_app_data);
                local_app_data
            }
        };

        let _ = fs::create_dir_all(&output_dir);
        ExportService { output_dir }
    }

    pub fn get_output_dir(&self) -> PathBuf {
        self.output_dir.clone()
    }

    pub fn export_to_csv(&self, logs: &[TimeLog], prefix: Option<&str>) -> Result<String, String> {
        let prefix = prefix.unwrap_or("reporte_tiempos");
        let timestamp = Local::now().format("%Y%m%d_%H%M%S").to_string();
        let filename = format!("{}_{}.csv", prefix, timestamp);
        let path = self.output_dir.join(&filename);

        let mut wtr = csv::WriterBuilder::new()
            .has_headers(true)
            .from_path(&path)
            .map_err(|e| e.to_string())?;

        wtr.write_record(&[
            "ID",
            "Cliente",
            "Trabajo",
            "Fecha Inicio",
            "Fecha Fin",
            "Duración (s)",
            "Horas",
            "Tarifa",
            "Facturable",
            "Total",
        ])
        .map_err(|e| e.to_string())?;

        for log in logs {
            let end_time = log.end_time.clone().unwrap_or_default();
            let billable = if log.is_billable { "Sí" } else { "No" };
            wtr.write_record(&[
                log.id.to_string(),
                log.client_name.clone(),
                log.job_title.clone(),
                log.start_time.clone(),
                end_time,
                log.duration_seconds.to_string(),
                format!("{:.2}", log.hours),
                format!("{:.2}", log.hourly_rate),
                billable.to_string(),
                format!("{:.2}", log.amount),
            ])
            .map_err(|e| e.to_string())?;
        }

        wtr.flush().map_err(|e| e.to_string())?;
        Ok(path.to_string_lossy().to_string())
    }

    pub fn export_to_html_report(&self, logs: &[TimeLog], period_name: Option<&str>) -> Result<String, String> {
        let period = period_name.unwrap_or("Histórico Completo");
        let timestamp = Local::now().format("%Y%m%d_%H%M%S").to_string();
        let filename = format!("informe_ejecutivo_{}.html", timestamp);
        let path = self.output_dir.join(&filename);

        let total_hours: f64 = logs.iter().map(|l| l.hours).sum();
        let total_billable: f64 = logs.iter().map(|l| l.amount).sum();
        let date_str = Local::now().format("%d/%m/%Y %H:%M").to_string();

        let mut rows_html = String::new();
        for l in logs {
            let bill_badge = if l.is_billable {
                r#"<span class="tag-billable">Sí</span>"#
            } else {
                r#"<span class="tag-nonbillable">No</span>"#
            };
            rows_html.push_str(&format!(
                r#"<tr>
                    <td><strong>{}</strong></td>
                    <td>{}</td>
                    <td>{}</td>
                    <td>{}</td>
                    <td>{:.2} h</td>
                    <td>${:.2}/h</td>
                    <td>{}</td>
                    <td><strong>${:.2}</strong></td>
                </tr>"#,
                html_escape(&l.client_name),
                html_escape(&l.job_title),
                html_escape(&l.start_time),
                html_escape(&l.formatted_duration),
                l.hours,
                l.hourly_rate,
                bill_badge,
                l.amount
            ));
        }

        let html = format!(
            r#"<!DOCTYPE html>
<html lang="es">
<head>
    <meta charset="UTF-8">
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
        .tag-nonbillable {{ background: #f3f2f1; color: #605e5c; padding: 3px 8px; border-radius: 12px; font-size: 11px; font-weight: 600; }}
        .footer {{ margin-top: 40px; text-align: center; font-size: 11px; color: #a19f9d; }}
    </style>
</head>
<body>
    <div class="header">
        <div>
            <h1>⏱️ Time Tracking Studio</h1>
            <div class="meta">Informe de Tiempos y Facturación | Período: {}</div>
        </div>
        <div class="meta" style="text-align: right;">
            <strong>Generado:</strong> {}<br/>
            <strong>Estado:</strong> 100% Local y Privado (Rust + Tauri v2)
        </div>
    </div>

    <div class="kpi-container">
        <div class="kpi-card">
            <div class="kpi-title">Horas Totales</div>
            <div class="kpi-value">{:.2} h</div>
        </div>
        <div class="kpi-card">
            <div class="kpi-title">Monto Facturable Estimado</div>
            <div class="kpi-value">${:.2}</div>
        </div>
        <div class="kpi-card">
            <div class="kpi-title">Registros Analizados</div>
            <div class="kpi-value">{}</div>
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
        <tbody>
            {}
        </tbody>
    </table>

    <div class="footer">
        Generado por Time Tracking Studio para Windows | Arquitectura Rust y Tauri v2
    </div>
</body>
</html>"#,
            html_escape(period),
            date_str,
            total_hours,
            total_billable,
            logs.len(),
            rows_html
        );

        fs::write(&path, html.as_bytes()).map_err(|e| e.to_string())?;
        Ok(path.to_string_lossy().to_string())
    }
}

fn html_escape(input: &str) -> String {
    input
        .replace('&', "&amp;")
        .replace('<', "&lt;")
        .replace('>', "&gt;")
        .replace('"', "&quot;")
        .replace('\'', "&#39;")
}

