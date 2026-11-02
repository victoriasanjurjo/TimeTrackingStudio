using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using Microsoft.UI.Dispatching;
using TimeTrackingStudio.WinUI.Models;

namespace TimeTrackingStudio.WinUI.Services;

public enum TimerState
{
    Stopped,
    Running,
    Paused
}

/// <summary>
/// Servicio de cronometraje en vivo y gestión histórica de tiempos trabajados.
/// </summary>
public class TimeService
{
    private static TimeService? _instance;
    public static TimeService Instance => _instance ??= new TimeService();

    private readonly DatabaseService _db;
    private DispatcherQueueTimer? _timer;

    public TimerState State { get; private set; } = TimerState.Stopped;
    public int? CurrentJobId { get; private set; }
    public string CurrentJobTitle { get; private set; } = string.Empty;
    public string CurrentClientName { get; private set; } = string.Empty;
    public double CurrentHourlyRate { get; private set; } = 0.0;
    public string CurrentDescription { get; set; } = string.Empty;
    public DateTime? StartTime { get; private set; }
    public int ElapsedSeconds { get; private set; } = 0;

    public event Action<int>? Tick;
    public event Action<TimerState>? StateChanged;

    public TimeService(DatabaseService? db = null)
    {
        _db = db ?? DatabaseService.Instance;
    }

    public void InitializeDispatcher(DispatcherQueue dispatcher)
    {
        _timer = dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += (s, e) =>
        {
            if (State == TimerState.Running)
            {
                ElapsedSeconds++;
                Tick?.Invoke(ElapsedSeconds);
            }
        };
    }

    public void StartTimer(int jobId, string jobTitle, string clientName, double hourlyRate, string description)
    {
        CurrentJobId = jobId;
        CurrentJobTitle = jobTitle;
        CurrentClientName = clientName;
        CurrentHourlyRate = hourlyRate;
        CurrentDescription = description;
        StartTime = DateTime.Now;
        ElapsedSeconds = 0;
        State = TimerState.Running;

        _timer?.Start();
        StateChanged?.Invoke(State);
        Tick?.Invoke(ElapsedSeconds);
    }

    public void PauseTimer()
    {
        if (State == TimerState.Running)
        {
            State = TimerState.Paused;
            _timer?.Stop();
            StateChanged?.Invoke(State);
        }
    }

    public void ResumeTimer()
    {
        if (State == TimerState.Paused)
        {
            State = TimerState.Running;
            _timer?.Start();
            StateChanged?.Invoke(State);
        }
    }

    public TimeLog? StopAndSave()
    {
        if (CurrentJobId == null || ElapsedSeconds <= 0)
        {
            Discard();
            return null;
        }

        var log = new TimeLog
        {
            JobId = CurrentJobId.Value,
            StartTime = StartTime?.ToString("yyyy-MM-ddTHH:mm:ss") ?? DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
            EndTime = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"),
            DurationSeconds = ElapsedSeconds,
            Description = CurrentDescription,
            IsBillable = true,
            HourlyRate = CurrentHourlyRate,
            CreatedAt = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss")
        };

        AddLog(log);
        Discard();
        return log;
    }

    public void Discard()
    {
        _timer?.Stop();
        State = TimerState.Stopped;
        CurrentJobId = null;
        CurrentJobTitle = string.Empty;
        CurrentClientName = string.Empty;
        CurrentDescription = string.Empty;
        StartTime = null;
        ElapsedSeconds = 0;
        StateChanged?.Invoke(State);
        Tick?.Invoke(0);
    }

    public int AddLog(TimeLog log)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO time_logs (job_id, start_time, end_time, duration_seconds, description, is_billable, hourly_rate, created_at)
            VALUES (@jobId, @start, @end, @sec, @desc, @bill, @rate, @created);
            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@jobId", log.JobId);
        cmd.Parameters.AddWithValue("@start", log.StartTime);
        cmd.Parameters.AddWithValue("@end", (object?)log.EndTime ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@sec", log.DurationSeconds);
        cmd.Parameters.AddWithValue("@desc", log.Description);
        cmd.Parameters.AddWithValue("@bill", log.IsBillable ? 1 : 0);
        cmd.Parameters.AddWithValue("@rate", log.HourlyRate);
        cmd.Parameters.AddWithValue("@created", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));

