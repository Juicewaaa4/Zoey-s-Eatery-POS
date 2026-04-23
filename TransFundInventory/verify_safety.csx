using Microsoft.Data.Sqlite;

var dbPath = @"c:\Transfund Inventory\TransFundInventory\bin\Debug\net8.0-windows\TransFundInventory.db";
using var conn = new SqliteConnection($"Data Source={dbPath}");
conn.Open();

// Check a few items that definitely WEREN'T in the screenshot but exist in Store
var checkItems = new[] { "MAXX YELLOW", "MEGASTICK(BUTTERSCOTCH)", "NESTEA LEMON (B)" };

Console.WriteLine("--- VERIFICATION ---");
foreach (var name in checkItems)
{
    using var cmd = conn.CreateCommand();
    cmd.CommandText = "SELECT Name, Price, CostPrice, Quantity FROM Products WHERE Name = @name AND Section = 'Store'";
    cmd.Parameters.AddWithValue("@name", name);
    using var r = cmd.ExecuteReader();
    if (r.Read())
    {
        Console.WriteLine($"Untouched Item: {r.GetString(0)} | Price: {r.GetDouble(1)} | Qty: {r.GetInt32(3)} (SAFE)");
    }
}
