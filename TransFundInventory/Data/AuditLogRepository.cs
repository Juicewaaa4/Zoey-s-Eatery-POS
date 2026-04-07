using Microsoft.Data.Sqlite;
using TransFundInventory.Models;

namespace TransFundInventory.Data
{
    public class AuditLogRepository
    {
        public void Log(int userId, string action, string details)
        {
            try
            {
                using var connection = DatabaseHelper.GetConnection();
                connection.Open();

                var cmd = connection.CreateCommand();
                cmd.CommandText = @"INSERT INTO AuditLogs (UserId, Action, Details, Timestamp)
                    VALUES (@userId, @action, @details, @timestamp)";
                cmd.Parameters.AddWithValue("@userId", userId);
                cmd.Parameters.AddWithValue("@action", action);
                cmd.Parameters.AddWithValue("@details", details);
                cmd.Parameters.AddWithValue("@timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.ExecuteNonQuery();
            }
            catch { /* Don't let audit logging break the app */ }
        }

        public List<AuditLog> GetAll(DateTime? dateFrom = null, DateTime? dateTo = null, string? actionFilter = null)
        {
            var logs = new List<AuditLog>();
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            var sql = @"SELECT a.Id, a.UserId, a.Action, a.Details, a.Timestamp, u.FullName AS UserName 
                FROM AuditLogs a 
                LEFT JOIN Users u ON a.UserId = u.Id 
                WHERE 1=1";

            if (dateFrom.HasValue)
            {
                sql += " AND a.Timestamp >= @dateFrom";
                cmd.Parameters.AddWithValue("@dateFrom", dateFrom.Value.ToString("yyyy-MM-dd 00:00:00"));
            }
            if (dateTo.HasValue)
            {
                sql += " AND a.Timestamp <= @dateTo";
                cmd.Parameters.AddWithValue("@dateTo", dateTo.Value.ToString("yyyy-MM-dd 23:59:59"));
            }
            if (!string.IsNullOrWhiteSpace(actionFilter) && actionFilter != "All")
            {
                sql += " AND a.Action = @action";
                cmd.Parameters.AddWithValue("@action", actionFilter);
            }

            sql += " ORDER BY a.Timestamp DESC";
            cmd.CommandText = sql;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                logs.Add(new AuditLog
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    UserName = reader.IsDBNull(reader.GetOrdinal("UserName"))
                        ? $"User #{reader.GetInt32(reader.GetOrdinal("UserId"))}" 
                        : reader.GetString(reader.GetOrdinal("UserName")),
                    Action = reader.GetString(reader.GetOrdinal("Action")),
                    Details = reader.IsDBNull(reader.GetOrdinal("Details"))
                        ? string.Empty : reader.GetString(reader.GetOrdinal("Details")),
                    Timestamp = reader.GetString(reader.GetOrdinal("Timestamp"))
                });
            }
            return logs;
        }

        public List<string> GetDistinctActions()
        {
            var actions = new List<string> { "All" };
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT DISTINCT Action FROM AuditLogs ORDER BY Action";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                actions.Add(reader.GetString(0));
            }
            return actions;
        }
    }
}
