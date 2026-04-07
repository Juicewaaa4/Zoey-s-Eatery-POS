using Microsoft.Data.Sqlite;
using TransFundInventory.Models;

namespace TransFundInventory.Data
{
    public class UserRepository
    {
        public User? Authenticate(string username, string password)
        {
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users WHERE Username = @username AND Password = @password";
            cmd.Parameters.AddWithValue("@username", username);
            cmd.Parameters.AddWithValue("@password", DatabaseHelper.HashPassword(password));

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return MapUser(reader);
            }
            return null;
        }

        public List<User> GetAll()
        {
            var users = new List<User>();
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM Users ORDER BY FullName";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                users.Add(MapUser(reader));
            }
            return users;
        }

        public bool Add(User user)
        {
            try
            {
                using var connection = DatabaseHelper.GetConnection();
                connection.Open();

                var cmd = connection.CreateCommand();
                cmd.CommandText = @"INSERT INTO Users (Username, Password, FullName, Role, CreatedAt)
                    VALUES (@username, @password, @fullName, @role, @createdAt)";
                cmd.Parameters.AddWithValue("@username", user.Username);
                cmd.Parameters.AddWithValue("@password", DatabaseHelper.HashPassword(user.Password));
                cmd.Parameters.AddWithValue("@fullName", user.FullName);
                cmd.Parameters.AddWithValue("@role", user.Role);
                cmd.Parameters.AddWithValue("@createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqliteException)
            {
                return false;
            }
        }

        public bool Update(User user)
        {
            try
            {
                using var connection = DatabaseHelper.GetConnection();
                connection.Open();

                var cmd = connection.CreateCommand();
                cmd.CommandText = @"UPDATE Users SET Username = @username, FullName = @fullName, 
                    Role = @role WHERE Id = @id";
                cmd.Parameters.AddWithValue("@username", user.Username);
                cmd.Parameters.AddWithValue("@fullName", user.FullName);
                cmd.Parameters.AddWithValue("@role", user.Role);
                cmd.Parameters.AddWithValue("@id", user.Id);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqliteException)
            {
                return false;
            }
        }

        public bool UpdatePassword(int userId, string newPassword)
        {
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "UPDATE Users SET Password = @password WHERE Id = @id";
            cmd.Parameters.AddWithValue("@password", DatabaseHelper.HashPassword(newPassword));
            cmd.Parameters.AddWithValue("@id", userId);
            return cmd.ExecuteNonQuery() > 0;
        }

        public bool Delete(int id)
        {
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Users WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);
            return cmd.ExecuteNonQuery() > 0;
        }

        private static User MapUser(SqliteDataReader reader)
        {
            return new User
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Username = reader.GetString(reader.GetOrdinal("Username")),
                Password = reader.GetString(reader.GetOrdinal("Password")),
                FullName = reader.GetString(reader.GetOrdinal("FullName")),
                Role = reader.GetString(reader.GetOrdinal("Role")),
                CreatedAt = reader.GetString(reader.GetOrdinal("CreatedAt"))
            };
        }
    }
}
