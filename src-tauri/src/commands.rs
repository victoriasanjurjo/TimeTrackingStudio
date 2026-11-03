use std::process::Command;
use tauri::State;
use crate::db::DbState;
use crate::export::ExportService;
use crate::models::{
    Client, CreateClientInput, UpdateClientInput,
    Job, CreateJobInput, UpdateJobInput,
    TimeLog, CreateTimeLogInput,
    DashboardMetrics,
};

#[tauri::command]
pub fn get_clients(
    state: State<'_, DbState>,
    search: Option<String>,
    only_active: Option<bool>,
) -> Result<Vec<Client>, String> {
    state.get_all_clients(search, only_active.unwrap_or(false))
        .map_err(|e| e.to_string())
}

#[tauri::command]
pub fn add_client(
    state: State<'_, DbState>,
    input: CreateClientInput,
) -> Result<i64, String> {
    state.add_client(input).map_err(|e| e.to_string())
}

#[tauri::command]
pub fn update_client(
    state: State<'_, DbState>,
    input: UpdateClientInput,
) -> Result<(), String> {
    state.update_client(input).map_err(|e| e.to_string())
}

#[tauri::command]
pub fn delete_client(
    state: State<'_, DbState>,
    id: i64,
) -> Result<(), String> {
    state.delete_client(id).map_err(|e| e.to_string())
}

#[tauri::command]
pub fn get_jobs(
    state: State<'_, DbState>,
    status: Option<String>,
    client_id: Option<i64>,
    search: Option<String>,
) -> Result<Vec<Job>, String> {
    state.get_all_jobs(status, client_id, search)
        .map_err(|e| e.to_string())
}

#[tauri::command]
pub fn add_job(
    state: State<'_, DbState>,
    input: CreateJobInput,
) -> Result<i64, String> {
    state.add_job(input).map_err(|e| e.to_string())
}

#[tauri::command]
pub fn update_job(
    state: State<'_, DbState>,
    input: UpdateJobInput,
) -> Result<(), String> {
    state.update_job(input).map_err(|e| e.to_string())
}

#[tauri::command]
pub fn update_job_status(
    state: State<'_, DbState>,
    id: i64,
    status: String,
) -> Result<(), String> {
    state.update_job_status(id, &status).map_err(|e| e.to_string())
}

#[tauri::command]
pub fn delete_job(
    state: State<'_, DbState>,
    id: i64,
) -> Result<(), String> {
    state.delete_job(id).map_err(|e| e.to_string())
}

#[tauri::command]
pub fn get_time_logs(
    state: State<'_, DbState>,
    job_id: Option<i64>,
    client_id: Option<i64>,
    from_date: Option<String>,
    to_date: Option<String>,
) -> Result<Vec<TimeLog>, String> {
    state.get_all_logs(job_id, client_id, from_date, to_date)
        .map_err(|e| e.to_string())
}

#[tauri::command]
pub fn add_time_log(
    state: State<'_, DbState>,
    input: CreateTimeLogInput,
) -> Result<i64, String> {
    state.add_log(input).map_err(|e| e.to_string())
}

#[tauri::command]
pub fn delete_time_log(
    state: State<'_, DbState>,
    id: i64,
) -> Result<(), String> {
    state.delete_log(id).map_err(|e| e.to_string())
}

#[tauri::command]
pub fn get_dashboard_metrics(
    state: State<'_, DbState>,
) -> Result<DashboardMetrics, String> {
    state.get_dashboard_metrics().map_err(|e| e.to_string())
}

#[tauri::command]
pub fn export_csv(
    state: State<'_, DbState>,
    job_id: Option<i64>,
    client_id: Option<i64>,
    from_date: Option<String>,
    to_date: Option<String>,
    prefix: Option<String>,
) -> Result<String, String> {
    let logs = state.get_all_logs(job_id, client_id, from_date, to_date)
        .map_err(|e| e.to_string())?;
    let exporter = ExportService::new();
    exporter.export_to_csv(&logs, prefix.as_deref())
}

#[tauri::command]
pub fn export_html(
    state: State<'_, DbState>,
    job_id: Option<i64>,
    client_id: Option<i64>,
    from_date: Option<String>,
    to_date: Option<String>,
    period_name: Option<String>,
) -> Result<String, String> {
    let logs = state.get_all_logs(job_id, client_id, from_date, to_date)
        .map_err(|e| e.to_string())?;
    let exporter = ExportService::new();
    exporter.export_to_html_report(&logs, period_name.as_deref())
}

#[tauri::command]
pub fn open_output_folder() -> Result<String, String> {
    let exporter = ExportService::new();
    let dir = exporter.get_output_dir();
    #[cfg(target_os = "windows")]
    {
        Command::new("explorer")
            .arg(&dir)
            .spawn()
            .map_err(|e| e.to_string())?;
    }
    Ok(dir.to_string_lossy().to_string())
}

#[tauri::command]
pub fn open_file_path(path: String) -> Result<(), String> {
    #[cfg(target_os = "windows")]
    {
        Command::new("cmd")
            .args(["/c", "start", "", &path])
            .spawn()
            .map_err(|e| e.to_string())?;
    }
    Ok(())
}

