using System;
using System.Collections.Generic;
using Microsoft.Data.Sqlite;
using TimeTrackingStudio.WinUI.Models;

namespace TimeTrackingStudio.WinUI.Services;

/// <summary>
/// Servicio de lógica de negocio y persistencia para la entidad Cliente.
/// </summary>
public class ClientService
{
    private readonly DatabaseService _db;

    public ClientService(DatabaseService? db = null)
    {
        _db = db ?? DatabaseService.Instance;
    }

    public List<Client> GetAllClients(string? searchQuery = null, bool onlyActive = false)
    {
        var list = new List<Client>();
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();

        var query = @"
            SELECT c.*, 
                   COALESCE((SELECT COUNT(*) FROM jobs j WHERE j.client_id = c.id AND j.status != 'Facturado'), 0) AS active_jobs,
                   COALESCE((SELECT SUM((t.duration_seconds / 3600.0) * t.hourly_rate) 
                             FROM time_logs t 
                             JOIN jobs j ON t.job_id = j.id 
                             WHERE j.client_id = c.id AND t.is_billable = 1), 0.0) AS total_earned
            FROM clients c
            WHERE 1=1 ";

        if (onlyActive)
        {
            query += " AND c.is_active = 1 ";
        }

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            query += " AND (c.name LIKE @search OR c.company LIKE @search OR c.email LIKE @search) ";
            cmd.Parameters.AddWithValue("@search", $"%{searchQuery.Trim()}%");
        }

        query += " ORDER BY c.name ASC;";
        cmd.CommandText = query;

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            list.Add(new Client
            {
                Id = reader.GetInt32(reader.GetOrdinal("id")),
                Name = reader.GetString(reader.GetOrdinal("name")),
                Company = reader.IsDBNull(reader.GetOrdinal("company")) ? "" : reader.GetString(reader.GetOrdinal("company")),
                Email = reader.IsDBNull(reader.GetOrdinal("email")) ? "" : reader.GetString(reader.GetOrdinal("email")),
                Phone = reader.IsDBNull(reader.GetOrdinal("phone")) ? "" : reader.GetString(reader.GetOrdinal("phone")),
                HourlyRate = reader.GetDouble(reader.GetOrdinal("hourly_rate")),
                Currency = reader.GetString(reader.GetOrdinal("currency")),
                Notes = reader.IsDBNull(reader.GetOrdinal("notes")) ? "" : reader.GetString(reader.GetOrdinal("notes")),
                IsActive = reader.GetInt32(reader.GetOrdinal("is_active")) == 1,
                CreatedAt = reader.GetString(reader.GetOrdinal("created_at")),
                ActiveJobsCount = Convert.ToInt32(reader["active_jobs"]),
                TotalEarned = Math.Round(Convert.ToDouble(reader["total_earned"]), 2)
            });
        }
        return list;
    }

    public int AddClient(Client c)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            INSERT INTO clients (name, company, email, phone, hourly_rate, currency, notes, is_active, created_at)
            VALUES (@name, @company, @email, @phone, @rate, @curr, @notes, @active, @created);
            SELECT last_insert_rowid();";
        cmd.Parameters.AddWithValue("@name", c.Name);
        cmd.Parameters.AddWithValue("@company", c.Company);
        cmd.Parameters.AddWithValue("@email", c.Email);
        cmd.Parameters.AddWithValue("@phone", c.Phone);
        cmd.Parameters.AddWithValue("@rate", c.HourlyRate);
        cmd.Parameters.AddWithValue("@curr", c.Currency);
        cmd.Parameters.AddWithValue("@notes", c.Notes);
        cmd.Parameters.AddWithValue("@active", c.IsActive ? 1 : 0);
        cmd.Parameters.AddWithValue("@created", DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss"));

        var newId = Convert.ToInt32(cmd.ExecuteScalar());
        c.Id = newId;
        return newId;
    }

    public void UpdateClient(Client c)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            UPDATE clients 
            SET name = @name, company = @company, email = @email, phone = @phone,
                hourly_rate = @rate, currency = @curr, notes = @notes, is_active = @active
            WHERE id = @id;";
        cmd.Parameters.AddWithValue("@id", c.Id);
        cmd.Parameters.AddWithValue("@name", c.Name);
        cmd.Parameters.AddWithValue("@company", c.Company);
        cmd.Parameters.AddWithValue("@email", c.Email);
        cmd.Parameters.AddWithValue("@phone", c.Phone);
        cmd.Parameters.AddWithValue("@rate", c.HourlyRate);
        cmd.Parameters.AddWithValue("@curr", c.Currency);
        cmd.Parameters.AddWithValue("@notes", c.Notes);
        cmd.Parameters.AddWithValue("@active", c.IsActive ? 1 : 0);
        cmd.ExecuteNonQuery();
    }

    public void DeleteClient(int id)
    {
        using var conn = _db.CreateConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM clients WHERE id = @id;";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.ExecuteNonQuery();
    }
}
