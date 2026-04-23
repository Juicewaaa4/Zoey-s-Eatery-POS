using System;
using Microsoft.Data.Sqlite;
using TransFundInventory.Data;

namespace TransFundInventory.Data
{
    public static class SettingsRepository
    {
        public static void SaveSetting(string key, string value)
        {
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = @"
                INSERT INTO Settings (SettingKey, SettingValue) 
                VALUES (@key, @val) 
                ON CONFLICT(SettingKey) DO UPDATE SET SettingValue = @val";
            command.Parameters.AddWithValue("@key", key);
            command.Parameters.AddWithValue("@val", value);
            command.ExecuteNonQuery();
        }

        public static string? GetSetting(string key)
        {
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT SettingValue FROM Settings WHERE SettingKey = @key";
            command.Parameters.AddWithValue("@key", key);
            return command.ExecuteScalar()?.ToString();
        }
    }
}
