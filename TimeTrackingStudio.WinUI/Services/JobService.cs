using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using TimeTrackingStudio.WinUI.Models;

namespace TimeTrackingStudio.WinUI.Services;

/// <summary>
/// Servicio de gestión de proyectos y tareas para clientes.
/// </summary>
public class JobService
{
    private readonly DatabaseService _db;

    public JobService(DatabaseService? db = null)
    {
        _db = db ?? DatabaseService.Instance;
    }

    public List<Job> GetAllJobs(string? statusFilter = null, int? clientId = null, string? searchQuery = null)
    {
        var list = new List<Job>();
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();

        var query = @"
            SELECT j.*, c.name AS client_name,
                   COALESCE((SELECT SUM(t.duration_seconds / 3600.0) FROM time_logs t WHERE t.job_id = j.id), 0.0) AS logged_hours,
                   COALESCE((SELECT SUM((t.duration_seconds / 3600.0) * t.hourly_rate) FROM time_logs t WHERE t.job_id = j.id AND t.is_billable = 1), 0.0) AS total_earned
            FROM jobs j
            JOIN clients c ON j.client_id = c.id
            WHERE 1=1 ";

        if (!string.IsNullOrWhiteSpace(statusFilter) && statusFilter != "Todos")
        {
            query += " AND j.status = @status ";
            cmd.Parameters.AddWithValue("@status", statusFilter);
        }

        if (clientId.HasValue && clientId.Value > 0)
        {
            query += " AND j.client_id = @clientId ";
            cmd.Parameters.AddWithValue("@clientId", clientId.Value);
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query += " AND (j.title LIKE @search OR c.name LIKE @search OR j.description LIKE @search) ";
            cmd.Parameters.AddWithValue("@search", $"%{searchQuery.Trim()}%");
        }

        query += " ORDER BY j.updated_at DESC;";
        cmd.CommandText = query;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Job
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                ClientId = reader.GetInt32(reader.GetOrdinal("client_id")),
                ClientName = reader.GetString(reader.GetOrdinal("client_name")),
                Title = reader.GetString(reader.GetOrdinal("title")),
                Description = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString(reader.GetOrdinal("description")),
                Status = reader.GetString(reader.GetOrdinal("status")),
                Priority = reader.GetString(reader.GetOrdinal("priority")),
                HourlyRate = reader.IsDBNull(reader.GetOrdinal("hourly_rate")) ? null : reader.GetDouble(reader.GetOrdinal("hourly_rate")),
                EstimatedHours = reader.GetDouble(reader.GetOrdinal("estimated_hours")),
                DueDate = reader.IsDBNull(reader.GetOrdinal("due_date")) ? null : reader.GetString(reader.GetOrdinal("due_date")),
                CreatedAt = reader.GetString(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetString(reader.GetOrdinal("updated_at")),
                LoggedHours = Math.Round(Convert.ToDouble(reader["logged_hours"]), 2),
                TotalEarned = Math.Round(Convert.ToDouble(reader["total_earned"]), 2)
            });
        }
        return list;
    }

    public int AddJob(Job j)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO jobs (client_id, title, description, status, priority, hourly_rate, estimated_hours, due_date, created_at, updated_at)
            VALUES (@clientId, @title, @desc, @status, @priority, @rate, @estHours, @dueDate, @created, @updated);
            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@clientId", j.ClientId);
        cmd.Parameters.AddWithValue("@title", j.Title);
        cmd.Parameters.AddWithValue("@desc", j.Description);
        cmd.Parameters.AddWithValue("@status", j.Status);
        cmd.Parameters.AddWithValue("@priority", j.Priority);
        cmd.Parameters.AddWithValue("@rate", (object?)j.HourlyRate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@estHours", j.EstimatedHours);
        cmd.Parameters.AddWithValue("@dueDate", (object?)j.DueDate ?? DBNull.Value);
        var now = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss");
        cmd.Parameters.AddWithValue("@created", now);
        cmd.Parameters.AddWithValue("@updated", now);

        var newId = Convert.ToInt32(cmd.ExecuteScalar());
        j.Id = newId;
        return newId;
    }

    public void UpdateJob(Job j)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE jobs 
            SET client_id = @clientId, title = @title, description = @desc,
                status = @status, priority = @priority, hourly_rate = @rate,
                estimated_hours = @estHours, due_date = @dueDate, updated_at = @updated
            WHERE id = @id;";
        cmd.Parameters.AddWithValue("@id", j.Id);
        cmd.Parameters.AddWithValue("@clientId", j.ClientId);
        cmd.Parameters.AddWithValue("@title", j.Title);
        cmd.Parameters.AddWithValue("@desc", j.Description);
        cmd.Parameters.AddWithValue("@status", j.Status);
        cmd.Parameters.AddWithValue("@priority", j.Priority);
        cmd.Parameters.AddWithValue("@rate", (object?)j.HourlyRate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@estHours", j.EstimatedHours);
        cmd.Parameters.AddWithValue("@dueDate", (object?)j.DueDate ?? DBNull.Value);
        cmd.Parameters.AddWithValue("@updated", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
        cmd.ExecuteNonQuery();
    }

    public void UpdateStatus(int jobId, string newStatus)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE jobs SET status = @status, updated_at = @updated WHERE id = @id;";
        cmd.Parameters.AddWithValue("@id", jobId);
        cmd.Parameters.AddWithValue("@status", newStatus);
        cmd.Parameters.AddWithValue("@updated", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));
        cmd.ExecuteNonQuery();
    }

    public void DeleteJob(int id)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM jobs WHERE id = @id;";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }
}
