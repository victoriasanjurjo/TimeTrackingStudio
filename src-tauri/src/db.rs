use std::path::PathBuf;
use std::sync::Mutex;
use chrono::{Local, Duration};
use rusqlite::{params, Connection, Result};
use crate::models::{
    Client, CreateClientInput, UpdateClientInput,
    Job, CreateJobInput, UpdateJobInput,
    TimeLog, CreateTimeLogInput,
    DashboardMetrics, DailyHourStat,
};

pub struct DbState {
    pub conn: Mutex<Connection>,
    pub db_path: PathBuf,
}

impl DbState {
    pub fn new() -> Self {
        let app_dir = dirs::data_local_dir()
            .unwrap_or_else(|| PathBuf::from("."))
            .join("TimeTrackingStudio");
        let _ = std::fs::create_dir_all(&app_dir);
        let db_path = app_dir.join("timetracker.db");

        let conn = Connection::open(&db_path).expect("Failed to open SQLite database");
        conn.execute_batch("PRAGMA foreign_keys = ON;").expect("Failed to set PRAGMA");

        let db = DbState {
            conn: Mutex::new(conn),
            db_path,
        };

        db.initialize_schema().expect("Failed to initialize database schema");
        db.seed_initial_data().expect("Failed to seed initial data");

        db
    }

    pub fn new_in_memory() -> Self {
        let conn = Connection::open_in_memory().expect("Failed to open in-memory database");
        conn.execute_batch("PRAGMA foreign_keys = ON;").expect("Failed to set PRAGMA");
        let db = DbState {
            conn: Mutex::new(conn),
            db_path: PathBuf::from(":memory:"),
        };
        db.initialize_schema().expect("Failed to initialize database schema");
        db
    }

    fn initialize_schema(&self) -> Result<()> {
        let conn = self.conn.lock().unwrap();
        conn.execute_batch(
            r#"
            CREATE TABLE IF NOT EXISTS clients (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                name TEXT NOT NULL,
                company TEXT DEFAULT '',
                email TEXT DEFAULT '',
                phone TEXT DEFAULT '',
                hourly_rate REAL DEFAULT 0.0,
                currency TEXT DEFAULT 'USD',
                notes TEXT DEFAULT '',
                is_active INTEGER DEFAULT 1,
                created_at TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS jobs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                client_id INTEGER NOT NULL,
                title TEXT NOT NULL,
                description TEXT DEFAULT '',
                status TEXT DEFAULT 'Pendiente',
                priority TEXT DEFAULT 'Media',
                hourly_rate REAL,
                estimated_hours REAL DEFAULT 0.0,
                due_date TEXT,
                created_at TEXT NOT NULL,
                updated_at TEXT NOT NULL,
                FOREIGN KEY (client_id) REFERENCES clients(id) ON DELETE CASCADE
            );

            CREATE TABLE IF NOT EXISTS time_logs (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                job_id INTEGER NOT NULL,
                start_time TEXT NOT NULL,
                end_time TEXT,
                duration_seconds INTEGER DEFAULT 0,
                description TEXT DEFAULT '',
                is_billable INTEGER DEFAULT 1,
                hourly_rate REAL DEFAULT 0.0,
                created_at TEXT NOT NULL,
                FOREIGN KEY (job_id) REFERENCES jobs(id) ON DELETE CASCADE
            );

            CREATE INDEX IF NOT EXISTS idx_jobs_client ON jobs(client_id);
            CREATE INDEX IF NOT EXISTS idx_timelogs_job ON time_logs(job_id);
            CREATE INDEX IF NOT EXISTS idx_timelogs_start ON time_logs(start_time);
            "#,
        )?;
        Ok(())
    }

