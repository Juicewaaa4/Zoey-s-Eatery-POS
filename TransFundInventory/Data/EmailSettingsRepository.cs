using Microsoft.Data.Sqlite;
using TransFundInventory.Models;

namespace TransFundInventory.Data
{
    public class EmailSettingsRepository
    {
        public EmailSettings? GetSettings()
        {
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM EmailSettings LIMIT 1";

            try
            {
                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    return new EmailSettings
                    {
                        Id = reader.GetInt32(reader.GetOrdinal("Id")),
                        ResendApiKey = reader.GetString(reader.GetOrdinal("ResendApiKey")),
                        OwnerEmail = reader.GetString(reader.GetOrdinal("OwnerEmail")),
                        OwnerName = reader.GetString(reader.GetOrdinal("OwnerName")),
                        NotifyOnLogin = reader.GetInt32(reader.GetOrdinal("NotifyOnLogin")) == 1,
                        NotifyOnLowStock = reader.GetInt32(reader.GetOrdinal("NotifyOnLowStock")) == 1,
                        IsEnabled = reader.GetInt32(reader.GetOrdinal("IsEnabled")) == 1
                    };
                }
            }
            catch (SqliteException) { /* Table may have old schema */ }

            return null;
        }

        public void SaveSettings(EmailSettings settings)
        {
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            // Check if settings exist
            var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM EmailSettings";
            var count = Convert.ToInt64(checkCmd.ExecuteScalar());

            var cmd = connection.CreateCommand();
            if (count == 0)
            {
                cmd.CommandText = @"
                    INSERT INTO EmailSettings (ResendApiKey, OwnerEmail, OwnerName, NotifyOnLogin, NotifyOnLowStock, IsEnabled)
                    VALUES (@apiKey, @ownerEmail, @ownerName, @notifyOnLogin, @notifyOnLowStock, @isEnabled)";
            }
            else
            {
                cmd.CommandText = @"
                    UPDATE EmailSettings SET 
                        ResendApiKey = @apiKey,
                        OwnerEmail = @ownerEmail,
                        OwnerName = @ownerName,
                        NotifyOnLogin = @notifyOnLogin,
                        NotifyOnLowStock = @notifyOnLowStock,
                        IsEnabled = @isEnabled";
            }

            cmd.Parameters.AddWithValue("@apiKey", settings.ResendApiKey);
            cmd.Parameters.AddWithValue("@ownerEmail", settings.OwnerEmail);
            cmd.Parameters.AddWithValue("@ownerName", settings.OwnerName);
            cmd.Parameters.AddWithValue("@notifyOnLogin", settings.NotifyOnLogin ? 1 : 0);
            cmd.Parameters.AddWithValue("@notifyOnLowStock", settings.NotifyOnLowStock ? 1 : 0);
            cmd.Parameters.AddWithValue("@isEnabled", settings.IsEnabled ? 1 : 0);
            cmd.ExecuteNonQuery();
        }
    }
}
