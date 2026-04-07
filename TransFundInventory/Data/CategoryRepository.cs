using Microsoft.Data.Sqlite;
using TransFundInventory.Helpers;
using TransFundInventory.Models;

namespace TransFundInventory.Data
{
    public class CategoryRepository
    {
        private string Section => SessionManager.CurrentSection;

        public List<Category> GetAll()
        {
            var categories = new List<Category>();
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM Categories WHERE Section = @section ORDER BY Name COLLATE NOCASE";
            cmd.Parameters.AddWithValue("@section", Section);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                categories.Add(MapCategory(reader));
            }
            return categories;
        }

        public Category? GetById(int id)
        {
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT * FROM Categories WHERE Id = @id";
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return MapCategory(reader);
            }
            return null;
        }

        public bool Add(Category category)
        {
            try
            {
                using var connection = DatabaseHelper.GetConnection();
                connection.Open();

                var cmd = connection.CreateCommand();
                cmd.CommandText = @"INSERT INTO Categories (Name, Description, Section) 
                    VALUES (@name, @description, @section)";
                cmd.Parameters.AddWithValue("@name", category.Name);
                cmd.Parameters.AddWithValue("@description", category.Description);
                cmd.Parameters.AddWithValue("@section", Section);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqliteException)
            {
                return false;
            }
        }

        public bool Update(Category category)
        {
            try
            {
                using var connection = DatabaseHelper.GetConnection();
                connection.Open();

                var cmd = connection.CreateCommand();
                cmd.CommandText = @"UPDATE Categories SET Name = @name, 
                    Description = @description WHERE Id = @id";
                cmd.Parameters.AddWithValue("@name", category.Name);
                cmd.Parameters.AddWithValue("@description", category.Description);
                cmd.Parameters.AddWithValue("@id", category.Id);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqliteException)
            {
                return false;
            }
        }

        public bool Delete(int id)
        {
            try
            {
                using var connection = DatabaseHelper.GetConnection();
                connection.Open();

                var cmd = connection.CreateCommand();
                cmd.CommandText = "DELETE FROM Categories WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", id);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (SqliteException)
            {
                return false;
            }
        }

        public int GetProductCount(int categoryId)
        {
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Products WHERE CategoryId = @categoryId AND Section = @section";
            cmd.Parameters.AddWithValue("@categoryId", categoryId);
            cmd.Parameters.AddWithValue("@section", Section);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private static Category MapCategory(SqliteDataReader reader)
        {
            return new Category
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                    ? string.Empty
                    : reader.GetString(reader.GetOrdinal("Description"))
            };
        }
    }
}