    pub fn seed_initial_data(&self) -> Result<()> {
        let conn = self.conn.lock().unwrap();
        let count: i64 = conn.query_row("SELECT COUNT(*) FROM clients", [], |row| row.get(0))?;
        if count > 0 {
            return Ok(());
        }

        conn.execute_batch(
            r#"
            INSERT INTO clients (name, company, email, phone, hourly_rate, currency, notes, is_active, created_at)
            VALUES 
            ('Carlos Mendoza', 'Acme Innovación SL', 'carlos@acme.com', '+34 600 123 456', 65.0, 'EUR', 'Cliente recurrente desarrollo web', 1, datetime('now')),
            ('Laura Gómez', 'Nexis Solutions', 'lgomez@nexis.io', '+1 555 0192', 80.0, 'USD', 'Consultoría de software cloud', 1, datetime('now')),
            ('Martín Silva', 'DesignLab Studio', 'martin@designlab.co', '+54 9 11 4455 6677', 50.0, 'USD', 'Diseño de UI y sistemas visuales', 1, datetime('now'));

            INSERT INTO jobs (client_id, title, description, status, priority, hourly_rate, estimated_hours, due_date, created_at, updated_at)
            VALUES
            (1, 'Rediseño Portal Corporativo', 'Migración completa de frontend y optimización de carga', 'En progreso', 'Alta', 65.0, 40.0, date('now', '+15 days'), datetime('now'), datetime('now')),
            (1, 'Integración Pasarela Pagos', 'Módulo de checkout con Stripe y Webhooks', 'Pendiente', 'Media', 65.0, 15.0, date('now', '+30 days'), datetime('now'), datetime('now')),
            (2, 'Auditoría Arquitectura Cloud', 'Revisión de escalabilidad y perfiles de seguridad', 'En revisión', 'Urgente', 80.0, 20.0, date('now', '+5 days'), datetime('now'), datetime('now')),
            (3, 'Kit de Componentes WinUI 3', 'Diseño y maquetación de controles XAML Fluent', 'Entregado', 'Media', 50.0, 25.0, date('now', '-2 days'), datetime('now'), datetime('now'));

            INSERT INTO time_logs (job_id, start_time, end_time, duration_seconds, description, is_billable, hourly_rate, created_at)
            VALUES
            (1, datetime('now', '-2 hours'), datetime('now'), 7200, 'Maquetación de páginas y ajustes responsive', 1, 65.0, datetime('now')),
            (1, datetime('now', '-1 day', '-3 hours'), datetime('now', '-1 day'), 10800, 'Desarrollo de navegación principal', 1, 65.0, datetime('now', '-1 day')),
            (3, datetime('now', '-2 days', '-4 hours'), datetime('now', '-2 days'), 14400, 'Auditoría de contenedores e infraestructura', 1, 80.0, datetime('now', '-2 days')),
            (4, datetime('now', '-3 days', '-5 hours'), datetime('now', '-3 days'), 18000, 'Exportación de paleta de colores y estilos XAML', 1, 50.0, datetime('now', '-3 days'));
            "#,
        )?;

        Ok(())
    }

    // ================= CLIENTES =================