        var newId = Convert.ToInt32(cmd.ExecuteScalar());
        log.Id = newId;
        return newId;
    }

    public List<TimeLog> GetAllLogs(int? jobId = null, int? clientId = null, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var list = new List<TimeLog>();
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();

        var query = @"
            SELECT t.*, j.title AS job_title, c.id AS client_id, c.name AS client_name
            FROM time_logs t
            JOIN jobs j ON t.job_id = j.id
            JOIN clients c ON j.client_id = c.id
            WHERE 1=1 ";

        if (jobId.HasValue && jobId.Value > 0)
        {
            query += " AND t.job_id = @jobId ";
            cmd.Parameters.AddWithValue("@jobId", jobId.Value);
        }

        if (clientId.HasValue && clientId.Value > 0)
        {
            query += " AND c.id = @clientId ";
            cmd.Parameters.AddWithValue("@clientId", clientId.Value);
        }

        if (fromDate.HasValue)
        {
            query += " AND date(t.start_time) >= date(@fromDate) ";
            cmd.Parameters.AddWithValue("@fromDate", fromDate.Value.ToString("yyyy-MM-dd"));
        }

        if (toDate.HasValue)
        {
            query += " AND date(t.start_time) <= date(@toDate) ";
            cmd.Parameters.AddWithValue("@toDate", toDate.Value.ToString("yyyy-MM-dd"));
        }

        query += " ORDER BY t.start_time DESC;";
        cmd.CommandText = query;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new TimeLog
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                JobId = reader.GetInt32(reader.GetOrdinal("job_id")),
                JobTitle = reader.GetString(reader.GetOrdinal("job_title")),
                ClientId = reader.GetInt32(reader.GetOrdinal("client_id")),
                ClientName = reader.GetString(reader.GetOrdinal("client_name")),
                StartTime = reader.GetString(reader.GetOrdinal("start_time")),
                EndTime = reader.IsDBNull(reader.GetOrdinal("end_time")) ? null : reader.GetString(reader.GetOrdinal("end_time")),
                DurationSeconds = reader.GetInt32(reader.GetOrdinal("duration_seconds")),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString(reader.GetOrdinal("description")),
                IsBillable = reader.GetInt32(reader.GetOrdinal("is_billable")) == 1,
                HourlyRate = reader.GetDouble(reader.GetOrdinal("hourly_rate")),
                CreatedAt = reader.GetString(reader.GetOrdinal("created_at"))
            });
        }
        return list;
    }

    public void DeleteLog(int id)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM time_logs WHERE id = @id;";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }

    public DashboardMetrics GetDashboardMetrics()
    {
        var metrics = new DashboardMetrics();
        using var conn = _db.CreateConnection();

        // 1. Horas y facturación hoy
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT 
                    COALESCE(SUM(duration_seconds / 3600.0), 0.0) AS hours_today,
                    COALESCE(SUM((duration_seconds / 3600.0) * hourly_rate), 0.0) AS billable_today
                FROM time_logs 
                WHERE date(start_time) = date('now', 'localtime') AND is_billable = 1;";
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                metrics.HoursToday = Math.Round(r.GetDouble(0), 1);
                metrics.BillableToday = Math.Round(r.GetDouble(1), 2);
            }
        }

        // 2. Horas y facturación mes actual
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                SELECT 
                    COALESCE(SUM(duration_seconds / 3600.0), 0.0) AS hours_month,
                    COALESCE(SUM((duration_seconds / 3600.0) * hourly_rate), 0.0) AS billable_month
                FROM time_logs 
                WHERE strftime('%Y-%m', start_time) = strftime('%Y-%m', 'now', 'localtime') AND is_billable = 1;";
            using var r = cmd.ExecuteReader();
            if (r.Read())
            {
                metrics.HoursThisMonth = Math.Round(r.GetDouble(0), 1);
                metrics.BillableThisMonth = Math.Round(r.GetDouble(1), 2);
            }
        }

        // 3. Trabajos activos y clientes
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM jobs WHERE status NOT IN ('Entregado', 'Facturado');";
            metrics.ActiveJobsCount = Convert.ToInt32(cmd.ExecuteScalar());
        }

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT COUNT(*) FROM clients WHERE is_active = 1;";
            metrics.TotalClientsCount = Convert.ToInt32(cmd.ExecuteScalar());
        }

        // 4. Últimos 7 días
        for (int i = 6; i >= 0; i--)
        {
            var date = DateTime.Today.AddDays(-i);
            var dateStr = date.ToString("yyyy-MM-dd");
            var dayLabel = date.ToString("ddd d");

            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT COALESCE(SUM(duration_seconds / 3600.0), 0.0) FROM time_logs WHERE date(start_time) = @dt;";
            cmd.Parameters.AddWithValue("@dt", dateStr);
            var h = Math.Round(Convert.ToDouble(cmd.ExecuteScalar()), 1);

            metrics.Last7Days.Add(new DailyHourStat
            {
                DayName = dayLabel,
                Hours = h
            });
        }

        return metrics;
    }
}
