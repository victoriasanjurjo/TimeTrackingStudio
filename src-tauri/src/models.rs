use serde::{Deserialize, Serialize};

/// Representa a un cliente con sus datos de contacto, tarifa base y divisa.
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct Client {
    pub id: i64,
    pub name: String,
    pub company: String,
    pub email: String,
    pub phone: String,
    pub hourly_rate: f64,
    pub currency: String,
    pub notes: String,
    pub is_active: bool,
    pub created_at: String,
    // Propiedades calculadas para la interfaz
    pub total_earned: f64,
    pub active_jobs_count: i64,
    pub display_rate: String,
}

impl Client {
    pub fn format_display_rate(rate: f64, currency: &str) -> String {
        format!("{:.2} {}/h", rate, currency)
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CreateClientInput {
    pub name: String,
    pub company: Option<String>,
    pub email: Option<String>,
    pub phone: Option<String>,
    pub hourly_rate: f64,
    pub currency: Option<String>,
    pub notes: Option<String>,
    pub is_active: Option<bool>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct UpdateClientInput {
    pub id: i64,
    pub name: String,
    pub company: Option<String>,
    pub email: Option<String>,
    pub phone: Option<String>,
    pub hourly_rate: f64,
    pub currency: Option<String>,
    pub notes: Option<String>,
    pub is_active: bool,
}

/// Representa una tarea o proyecto asociado a un cliente.
/// Estados: Pendiente, En progreso, En revisión, Entregado, Facturado.
/// Prioridades: Baja, Media, Alta, Urgente.
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct Job {
    pub id: i64,
    pub client_id: i64,
    pub client_name: String,
    pub title: String,
    pub description: String,
    pub status: String,
    pub priority: String,
    pub hourly_rate: Option<f64>,
    pub estimated_hours: f64,
    pub due_date: Option<String>,
    pub created_at: String,
    pub updated_at: String,
    // Métricas enlazadas
    pub logged_hours: f64,
    pub total_earned: f64,
    pub progress_percentage: f64,
}

impl Job {
    pub fn calculate_progress(estimated: f64, logged: f64) -> f64 {
        if estimated > 0.0 {
            let pct = (logged / estimated) * 100.0;
            if pct > 100.0 { 100.0 } else { pct }
        } else {
            0.0
        }
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CreateJobInput {
    pub client_id: i64,
    pub title: String,
    pub description: Option<String>,
    pub status: Option<String>,
    pub priority: Option<String>,
    pub hourly_rate: Option<f64>,
    pub estimated_hours: Option<f64>,
    pub due_date: Option<String>,
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct UpdateJobInput {
    pub id: i64,
    pub client_id: i64,
    pub title: String,
    pub description: Option<String>,
    pub status: String,
    pub priority: String,
    pub hourly_rate: Option<f64>,
    pub estimated_hours: f64,
    pub due_date: Option<String>,
}

/// Representa una sesión o entrada de tiempo invertido en un trabajo.
#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct TimeLog {
    pub id: i64,
    pub job_id: i64,
    pub job_title: String,
    pub client_id: i64,
    pub client_name: String,
    pub start_time: String,
    pub end_time: Option<String>,
    pub duration_seconds: i64,
    pub description: String,
    pub is_billable: bool,
    pub hourly_rate: f64,
    pub created_at: String,
    pub hours: f64,
    pub amount: f64,
    pub formatted_duration: String,
}

impl TimeLog {
    pub fn calculate_hours(duration_seconds: i64) -> f64 {
        ((duration_seconds as f64) / 3600.0 * 100.0).round() / 100.0
    }

    pub fn calculate_amount(duration_seconds: i64, hourly_rate: f64, is_billable: bool) -> f64 {
        if is_billable {
            (((duration_seconds as f64) / 3600.0 * hourly_rate) * 100.0).round() / 100.0
        } else {
            0.0
        }
    }

    pub fn format_duration(duration_seconds: i64) -> String {
        let total_seconds = duration_seconds.max(0);
        let hours = total_seconds / 3600;
        let minutes = (total_seconds % 3600) / 60;
        let seconds = total_seconds % 60;
        format!("{:02}:{:02}:{:02}", hours, minutes, seconds)
    }
}

#[derive(Debug, Clone, Serialize, Deserialize)]
pub struct CreateTimeLogInput {
    pub job_id: i64,
    pub start_time: Option<String>,
    pub end_time: Option<String>,
    pub duration_seconds: i64,
    pub description: Option<String>,
    pub is_billable: Option<bool>,
    pub hourly_rate: Option<f64>,
}

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct DailyHourStat {
    pub day_name: String,
    pub hours: f64,
    pub formatted_hours: String,
    pub bar_height: f64,
}

#[derive(Debug, Clone, Serialize, Deserialize, PartialEq)]
pub struct DashboardMetrics {
    pub hours_today: f64,
    pub billable_today: f64,
    pub hours_this_month: f64,
    pub billable_this_month: f64,
    pub active_jobs_count: i64,
    pub total_clients_count: i64,
    pub last_7_days: Vec<DailyHourStat>,
}