    pub fn get_all_clients(&self, search: Option<String>, only_active: bool) -> Result<Vec<Client>> {
        let conn = self.conn.lock().unwrap();
        let mut query = String::from(
            r#"
            SELECT c.id, c.name, c.company, c.email, c.phone, c.hourly_rate, c.currency, c.notes, c.is_active, c.created_at,
                   COALESCE((SELECT COUNT(*) FROM jobs j WHERE j.client_id = c.id AND j.status != 'Facturado'), 0) AS active_jobs,
                   COALESCE((SELECT SUM((t.duration_seconds / 3600.0) * t.hourly_rate) 
                             FROM time_logs t 
                             JOIN jobs j ON t.job_id = j.id 
                             WHERE j.client_id = c.id AND t.is_billable = 1), 0.0) AS total_earned
            FROM clients c
            WHERE 1=1
            "#,
        );

        if only_active {
            query.push_str(" AND c.is_active = 1 ");
        }

        let search_pattern = search.filter(|s| !s.trim().is_empty()).map(|s| format!("%{}%", s.trim()));
        if search_pattern.is_some() {
            query.push_str(" AND (c.name LIKE ?1 OR c.company LIKE ?1 OR c.email LIKE ?1) ");
        }
        query.push_str(" ORDER BY c.name ASC;");

        let mut stmt = conn.prepare(&query)?;
        let map_row = |row: &rusqlite::Row| -> Result<Client> {
            let hourly_rate: f64 = row.get(5)?;
            let currency: String = row.get(6)?;
            let total_earned: f64 = row.get(11)?;
            Ok(Client {
                id: row.get(0)?,
                name: row.get(1)?,
                company: row.get(2)?,
                email: row.get(3)?,
                phone: row.get(4)?,
                hourly_rate,
                currency: currency.clone(),
                notes: row.get(7)?,
                is_active: row.get::<_, i64>(8)? == 1,
                created_at: row.get(9)?,
                active_jobs_count: row.get(10)?,
                total_earned: (total_earned * 100.0).round() / 100.0,
                display_rate: Client::format_display_rate(hourly_rate, &currency),
            })
        };

        let mut clients = Vec::new();
        if let Some(ref pattern) = search_pattern {
            let mut rows = stmt.query(params![pattern])?;
            while let Some(row) = rows.next()? {
                clients.push(map_row(row)?);
            }
        } else {
            let mut rows = stmt.query([])?;
            while let Some(row) = rows.next()? {
                clients.push(map_row(row)?);
            }
        }
        Ok(clients)
    }

    pub fn add_client(&self, input: CreateClientInput) -> Result<i64> {
        let conn = self.conn.lock().unwrap();
        let now = Local::now().format("%Y-%m-%dT%H:%M:%S").to_string();
        let currency = input.currency.unwrap_or_else(|| "USD".to_string());
        let is_active = if input.is_active.unwrap_or(true) { 1 } else { 0 };

        conn.execute(
            r#"
            INSERT INTO clients (name, company, email, phone, hourly_rate, currency, notes, is_active, created_at)
            VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9);
            "#,
            params![
                input.name,
                input.company.unwrap_or_default(),
                input.email.unwrap_or_default(),
                input.phone.unwrap_or_default(),
                input.hourly_rate,
                currency,
                input.notes.unwrap_or_default(),
                is_active,
                now,
            ],
        )?;

