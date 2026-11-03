use app_lib::models::*;
use app_lib::db::DbState;
use app_lib::export::ExportService;

#[test]
fn test_time_log_calculates_hours_and_amount_accurately() {
    let duration_seconds = 7200; // 2 horas
    let hourly_rate = 65.0;
    let is_billable = true;

    let hours = TimeLog::calculate_hours(duration_seconds);
    let amount = TimeLog::calculate_amount(duration_seconds, hourly_rate, is_billable);
    let formatted = TimeLog::format_duration(duration_seconds);

    assert!((hours - 2.0).abs() < 0.001);
    assert!((amount - 130.0).abs() < 0.001);
    assert_eq!(formatted, "02:00:00");
}

#[test]
fn test_time_log_non_billable_returns_zero_amount() {
    let duration_seconds = 3600;
    let hourly_rate = 100.0;
    let is_billable = false;

    let hours = TimeLog::calculate_hours(duration_seconds);
    let amount = TimeLog::calculate_amount(duration_seconds, hourly_rate, is_billable);

    assert!((hours - 1.0).abs() < 0.001);
    assert!((amount - 0.0).abs() < 0.001);
}

#[test]
fn test_job_calculates_progress_percentage_accurately() {
    let estimated = 40.0;
    let logged = 20.0;
    let progress = Job::calculate_progress(estimated, logged);
    assert!((progress - 50.0).abs() < 0.001);

    // Capped at 100%
    let progress_capped = Job::calculate_progress(10.0, 15.0);
    assert!((progress_capped - 100.0).abs() < 0.001);

    // Zero estimated
    let progress_zero = Job::calculate_progress(0.0, 5.0);
    assert_eq!(progress_zero, 0.0);
}

#[test]
fn test_client_display_rate_formats_correctly() {
    let rate = 75.5;
    let currency = "EUR";
    let formatted = Client::format_display_rate(rate, currency);
    assert_eq!(formatted, "75.50 EUR/h");
}

#[test]
fn test_db_in_memory_crud_and_metrics() {
    let db = DbState::new_in_memory();

    // 1. Añadir cliente
    let client_id = db.add_client(CreateClientInput {
        name: "Test Corp".to_string(),
        company: Some("Test Company".to_string()),
        email: Some("test@corp.com".to_string()),
        phone: Some("123456".to_string()),
        hourly_rate: 60.0,
        currency: Some("USD".to_string()),
        notes: Some("Cliente de prueba".to_string()),
        is_active: Some(true),
    }).expect("Failed to add client");

    let clients = db.get_all_clients(None, false).expect("Failed to get clients");
    assert_eq!(clients.len(), 1);
    assert_eq!(clients[0].name, "Test Corp");

    // 2. Añadir trabajo
    let job_id = db.add_job(CreateJobInput {
        client_id,
        title: "Desarrollo Rust".to_string(),
        description: Some("Migración a Tauri".to_string()),
        status: Some("En progreso".to_string()),
        priority: Some("Alta".to_string()),
        hourly_rate: Some(60.0),
        estimated_hours: Some(20.0),
        due_date: None,
    }).expect("Failed to add job");

    let jobs = db.get_all_jobs(None, None, None).expect("Failed to get jobs");
    assert_eq!(jobs.len(), 1);
    assert_eq!(jobs[0].title, "Desarrollo Rust");

    // 3. Registrar tiempo
    db.add_log(CreateTimeLogInput {
        job_id,
        start_time: Some("2026-11-02 10:00:00".to_string()),
        end_time: Some("2026-11-02 12:00:00".to_string()),
        duration_seconds: 7200,
        description: Some("Implementación backend".to_string()),
        is_billable: Some(true),
        hourly_rate: Some(60.0),
    }).expect("Failed to add log");

    let logs = db.get_all_logs(None, None, None, None).expect("Failed to get logs");
    assert_eq!(logs.len(), 1);
    assert_eq!(logs[0].hours, 2.0);
    assert_eq!(logs[0].amount, 120.0);

    // 4. Exportar a CSV y HTML
    let exporter = ExportService::new();
    let csv_path = exporter.export_to_csv(&logs, Some("test_report")).expect("CSV export failed");
    assert!(csv_path.ends_with(".csv"));

    let html_path = exporter.export_to_html_report(&logs, Some("Test Period")).expect("HTML export failed");
    assert!(html_path.ends_with(".html"));
}

