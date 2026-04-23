using Microsoft.Data.Sqlite;

namespace TransFundInventory.Data
{
    public static class DatabaseHelper
    {
        private static readonly string DbPath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "TransFundInventory.db");

        private static readonly string ImageFolder = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "ProductImages");

        public static string ConnectionString => $"Data Source={DbPath}";
        public static string GetDbPath() => DbPath;
        public static string GetImageFolder()
        {
            if (!Directory.Exists(ImageFolder))
                Directory.CreateDirectory(ImageFolder);
            return ImageFolder;
        }

        public static SqliteConnection GetConnection()
        {
            return new SqliteConnection(ConnectionString);
        }

        public static void InitializeDatabase()
        {
            using var connection = GetConnection();
            connection.Open();

            var command = connection.CreateCommand();
            command.CommandText = @"
                CREATE TABLE IF NOT EXISTS Users (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Username TEXT NOT NULL UNIQUE,
                    Password TEXT NOT NULL,
                    FullName TEXT NOT NULL,
                    Role TEXT NOT NULL DEFAULT 'Cashier',
                    CreatedAt TEXT NOT NULL
                );

                CREATE TABLE IF NOT EXISTS Categories (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    Section TEXT NOT NULL DEFAULT 'Store'
                );

                CREATE TABLE IF NOT EXISTS Products (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    Description TEXT,
                    SKU TEXT UNIQUE,
                    CategoryId INTEGER,
                    Price REAL NOT NULL DEFAULT 0,
                    CostPrice REAL NOT NULL DEFAULT 0,
                    Quantity INTEGER NOT NULL DEFAULT 0,
                    MinStockLevel INTEGER NOT NULL DEFAULT 10,
                    Unit TEXT NOT NULL DEFAULT 'pcs',
                    ImagePath TEXT,
                    Section TEXT NOT NULL DEFAULT 'Store',
                    CreatedAt TEXT NOT NULL,
                    UpdatedAt TEXT NOT NULL,
                    FOREIGN KEY (CategoryId) REFERENCES Categories(Id)
                );

                CREATE TABLE IF NOT EXISTS StockTransactions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ProductId INTEGER NOT NULL,
                    Type TEXT NOT NULL,
                    Quantity INTEGER NOT NULL,
                    Notes TEXT,
                    UserId INTEGER NOT NULL,
                    Section TEXT NOT NULL DEFAULT 'Store',
                    TransactionDate TEXT NOT NULL,
                    FOREIGN KEY (ProductId) REFERENCES Products(Id),
                    FOREIGN KEY (UserId) REFERENCES Users(Id)
                );

                CREATE TABLE IF NOT EXISTS AuditLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    UserId INTEGER NOT NULL,
                    Action TEXT NOT NULL,
                    Details TEXT,
                    Timestamp TEXT NOT NULL,
                    FOREIGN KEY (UserId) REFERENCES Users(Id)
                );

                CREATE TABLE IF NOT EXISTS SalesTransactions (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    OrderNumber TEXT,
                    TransactionDate TEXT NOT NULL,
                    TotalAmount REAL NOT NULL,
                    CashTendered REAL NOT NULL,
                    ChangeAmount REAL NOT NULL,
                    UserId INTEGER NOT NULL,
                    CustomerName TEXT,
                    PaymentMethod TEXT NOT NULL DEFAULT 'Cash',
                    ReferenceNumber TEXT,
                    Section TEXT NOT NULL DEFAULT 'Store',
                    FOREIGN KEY (UserId) REFERENCES Users(Id)
                );

                CREATE TABLE IF NOT EXISTS SalesItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SalesTransactionId INTEGER NOT NULL,
                    ProductId INTEGER NOT NULL,
                    Quantity INTEGER NOT NULL,
                    PriceAtSale REAL NOT NULL,
                    CostAtSale REAL NOT NULL,
                    Subtotal REAL NOT NULL,
                    FOREIGN KEY (SalesTransactionId) REFERENCES SalesTransactions(Id),
                    FOREIGN KEY (ProductId) REFERENCES Products(Id)
                );

                CREATE TABLE IF NOT EXISTS EmailSettings (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ResendApiKey TEXT NOT NULL DEFAULT '',
                    OwnerEmail TEXT NOT NULL DEFAULT '',
                    OwnerName TEXT NOT NULL DEFAULT 'Owner',
                    NotifyOnLogin INTEGER NOT NULL DEFAULT 1,
                    NotifyOnLowStock INTEGER NOT NULL DEFAULT 1,
                    IsEnabled INTEGER NOT NULL DEFAULT 0
                );
                
                CREATE TABLE IF NOT EXISTS Settings (
                    SettingKey TEXT PRIMARY KEY,
                    SettingValue TEXT
                );
            ";
            command.ExecuteNonQuery();

            // Migration: Add columns for existing databases
            var migrations = new[]
            {
                "ALTER TABLE Products ADD COLUMN ImagePath TEXT",
                "ALTER TABLE Products ADD COLUMN Section TEXT NOT NULL DEFAULT 'Store'",
                "ALTER TABLE Categories ADD COLUMN Section TEXT NOT NULL DEFAULT 'Store'",
                "ALTER TABLE SalesTransactions ADD COLUMN OrderNumber TEXT",
                "ALTER TABLE SalesTransactions ADD COLUMN Section TEXT NOT NULL DEFAULT 'Store'",
                "ALTER TABLE StockTransactions ADD COLUMN Section TEXT NOT NULL DEFAULT 'Store'",
                "ALTER TABLE SalesTransactions ADD COLUMN PaymentMethod TEXT NOT NULL DEFAULT 'Cash'",
                "ALTER TABLE SalesTransactions ADD COLUMN ReferenceNumber TEXT",
                "ALTER TABLE SalesTransactions ADD COLUMN IsCancelled INTEGER NOT NULL DEFAULT 0",
                "ALTER TABLE SalesTransactions ADD COLUMN CancelledBy INTEGER",
                "ALTER TABLE SalesTransactions ADD COLUMN CancelledDate TEXT"
            };

            foreach (var migration in migrations)
            {
                try
                {
                    var alterCmd = connection.CreateCommand();
                    alterCmd.CommandText = migration;
                    alterCmd.ExecuteNonQuery();
                }
                catch (SqliteException) { /* Column already exists */ }
            }

            // Remove unique constraint on Categories.Name if it conflicts with section separation
            // (handled by application-level validation per section)

            // Fix: Reset eatery products that had fake 9999 quantity to 0
            try
            {
                var fixCmd = connection.CreateCommand();
                fixCmd.CommandText = "UPDATE Products SET Quantity = 0 WHERE Section = 'Eatery' AND Quantity = 9999";
                fixCmd.ExecuteNonQuery();

                // Migrate Staff to Cashier
                var migrateRoleCmd = connection.CreateCommand();
                migrateRoleCmd.CommandText = "UPDATE Users SET Role = 'Cashier' WHERE Role = 'Staff'";
                migrateRoleCmd.ExecuteNonQuery();
            }
            catch { /* safe to ignore */ }

            // Seed default admin if no users exist
            var checkCmd = connection.CreateCommand();
            checkCmd.CommandText = "SELECT COUNT(*) FROM Users";
            var count = Convert.ToInt64(checkCmd.ExecuteScalar());

            if (count == 0)
            {
                var seedCmd = connection.CreateCommand();
                seedCmd.CommandText = @"
                    INSERT INTO Users (Username, Password, FullName, Role, CreatedAt)
                    VALUES (@username, @password, @fullName, @role, @createdAt)";
                seedCmd.Parameters.AddWithValue("@username", "admin");
                seedCmd.Parameters.AddWithValue("@password", HashPassword("admin123"));
                seedCmd.Parameters.AddWithValue("@fullName", "System Administrator");
                seedCmd.Parameters.AddWithValue("@role", "Admin");
                seedCmd.Parameters.AddWithValue("@createdAt", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                seedCmd.ExecuteNonQuery();
            }
        }

        public static string HashPassword(string password)
        {
            using var sha256 = System.Security.Cryptography.SHA256.Create();
            var bytes = System.Text.Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