        Ok(conn.last_insert_rowid())
    }

    pub fn update_client(&self, input: UpdateClientInput) -> Result<()> {
        let conn = self.conn.lock().unwrap();
        let is_active = if input.is_active { 1 } else { 0 };
        conn.execute(
            r#"
            UPDATE clients 
            SET name = ?1, company = ?2, email = ?3, phone = ?4,
                hourly_rate = ?5, currency = ?6, notes = ?7, is_active = ?8
            WHERE id = ?9;
            "#,
            params![
                input.name,
                input.company.unwrap_or_default(),
                input.email.unwrap_or_default(),
                input.phone.unwrap_or_default(),
                input.hourly_rate,
                input.currency.unwrap_or_else(|| "USD".to_string()),
                input.notes.unwrap_or_default(),
                is_active,
                input.id,
            ],
        )?;
        Ok(())
    }

    pub fn delete_client(&self, id: i64) -> Result<()> {
        let conn = self.conn.lock().unwrap();
        conn.execute("DELETE FROM clients WHERE id = ?1;", params![id])?;
        Ok(())
    }

    // ================= TRABAJOS (JOBS) =================

    pub fn get_all_jobs(&self, status: Option<String>, client_id: Option<i64>, search: Option<String>) -> Result<Vec<Job>> {
        let conn = self.conn.lock().unwrap();
        let mut query = String::from(
            r#"
            SELECT j.id, j.client_id, c.name AS client_name, j.title, j.description, j.status, j.priority,
                   j.hourly_rate, j.estimated_hours, j.due_date, j.created_at, j.updated_at,
                   COALESCE((SELECT SUM(t.duration_seconds / 3600.0) FROM time_logs t WHERE t.job_id = j.id), 0.0) AS logged_hours,
                   COALESCE((SELECT SUM((t.duration_seconds / 3600.0) * t.hourly_rate) FROM time_logs t WHERE t.job_id = j.id AND t.is_billable = 1), 0.0) AS total_earned
            FROM jobs j
            JOIN clients c ON j.client_id = c.id
            WHERE 1=1
            "#,
        );

        let filter_status = status.filter(|s| !s.is_empty() && s != "Todos");
        if let Some(ref st) = filter_status {
            query.push_str(&format!(" AND j.status = '{}' ", st.replace('\'', "''")));
        }

        if let Some(cid) = client_id.filter(|&id| id > 0) {
            query.push_str(&format!(" AND j.client_id = {} ", cid));
        }

        let search_pattern = search.filter(|s| !s.trim().is_empty()).map(|s| format!("%{}%", s.trim()));
        if let Some(ref pattern) = search_pattern {
            query.push_str(&format!(" AND (j.title LIKE '{}' OR c.name LIKE '{}' OR j.description LIKE '{}') ",
                pattern.replace('\'', "''"), pattern.replace('\'', "''"), pattern.replace('\'', "''")));
        }

        query.push_str(" ORDER BY j.updated_at DESC;");

        let mut stmt = conn.prepare(&query)?;
        let rows = stmt.query_map([], |row| {
            let estimated_hours: f64 = row.get(8)?;
            let logged_hours: f64 = row.get(12)?;
            let logged_rounded = (logged_hours * 100.0).round() / 100.0;
            let total_earned: f64 = row.get(13)?;
            let progress = Job::calculate_progress(estimated_hours, logged_rounded);

            Ok(Job {
                id: row.get(0)?,
                client_id: row.get(1)?,
                client_name: row.get(2)?,
                title: row.get(3)?,
                description: row.get(4)?,
                status: row.get(5)?,
                priority: row.get(6)?,
                hourly_rate: row.get(7)?,
                estimated_hours,
                due_date: row.get(9)?,
                created_at: row.get(10)?,
                updated_at: row.get(11)?,
                logged_hours: logged_rounded,
                total_earned: (total_earned * 100.0).round() / 100.0,
                progress_percentage: (progress * 10.0).round() / 10.0,
            })
        })?;

        let mut jobs = Vec::new();
        for job in rows {
            jobs.push(job?);
        }
        Ok(jobs)
    }

    pub fn add_job(&self, input: CreateJobInput) -> Result<i64> {
        let conn = self.conn.lock().unwrap();
        let now = Local::now().format("%Y-%m-%dT%H:%M:%S").to_string();
        let status = input.status.unwrap_or_else(|| "Pendiente".to_string());
        let priority = input.priority.unwrap_or_else(|| "Media".to_string());
        let est_hours = input.estimated_hours.unwrap_or(0.0);

        conn.execute(
            r#"
            INSERT INTO jobs (client_id, title, description, status, priority, hourly_rate, estimated_hours, due_date, created_at, updated_at)
            VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8, ?9, ?10);
            "#,
            params![
                input.client_id,
                input.title,
                input.description.unwrap_or_default(),
                status,
                priority,
                input.hourly_rate,
                est_hours,
                input.due_date,
                now,
                now,
            ],
        )?;

        Ok(conn.last_insert_rowid())
    }

    pub fn update_job(&self, input: UpdateJobInput) -> Result<()> {
        let conn = self.conn.lock().unwrap();
        let now = Local::now().format("%Y-%m-%dT%H:%M:%S").to_string();
        conn.execute(
            r#"
            UPDATE jobs 
            SET client_id = ?1, title = ?2, description = ?3,
                status = ?4, priority = ?5, hourly_rate = ?6,
                estimated_hours = ?7, due_date = ?8, updated_at = ?9
            WHERE id = ?10;
            "#,
            params![
                input.client_id,
                input.title,
                input.description.unwrap_or_default(),
                input.status,
                input.priority,
                input.hourly_rate,
                input.estimated_hours,
                input.due_date,
                now,
                input.id,
            ],
        )?;
        Ok(())
    }

    pub fn update_job_status(&self, job_id: i64, status: &str) -> Result<()> {
        let conn = self.conn.lock().unwrap();
        let now = Local::now().format("%Y-%m-%dT%H:%M:%S").to_string();
        conn.execute(
            "UPDATE jobs SET status = ?1, updated_at = ?2 WHERE id = ?3;",
            params![status, now, job_id],
        )?;
        Ok(())
    }

    pub fn delete_job(&self, id: i64) -> Result<()> {
        let conn = self.conn.lock().unwrap();
        conn.execute("DELETE FROM jobs WHERE id = ?1;", params![id])?;
        Ok(())
    }

    // ================= ENTRADAS DE TIEMPO (TIME LOGS) =================

    pub fn get_all_logs(
        &self,
        job_id: Option<i64>,
        client_id: Option<i64>,
        from_date: Option<String>,
        to_date: Option<String>,
    ) -> Result<Vec<TimeLog>> {
        let conn = self.conn.lock().unwrap();
        let mut query = String::from(
            r#"
            SELECT t.id, t.job_id, j.title AS job_title, c.id AS client_id, c.name AS client_name,
                   t.start_time, t.end_time, t.duration_seconds, t.description, t.is_billable,
                   t.hourly_rate, t.created_at
            FROM time_logs t
            JOIN jobs j ON t.job_id = j.id
            JOIN clients c ON j.client_id = c.id
            WHERE 1=1
            "#,
        );

        if let Some(jid) = job_id.filter(|&id| id > 0) {
            query.push_str(&format!(" AND t.job_id = {} ", jid));
        }

        if let Some(cid) = client_id.filter(|&id| id > 0) {
            query.push_str(&format!(" AND c.id = {} ", cid));
        }

        if let Some(ref from) = from_date.filter(|d| !d.is_empty()) {
            query.push_str(&format!(" AND date(t.start_time) >= date('{}') ", from.replace('\'', "''")));
        }

        if let Some(ref to) = to_date.filter(|d| !d.is_empty()) {
            query.push_str(&format!(" AND date(t.start_time) <= date('{}') ", to.replace('\'', "''")));
        }

        query.push_str(" ORDER BY t.start_time DESC;");

        let mut stmt = conn.prepare(&query)?;
        let rows = stmt.query_map([], |row| {
            let duration_seconds: i64 = row.get(7)?;
            let hourly_rate: f64 = row.get(10)?;
            let is_billable = row.get::<_, i64>(9)? == 1;

            Ok(TimeLog {
                id: row.get(0)?,
                job_id: row.get(1)?,
                job_title: row.get(2)?,
                client_id: row.get(3)?,
                client_name: row.get(4)?,
                start_time: row.get(5)?,
                end_time: row.get(6)?,
                duration_seconds,
                description: row.get(8)?,
                is_billable,
                hourly_rate,
                created_at: row.get(11)?,
                hours: TimeLog::calculate_hours(duration_seconds),
                amount: TimeLog::calculate_amount(duration_seconds, hourly_rate, is_billable),
                formatted_duration: TimeLog::format_duration(duration_seconds),
            })
        })?;

        let mut logs = Vec::new();
        for log in rows {
            logs.push(log?);
        }
        Ok(logs)
    }

    pub fn add_log(&self, input: CreateTimeLogInput) -> Result<i64> {
        let conn = self.conn.lock().unwrap();
        let now = Local::now().format("%Y-%m-%dT%H:%M:%S").to_string();
        let start_time = input.start_time.unwrap_or_else(|| now.clone());
        let is_billable = if input.is_billable.unwrap_or(true) { 1 } else { 0 };

        // Si no se proporcionó hourly_rate, tomar el del Job o del Client
        let rate = match input.hourly_rate {
            Some(r) => r,
            None => {
                let job_rate: Option<f64> = conn.query_row(
                    "SELECT COALESCE(j.hourly_rate, c.hourly_rate) FROM jobs j JOIN clients c ON j.client_id = c.id WHERE j.id = ?1",
                    params![input.job_id],
                    |row| row.get(0),
                ).unwrap_or(Some(0.0));
                job_rate.unwrap_or(0.0)
            }
        };

        conn.execute(
            r#"
            INSERT INTO time_logs (job_id, start_time, end_time, duration_seconds, description, is_billable, hourly_rate, created_at)
            VALUES (?1, ?2, ?3, ?4, ?5, ?6, ?7, ?8);
            "#,
            params![
                input.job_id,
                start_time,
                input.end_time,
                input.duration_seconds,
                input.description.unwrap_or_default(),
                is_billable,
                rate,
                now,
            ],
        )?;

        Ok(conn.last_insert_rowid())
    }

    pub fn delete_log(&self, id: i64) -> Result<()> {
        let conn = self.conn.lock().unwrap();
        conn.execute("DELETE FROM time_logs WHERE id = ?1;", params![id])?;
        Ok(())
    }

    // ================= DASHBOARD METRICS =================

    pub fn get_dashboard_metrics(&self) -> Result<DashboardMetrics> {
        let conn = self.conn.lock().unwrap();

        // 1. Horas y facturación hoy
        let (hours_today, billable_today): (f64, f64) = conn.query_row(
            r#"
            SELECT 
                COALESCE(SUM(duration_seconds / 3600.0), 0.0) AS hours_today,
                COALESCE(SUM((duration_seconds / 3600.0) * hourly_rate), 0.0) AS billable_today
            FROM time_logs 
            WHERE date(start_time) = date('now', 'localtime') AND is_billable = 1;
            "#,
            [],
            |row| Ok((row.get(0)?, row.get(1)?)),
        )?;

        // 2. Horas y facturación mes actual
        let (hours_this_month, billable_this_month): (f64, f64) = conn.query_row(
            r#"
            SELECT 
                COALESCE(SUM(duration_seconds / 3600.0), 0.0) AS hours_month,
                COALESCE(SUM((duration_seconds / 3600.0) * hourly_rate), 0.0) AS billable_month
            FROM time_logs 
            WHERE strftime('%Y-%m', start_time) = strftime('%Y-%m', 'now', 'localtime') AND is_billable = 1;
            "#,
            [],
            |row| Ok((row.get(0)?, row.get(1)?)),
        )?;

        // 3. Trabajos activos y clientes
        let active_jobs_count: i64 = conn.query_row(
            "SELECT COUNT(*) FROM jobs WHERE status NOT IN ('Entregado', 'Facturado');",
            [],
            |row| row.get(0),
        )?;

        let total_clients_count: i64 = conn.query_row(
            "SELECT COUNT(*) FROM clients WHERE is_active = 1;",
            [],
            |row| row.get(0),
        )?;

        // 4. Últimos 7 días
        let mut last_7_days = Vec::new();
        let today = Local::now().date_naive();
        for i in (0..7).rev() {
            let day = today - Duration::days(i);
            let day_str = day.format("%Y-%m-%d").to_string();
            let day_label = day.format("%a %d").to_string();

            let hours: f64 = conn.query_row(
                "SELECT COALESCE(SUM(duration_seconds / 3600.0), 0.0) FROM time_logs WHERE date(start_time) = ?1;",
                params![day_str],
                |row| row.get(0),
            )?;

            let h_rounded = (hours * 10.0).round() / 10.0;
            let bar_height = (h_rounded * 20.0).max(10.0).min(150.0);

            last_7_days.push(DailyHourStat {
                day_name: day_label,
                hours: h_rounded,
                formatted_hours: format!("{:.1} h", h_rounded),
                bar_height,
            });
        }

        Ok(DashboardMetrics {
            hours_today: (hours_today * 10.0).round() / 10.0,
            billable_today: (billable_today * 100.0).round() / 100.0,
            hours_this_month: (hours_this_month * 10.0).round() / 10.0,
            billable_this_month: (billable_this_month * 100.0).round() / 100.0,
            active_jobs_count,
            total_clients_count,
            last_7_days,
        })
    }
}

