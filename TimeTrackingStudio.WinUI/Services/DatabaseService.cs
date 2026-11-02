using System;
using System.IO;
using Microsoft.Data.Sqlite;

namespace TimeTrackingStudio.WinUI.Services;

/// <summary>
/// Gestor de base de datos SQLite local para Time Tracking Studio.
/// Asegura privacidad absoluta, almacenamiento fuera de la nube e integridad referencial.
/// </summary>
public class DatabaseService
{
    private static DatabaseService? _instance;
    public static DatabaseService Instance => _instance ??= new DatabaseService();

    public string DatabasePath { get; }
    private readonly string _connectionString;

    public DatabaseService()
    {
        // Ubicación local del archivo de base de datos
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var dir = Path.Combine(appData, "TimeTrackingStudio");
        Directory.CreateDirectory(dir);
        DatabasePath = Path.Combine(dir, "timetracker.db");
        _connectionString = $"Data Source={DatabasePath}";

        InitializeDatabase();
        SeedInitialData();
    }

    public SqliteConnection CreateConnection()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "PRAGMA foreign_keys = ON;";
        cmd.ExecuteNonQuery();
        return conn;
    }

    private void InitializeDatabase()
    {
        using var conn = CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
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
        ";
        cmd.ExecuteNonQuery();
    }

    private void SeedInitialData()
    {
        using var conn = CreateConnection();
        using var checkCmd = conn.CreateCommand();
        checkCmd.CommandText = "SELECT COUNT(*) FROM clients;";
        var count = Convert.ToInt64(checkCmd.ExecuteScalar());
        if (count > 0) return;

        using var trans = conn.BeginTransaction();
        using var seedCmd = conn.CreateCommand();
        seedCmd.Transaction = trans;

        var now = DateTime.Now;
        seedCmd.CommandText = @"
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
        ";
        seedCmd.ExecuteNonQuery();
        trans.Commit();
    }
}
