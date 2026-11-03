pub mod models;
pub mod db;
pub mod export;
pub mod commands;

use db::DbState;

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    let db_state = DbState::new();

    tauri::Builder::default()
        .manage(db_state)
        .plugin(
            tauri_plugin_log::Builder::default()
                .level(log::LevelFilter::Info)
                .build(),
        )
        .invoke_handler(tauri::generate_handler![
            commands::get_clients,
            commands::add_client,
            commands::update_client,
            commands::delete_client,
            commands::get_jobs,
            commands::add_job,
            commands::update_job,
            commands::update_job_status,
            commands::delete_job,
            commands::get_time_logs,
            commands::add_time_log,
            commands::delete_time_log,
            commands::get_dashboard_metrics,
            commands::export_csv,
            commands::export_html,
            commands::open_output_folder,
            commands::open_file_path,
        ])
        .run(tauri::generate_context!())
        .expect("error while building tauri application");
}
