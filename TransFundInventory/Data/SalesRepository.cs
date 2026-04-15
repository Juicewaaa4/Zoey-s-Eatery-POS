using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using TransFundInventory.Helpers;
using TransFundInventory.Models;

namespace TransFundInventory.Data
{
    public class SalesRepository
    {
        private readonly string connectionString = DatabaseHelper.ConnectionString;
        private string Section => SessionManager.CurrentSection;
        private static (string From, string To) BuildSqlDateRange(DateTime fromDate, DateTime toDate)
        {
            var from = fromDate.Date;
            var to = toDate.Date;
            if (from > to) (from, to) = (to, from);
            return (from.ToString("yyyy-MM-dd 00:00:00"), to.ToString("yyyy-MM-dd 23:59:59"));
        }

        /// <summary>
        /// Generates order number in format MMDDYY-XXXX (auto-incrementing per day)
        /// </summary>
        private string GenerateOrderNumber(SqliteConnection connection, SqliteTransaction transaction)
        {
            string datePrefix = DateTime.Now.ToString("Mddyy");

            var cmd = connection.CreateCommand();
            cmd.Transaction = transaction;
            cmd.CommandText = @"
                SELECT OrderNumber FROM SalesTransactions 
                WHERE OrderNumber LIKE @prefix || '%' 
                ORDER BY OrderNumber DESC LIMIT 1";
            cmd.Parameters.AddWithValue("@prefix", datePrefix);

            var lastOrder = cmd.ExecuteScalar() as string;

            int nextSeq = 1;
            if (!string.IsNullOrEmpty(lastOrder) && lastOrder.Contains('-'))
            {
                var parts = lastOrder.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[1], out int lastSeq))
                {
                    nextSeq = lastSeq + 1;
                }
            }

