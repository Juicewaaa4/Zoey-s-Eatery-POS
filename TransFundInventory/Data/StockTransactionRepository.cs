using Microsoft.Data.Sqlite;
using TransFundInventory.Helpers;
using TransFundInventory.Models;

namespace TransFundInventory.Data
{
    public class StockTransactionRepository
    {
        private string Section => SessionManager.CurrentSection;
        private readonly ProductRepository _productRepo = new();

        public bool Add(StockTransaction transaction)
        {
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();
            using var dbTransaction = connection.BeginTransaction();

            try
            {
                // Insert the stock transaction
                var cmd = connection.CreateCommand();
                cmd.Transaction = dbTransaction;
                cmd.CommandText = @"INSERT INTO StockTransactions 
                    (ProductId, Type, Quantity, Notes, UserId, Section, TransactionDate)
                    VALUES (@productId, @type, @quantity, @notes, @userId, @section, @transactionDate)";
                cmd.Parameters.AddWithValue("@productId", transaction.ProductId);
                cmd.Parameters.AddWithValue("@type", transaction.Type);
                cmd.Parameters.AddWithValue("@quantity", transaction.Quantity);
                cmd.Parameters.AddWithValue("@notes", transaction.Notes);
                cmd.Parameters.AddWithValue("@userId", transaction.UserId);
                cmd.Parameters.AddWithValue("@section", Section);
                cmd.Parameters.AddWithValue("@transactionDate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.ExecuteNonQuery();

                // Update product quantity
                var updateCmd = connection.CreateCommand();
                updateCmd.Transaction = dbTransaction;
                if (transaction.Type == "IN")
                {
                    updateCmd.CommandText = @"UPDATE Products SET Quantity = Quantity + @qty, 
                        UpdatedAt = @updatedAt WHERE Id = @productId";
                }
                else
                {
                    updateCmd.CommandText = @"UPDATE Products SET Quantity = Quantity - @qty, 
                        UpdatedAt = @updatedAt WHERE Id = @productId";
                }
                updateCmd.Parameters.AddWithValue("@qty", transaction.Quantity);
                updateCmd.Parameters.AddWithValue("@updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                updateCmd.Parameters.AddWithValue("@productId", transaction.ProductId);
                updateCmd.ExecuteNonQuery();

                dbTransaction.Commit();
                return true;
            }
            catch
            {
                dbTransaction.Rollback();
                return false;
            }
        }

        public List<StockTransaction> GetByProductId(int productId)
        {
            var transactions = new List<StockTransaction>();
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT st.*, p.Name AS ProductName, u.FullName AS UserName
                FROM StockTransactions st
                JOIN Products p ON st.ProductId = p.Id
                JOIN Users u ON st.UserId = u.Id
                WHERE st.ProductId = @productId
                ORDER BY st.TransactionDate DESC";
            cmd.Parameters.AddWithValue("@productId", productId);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                transactions.Add(MapTransaction(reader));
            }
            return transactions;
        }

        public List<StockTransaction> GetAll(DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            var transactions = new List<StockTransaction>();
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            var sql = @"SELECT st.*, p.Name AS ProductName, u.FullName AS UserName
                FROM StockTransactions st
                JOIN Products p ON st.ProductId = p.Id
                JOIN Users u ON st.UserId = u.Id
                WHERE st.Section = @section";
            cmd.Parameters.AddWithValue("@section", Section);

            if (dateFrom.HasValue)
            {
                sql += " AND st.TransactionDate >= @dateFrom";
                cmd.Parameters.AddWithValue("@dateFrom", dateFrom.Value.ToString("yyyy-MM-dd 00:00:00"));
            }
            if (dateTo.HasValue)
            {
                sql += " AND st.TransactionDate <= @dateTo";
                cmd.Parameters.AddWithValue("@dateTo", dateTo.Value.ToString("yyyy-MM-dd 23:59:59"));
            }

            sql += " ORDER BY st.TransactionDate DESC";
            cmd.CommandText = sql;

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                transactions.Add(MapTransaction(reader));
            }
            return transactions;
        }

        public List<StockTransaction> GetRecent(int count = 10)
        {
            var transactions = new List<StockTransaction>();
            using var connection = DatabaseHelper.GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT st.*, p.Name AS ProductName, u.FullName AS UserName
                FROM StockTransactions st
                JOIN Products p ON st.ProductId = p.Id
                JOIN Users u ON st.UserId = u.Id
                WHERE st.Section = @section
                ORDER BY st.TransactionDate DESC
                LIMIT @count";
            cmd.Parameters.AddWithValue("@section", Section);
            cmd.Parameters.AddWithValue("@count", count);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                transactions.Add(MapTransaction(reader));
            }
            return transactions;
        }

        private static StockTransaction MapTransaction(SqliteDataReader reader)
        {
            return new StockTransaction
            {
                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                Type = reader.GetString(reader.GetOrdinal("Type")),
                Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                Notes = reader.IsDBNull(reader.GetOrdinal("Notes"))
                    ? string.Empty : reader.GetString(reader.GetOrdinal("Notes")),
                UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                UserName = reader.GetString(reader.GetOrdinal("UserName")),
                TransactionDate = reader.GetString(reader.GetOrdinal("TransactionDate"))
            };
        }
    }
}
