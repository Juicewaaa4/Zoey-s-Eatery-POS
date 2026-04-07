using Microsoft.Data.Sqlite;
using TransFundInventory.Helpers;
using TransFundInventory.Models;

namespace TransFundInventory.Data
{
    public class ProductRepository
    {
        private string Section => SessionManager.CurrentSection;

        public List<Product> GetAll()
        {
            var products = new List<Product>();
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT p.*, COALESCE(c.Name, 'Uncategorized') AS CategoryName 
                FROM Products p LEFT JOIN Categories c ON p.CategoryId = c.Id 
                WHERE p.Section = @section
                ORDER BY p.Name COLLATE NOCASE";
            cmd.Parameters.AddWithValue("@section", Section);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                products.Add(MapProduct(reader));
            }
            return products;
        }

        public Product? GetById(int id)
        {
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT p.*, COALESCE(c.Name, 'Uncategorized') AS CategoryName 
                FROM Products p LEFT JOIN Categories c ON p.CategoryId = c.Id 
                WHERE p.Id = @id";
            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return MapProduct(reader);
            }
            return null;
        }

        public List<Product> Search(string keyword, int? categoryId = null)
        {
            var products = new List<Product>();
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            var sql = @"SELECT p.*, COALESCE(c.Name, 'Uncategorized') AS CategoryName 
                FROM Products p LEFT JOIN Categories c ON p.CategoryId = c.Id 
                WHERE p.Section = @section";
            cmd.Parameters.AddWithValue("@section", Section);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                sql += " AND (p.Name LIKE @keyword OR p.SKU LIKE @keyword OR p.Description LIKE @keyword)";
                cmd.Parameters.AddWithValue("@keyword", $"%{keyword}%");
            }

            if (categoryId.HasValue && categoryId.Value > 0)
            {
                sql += " AND p.CategoryId = @categoryId";
                cmd.Parameters.AddWithValue("@categoryId", categoryId.Value);
            }

            sql += " ORDER BY p.Name COLLATE NOCASE";
            cmd.CommandText = sql;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                products.Add(MapProduct(reader));
            }
            return products;
        }

        public List<Product> GetLowStockProducts()
        {
            var products = new List<Product>();
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT p.*, COALESCE(c.Name, 'Uncategorized') AS CategoryName 
                FROM Products p LEFT JOIN Categories c ON p.CategoryId = c.Id 
                WHERE p.Quantity <= p.MinStockLevel AND p.Section = @section
                AND (p.Section = 'Store' OR p.MinStockLevel > 0)
                ORDER BY p.Quantity ASC";
            cmd.Parameters.AddWithValue("@section", Section);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                products.Add(MapProduct(reader));
            }
            return products;
        }

        public bool Add(Product product)
        {
            try
            {
                using var connection = DatabaseHelper.GetConnection();
                connection.Open();

                var cmd = connection.CreateCommand();
                cmd.CommandText = @"INSERT INTO Products 
                    (Name, Description, SKU, CategoryId, Price, CostPrice, Quantity, MinStockLevel, Unit, ImagePath, Section, CreatedAt, UpdatedAt)
                    VALUES (@name, @description, @sku, @categoryId, @price, @costPrice, @quantity, @minStock, @unit, @imagePath, @section, @createdAt, @updatedAt)";
                cmd.Parameters.AddWithValue("@name", product.Name);
                cmd.Parameters.AddWithValue("@description", product.Description);
                cmd.Parameters.AddWithValue("@sku", product.SKU);
                cmd.Parameters.AddWithValue("@categoryId", product.CategoryId);
                cmd.Parameters.AddWithValue("@price", product.Price);
                cmd.Parameters.AddWithValue("@costPrice", product.CostPrice);
                cmd.Parameters.AddWithValue("@quantity", product.Quantity);
                cmd.Parameters.AddWithValue("@minStock", product.MinStockLevel);
                cmd.Parameters.AddWithValue("@unit", product.Unit);
                cmd.Parameters.AddWithValue("@imagePath", (object?)product.ImagePath ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@section", Section);
                cmd.Parameters.AddWithValue("@createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.ExecuteNonQuery();
                return true;
            }
            catch (SqliteException)
            {
                return false;
            }
        }

        public bool Update(Product product)
        {
            try
            {
                using var connection = DatabaseHelper.GetConnection();
                connection.Open();

                var cmd = connection.CreateCommand();
                cmd.CommandText = @"UPDATE Products SET 
                    Name = @name, Description = @description, SKU = @sku, 
                    CategoryId = @categoryId, Price = @price, CostPrice = @costPrice,
                    Quantity = @quantity, MinStockLevel = @minStock, Unit = @unit, 
                    ImagePath = @imagePath, UpdatedAt = @updatedAt 
                    WHERE Id = @id";
                cmd.Parameters.AddWithValue("@name", product.Name);
                cmd.Parameters.AddWithValue("@description", product.Description);
                cmd.Parameters.AddWithValue("@sku", product.SKU);
                cmd.Parameters.AddWithValue("@categoryId", product.CategoryId);
                cmd.Parameters.AddWithValue("@price", product.Price);
                cmd.Parameters.AddWithValue("@costPrice", product.CostPrice);
                cmd.Parameters.AddWithValue("@quantity", product.Quantity);
                cmd.Parameters.AddWithValue("@minStock", product.MinStockLevel);
                cmd.Parameters.AddWithValue("@unit", product.Unit);
                cmd.Parameters.AddWithValue("@imagePath", (object?)product.ImagePath ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@id", product.Id);
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
                cmd.CommandText = "DELETE FROM Products WHERE Id = @id";
                cmd.Parameters.AddWithValue("@id", id);
                return cmd.ExecuteNonQuery() > 0;
            }
            catch (Exception ex)
            {
                // Likely a foreign key constraint failure
                return false;
            }
        }

        public int GetTotalProducts()
        {
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Products WHERE Section = @section";
            cmd.Parameters.AddWithValue("@section", Section);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        public decimal GetTotalStockValue()
        {
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COALESCE(SUM(Price * Quantity), 0) FROM Products WHERE Section = @section";
            cmd.Parameters.AddWithValue("@section", Section);
            return Convert.ToDecimal(cmd.ExecuteScalar());
        }

        public int GetLowStockCount()
        {
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "SELECT COUNT(*) FROM Products WHERE Quantity <= MinStockLevel AND Section = @section AND (Section = 'Store' OR MinStockLevel > 0)";
            cmd.Parameters.AddWithValue("@section", Section);
            return Convert.ToInt32(cmd.ExecuteScalar());
        }

        private static Product MapProduct(SqliteDataReader reader)
        {
            return new Product
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                Name = reader.GetString(reader.GetOrdinal("Name")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description"))
                    ? string.Empty : reader.GetString(reader.GetOrdinal("Description")),
                SKU = reader.IsDBNull(reader.GetOrdinal("SKU"))
                    ? string.Empty : reader.GetString(reader.GetOrdinal("SKU")),
                CategoryId = reader.IsDBNull(reader.GetOrdinal("CategoryId"))
                    ? 0 : reader.GetInt32(reader.GetOrdinal("CategoryId")),
                CategoryName = reader.GetString(reader.GetOrdinal("CategoryName")),
                Price = reader.GetDecimal(reader.GetOrdinal("Price")),
                CostPrice = reader.GetDecimal(reader.GetOrdinal("CostPrice")),
                Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                MinStockLevel = reader.GetInt32(reader.GetOrdinal("MinStockLevel")),
                Unit = reader.GetString(reader.GetOrdinal("Unit")),
                ImagePath = reader.IsDBNull(reader.GetOrdinal("ImagePath"))
                    ? null : reader.GetString(reader.GetOrdinal("ImagePath")),
                CreatedAt = reader.GetString(reader.GetOrdinal("CreatedAt")),
                UpdatedAt = reader.GetString(reader.GetOrdinal("UpdatedAt"))
            };
        }
    }
}