            return $"{datePrefix}-{nextSeq:D4}";
        }

        // Process a complete sale transaction atomically
        public bool ProcessSale(SalesTransaction sale, List<SalesItem> items, int currentUserId)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                // Generate order number
                sale.OrderNumber = GenerateOrderNumber(connection, transaction);

                // 1. Insert SalesTransaction with Section
                var insertSaleCmd = connection.CreateCommand();
                insertSaleCmd.Transaction = transaction;
                insertSaleCmd.CommandText = @"
                    INSERT INTO SalesTransactions (OrderNumber, TransactionDate, TotalAmount, CashTendered, ChangeAmount, UserId, CustomerName, Section, PaymentMethod, ReferenceNumber)
                    VALUES (@orderNumber, @date, @total, @tendered, @change, @userId, @customer, @section, @paymentMethod, @referenceNumber);
                    SELECT last_insert_rowid();";
                
                insertSaleCmd.Parameters.AddWithValue("@orderNumber", sale.OrderNumber);
                insertSaleCmd.Parameters.AddWithValue("@date", sale.TransactionDate);
                insertSaleCmd.Parameters.AddWithValue("@total", sale.TotalAmount);
                insertSaleCmd.Parameters.AddWithValue("@tendered", sale.CashTendered);
                insertSaleCmd.Parameters.AddWithValue("@change", sale.ChangeAmount);
                insertSaleCmd.Parameters.AddWithValue("@userId", sale.UserId);
                insertSaleCmd.Parameters.AddWithValue("@customer", sale.CustomerName ?? (object)DBNull.Value);
                insertSaleCmd.Parameters.AddWithValue("@section", Section);
                insertSaleCmd.Parameters.AddWithValue("@paymentMethod", sale.PaymentMethod);
                insertSaleCmd.Parameters.AddWithValue("@referenceNumber", sale.ReferenceNumber ?? (object)DBNull.Value);

                var saleId = Convert.ToInt32(insertSaleCmd.ExecuteScalar());
                sale.Id = saleId;

                // 2. Insert SalesItems and Deduct Stock
                foreach (var item in items)
                {
                    item.SalesTransactionId = saleId;

                    var insertItemCmd = connection.CreateCommand();
                    insertItemCmd.Transaction = transaction;
                    insertItemCmd.CommandText = @"
                        INSERT INTO SalesItems (SalesTransactionId, ProductId, Quantity, PriceAtSale, CostAtSale, Subtotal)
                        VALUES (@saleId, @productId, @qty, @price, @cost, @subtotal)";
                    insertItemCmd.Parameters.AddWithValue("@saleId", item.SalesTransactionId);
                    insertItemCmd.Parameters.AddWithValue("@productId", item.ProductId);
                    insertItemCmd.Parameters.AddWithValue("@qty", item.Quantity);
                    insertItemCmd.Parameters.AddWithValue("@price", item.PriceAtSale);
                    insertItemCmd.Parameters.AddWithValue("@cost", item.CostAtSale);
                    insertItemCmd.Parameters.AddWithValue("@subtotal", item.Subtotal);
                    insertItemCmd.ExecuteNonQuery();

                    // Deduct stock
                    var deductCmd = connection.CreateCommand();
                    deductCmd.Transaction = transaction;
                    deductCmd.CommandText = "UPDATE Products SET Quantity = Quantity - @qty, UpdatedAt = @updatedAt WHERE Id = @productId";
                    deductCmd.Parameters.AddWithValue("@qty", item.Quantity);
                    deductCmd.Parameters.AddWithValue("@updatedAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    deductCmd.Parameters.AddWithValue("@productId", item.ProductId);
                    deductCmd.ExecuteNonQuery();

                    // Log stock transaction so it reflects in Transaction History
                    var stockHistCmd = connection.CreateCommand();
                    stockHistCmd.Transaction = transaction;
                    stockHistCmd.CommandText = @"
                        INSERT INTO StockTransactions (ProductId, Type, Quantity, Notes, UserId, Section, TransactionDate)
                        VALUES (@productId, 'OUT', @qty, @notes, @userId, @section, @date)";
                    stockHistCmd.Parameters.AddWithValue("@productId", item.ProductId);
                    stockHistCmd.Parameters.AddWithValue("@qty", item.Quantity);
                    stockHistCmd.Parameters.AddWithValue("@notes", $"Sold (Order #{sale.OrderNumber})");
                    stockHistCmd.Parameters.AddWithValue("@userId", currentUserId);
                    stockHistCmd.Parameters.AddWithValue("@section", Section);
                    stockHistCmd.Parameters.AddWithValue("@date", sale.TransactionDate);
                    stockHistCmd.ExecuteNonQuery();
                }

                // 3. Log Audit
                var itemNames = string.Join(", ", items.Select(i => i.Quantity > 1 ? $"{i.Quantity}x {i.ProductName}" : i.ProductName));
                var auditCmd = connection.CreateCommand();
                auditCmd.Transaction = transaction;
                auditCmd.CommandText = @"
                    INSERT INTO AuditLogs (UserId, Action, Details, Timestamp)
                    VALUES (@userId, @action, @details, @timestamp)";
                auditCmd.Parameters.AddWithValue("@userId", currentUserId);
                auditCmd.Parameters.AddWithValue("@action", "Sales Checkout");
                auditCmd.Parameters.AddWithValue("@details", $"[{Section}] Processed sale Order#{sale.OrderNumber} for ₱{sale.TotalAmount:N2} via Cash ({itemNames})");
                auditCmd.Parameters.AddWithValue("@timestamp", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                auditCmd.ExecuteNonQuery();

                transaction.Commit();
                return true;
            }
            catch (Exception)
            {
                transaction.Rollback();
                throw;
            }
        }

        public List<SalesTransaction> GetAllSales(DateTime fromDate, DateTime toDate)
        {
            var sales = new List<SalesTransaction>();
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            var (from, to) = BuildSqlDateRange(fromDate, toDate);

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT s.*, u.FullName as UserName
                FROM SalesTransactions s
                JOIN Users u ON s.UserId = u.Id
                WHERE s.TransactionDate >= @from AND s.TransactionDate <= @to
                AND s.Section = @section
                ORDER BY s.TransactionDate DESC";
            
            command.Parameters.AddWithValue("@from", from);
            command.Parameters.AddWithValue("@to", to);
            command.Parameters.AddWithValue("@section", Section);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                var orderNumOrd = reader.GetOrdinal("OrderNumber");
                sales.Add(new SalesTransaction
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    OrderNumber = reader.IsDBNull(orderNumOrd) ? "" : reader.GetString(orderNumOrd),
                    TransactionDate = reader.GetString(reader.GetOrdinal("TransactionDate")),
                    TotalAmount = reader.GetDouble(reader.GetOrdinal("TotalAmount")),
                    CashTendered = reader.GetDouble(reader.GetOrdinal("CashTendered")),
                    ChangeAmount = reader.GetDouble(reader.GetOrdinal("ChangeAmount")),
                    UserId = reader.GetInt32(reader.GetOrdinal("UserId")),
                    CustomerName = reader.IsDBNull(reader.GetOrdinal("CustomerName")) ? null : reader.GetString(reader.GetOrdinal("CustomerName")),
                    PaymentMethod = reader.IsDBNull(reader.GetOrdinal("PaymentMethod")) ? "Cash" : reader.GetString(reader.GetOrdinal("PaymentMethod")),
                    ReferenceNumber = reader.IsDBNull(reader.GetOrdinal("ReferenceNumber")) ? null : reader.GetString(reader.GetOrdinal("ReferenceNumber")),
                    UserName = reader.GetString(reader.GetOrdinal("UserName"))
                });
            }

            return sales;
        }

        public List<SalesItem> GetSalesItems(int transactionId)
        {
            var items = new List<SalesItem>();
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT si.*, p.Name as ProductName, p.SKU
                FROM SalesItems si
                JOIN Products p ON si.ProductId = p.Id
                WHERE si.SalesTransactionId = @txId";
            command.Parameters.AddWithValue("@txId", transactionId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                items.Add(new SalesItem
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    SalesTransactionId = reader.GetInt32(reader.GetOrdinal("SalesTransactionId")),
                    ProductId = reader.GetInt32(reader.GetOrdinal("ProductId")),
                    Quantity = reader.GetInt32(reader.GetOrdinal("Quantity")),
                    PriceAtSale = reader.GetDouble(reader.GetOrdinal("PriceAtSale")),
                    CostAtSale = reader.GetDouble(reader.GetOrdinal("CostAtSale")),
                    Subtotal = reader.GetDouble(reader.GetOrdinal("Subtotal")),
                    ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                    SKU = reader.GetString(reader.GetOrdinal("SKU"))
                });
            }

            return items;
        }

        // Returns gross sales and net profit for a given date range
        public (double GrossSales, double NetProfit) GetSalesAnalytics(DateTime fromDate, DateTime toDate)
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            var (from, to) = BuildSqlDateRange(fromDate, toDate);

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT 
                    COALESCE(SUM(si.Subtotal), 0) as GrossSales,
                    COALESCE(SUM((si.PriceAtSale - si.CostAtSale) * si.Quantity), 0) as NetProfit
                FROM SalesItems si
                JOIN SalesTransactions st ON si.SalesTransactionId = st.Id
                WHERE st.TransactionDate >= @from AND st.TransactionDate <= @to
                AND st.Section = @section";
            
            command.Parameters.AddWithValue("@from", from);
            command.Parameters.AddWithValue("@to", to);
            command.Parameters.AddWithValue("@section", Section);

            using var reader = command.ExecuteReader();
            if (reader.Read())
            {
                var gross = reader.GetDouble(0);
                var net = reader.GetDouble(1);
                return (gross, net);
            }
            return (0, 0);
        }

        public Dictionary<string, double> GetCategorySalesAnalytics(DateTime date)
        {
            var results = new Dictionary<string, double>();
            using var connection = new SqliteConnection(connectionString);
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT 
                    COALESCE(c.Name, 'Uncategorized') as CategoryName,
                    SUM(si.Subtotal) as TotalSales
                FROM SalesItems si
                JOIN SalesTransactions st ON si.SalesTransactionId = st.Id
                JOIN Products p ON si.ProductId = p.Id
                LEFT JOIN Categories c ON p.CategoryId = c.Id
                WHERE date(st.TransactionDate) = date(@date)
                AND st.Section = @section
                GROUP BY c.Name";
            
            command.Parameters.AddWithValue("@date", date.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@section", Section);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                results[reader.GetString(0)] = reader.GetDouble(1);
            }
            return results;
        }

        /// <summary>
        /// Returns a flat list of all sold items with full details for Transaction History display.
        /// </summary>
        public List<SalesTransactionDetail> GetSalesItemsDetail(DateTime fromDate, DateTime toDate)
        {
            var details = new List<SalesTransactionDetail>();
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            var (from, to) = BuildSqlDateRange(fromDate, toDate);

            var command = connection.CreateCommand();
            command.CommandText = @"
                SELECT 
                    MAX(si.Id) as Id,
                    st.TransactionDate,
                    st.OrderNumber,
                    p.Name AS ProductName,
                    SUM(si.Quantity) AS QtySold,
                    si.PriceAtSale AS UnitPrice,
                    SUM(si.Subtotal) as Subtotal,
                    si.CostAtSale AS CostPrice,
                    SUM((si.PriceAtSale - si.CostAtSale) * si.Quantity) AS Profit,
                    st.PaymentMethod,
                    u.FullName AS Cashier
                FROM SalesItems si
                JOIN SalesTransactions st ON si.SalesTransactionId = st.Id
                JOIN Products p ON si.ProductId = p.Id
                JOIN Users u ON st.UserId = u.Id
                WHERE st.TransactionDate >= @from 
                  AND st.TransactionDate <= @to
                  AND st.Section = @section
                GROUP BY st.OrderNumber, p.Name, st.TransactionDate, si.PriceAtSale, si.CostAtSale, st.PaymentMethod, u.FullName
                ORDER BY st.TransactionDate DESC";

            command.Parameters.AddWithValue("@from", from);
            command.Parameters.AddWithValue("@to", to);
            command.Parameters.AddWithValue("@section", Section);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                details.Add(new SalesTransactionDetail
                {
                    Id = reader.GetInt32(reader.GetOrdinal("Id")),
                    Date = reader.GetString(reader.GetOrdinal("TransactionDate")),
                    OrderNumber = reader.IsDBNull(reader.GetOrdinal("OrderNumber")) ? "" : reader.GetString(reader.GetOrdinal("OrderNumber")),
                    ProductName = reader.GetString(reader.GetOrdinal("ProductName")),
                    QtySold = reader.GetInt32(reader.GetOrdinal("QtySold")),
                    UnitPrice = reader.GetDouble(reader.GetOrdinal("UnitPrice")),
                    Subtotal = reader.GetDouble(reader.GetOrdinal("Subtotal")),
                    CostPrice = reader.GetDouble(reader.GetOrdinal("CostPrice")),
                    Profit = reader.GetDouble(reader.GetOrdinal("Profit")),
                    PaymentMethod = reader.IsDBNull(reader.GetOrdinal("PaymentMethod")) ? "Cash" : reader.GetString(reader.GetOrdinal("PaymentMethod")),
                    Cashier = reader.GetString(reader.GetOrdinal("Cashier"))
                });
            }

            return details;
        }

        public void ResetSalesData()
        {
            using var connection = new SqliteConnection(connectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction();
            try
            {
                var cmd1 = connection.CreateCommand();
                cmd1.Transaction = transaction;
                cmd1.CommandText = @"
                    DELETE FROM SalesItems WHERE SalesTransactionId IN (
                        SELECT Id FROM SalesTransactions WHERE Section = @section
                    )";
                cmd1.Parameters.AddWithValue("@section", Section);
                cmd1.ExecuteNonQuery();

                var cmd2 = connection.CreateCommand();
                cmd2.Transaction = transaction;
                cmd2.CommandText = "DELETE FROM SalesTransactions WHERE Section = @section";
                cmd2.Parameters.AddWithValue("@section", Section);
                cmd2.ExecuteNonQuery();

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }
        public List<CashierSalesSummary> GetCashierSalesSummary(DateTime fromDate, DateTime toDate)
        {
            var summary = new List<CashierSalesSummary>();
            var (from, to) = BuildSqlDateRange(fromDate, toDate);
            string sql = @"
                SELECT 
                    u.FullName AS CashierName,
                    COUNT(DISTINCT t.Id) AS TotalTransactions,
                    COALESCE(SUM(i.Quantity), 0) AS ItemsSold,
                    COALESCE(SUM(t.TotalAmount), 0) AS GrossSales,
                    COALESCE(SUM(i.Subtotal - (i.Quantity * i.CostAtSale)), 0) AS NetProfit
                FROM SalesTransactions t
                JOIN Users u ON t.UserId = u.Id
                LEFT JOIN SalesItems i ON t.Id = i.SalesTransactionId
                WHERE t.Section = @section AND t.TransactionDate >= @from AND t.TransactionDate <= @to
                GROUP BY u.FullName
                ORDER BY GrossSales DESC";

            using var connection = DatabaseHelper.GetConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@section", Section);
            command.Parameters.AddWithValue("@from", from);
            command.Parameters.AddWithValue("@to", to);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                summary.Add(new CashierSalesSummary
                {
                    CashierName = reader.GetString(0),
                    TotalTransactions = reader.GetInt32(1),
                    ItemsSold = reader.GetInt32(2),
                    GrossSales = reader.GetDouble(3),
                    NetProfit = reader.GetDouble(4)
                });
            }

            return summary;
        }

        public List<ShiftSalesDetail> GetShiftSalesDetails(DateTime fromDate, DateTime toDate)
        {
            var details = new List<ShiftSalesDetail>();
            string sql = @"
                SELECT 
                    p.Name AS ProductName,
                    COALESCE(c.Name, 'Uncategorized') AS CategoryName,
                    i.CostAtSale AS BuyingPrice,
                    i.PriceAtSale AS SellingPrice,
                    i.Quantity AS QtySold,
                    t.TransactionDate
                FROM SalesItems i
                JOIN SalesTransactions t ON i.SalesTransactionId = t.Id
                JOIN Products p ON i.ProductId = p.Id
                LEFT JOIN Categories c ON p.CategoryId = c.Id
                WHERE t.Section = @section 
                  AND date(t.TransactionDate) BETWEEN date(@from) AND date(@to)
                ORDER BY p.Name";

            using var connection = DatabaseHelper.GetConnection();
            connection.Open();
            using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.Parameters.AddWithValue("@section", Section);
            command.Parameters.AddWithValue("@from", fromDate.ToString("yyyy-MM-dd"));
            command.Parameters.AddWithValue("@to", toDate.ToString("yyyy-MM-dd"));

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                details.Add(new ShiftSalesDetail
                {
                    ProductName = reader.GetString(0),
                    CategoryName = reader.IsDBNull(1) ? "Uncategorized" : reader.GetString(1),
                    BuyingPrice = reader.GetDouble(2),
                    SellingPrice = reader.GetDouble(3),
                    QtySold = reader.GetInt32(4),
                    TransactionTime = reader.GetString(5)
                });
            }

            return details;
        }
    }
}
