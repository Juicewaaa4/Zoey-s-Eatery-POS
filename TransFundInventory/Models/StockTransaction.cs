namespace TransFundInventory.Models
{
    public class StockTransaction
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty; // For display
        public string Type { get; set; } = "IN"; // "IN" or "OUT"
        public int Quantity { get; set; }
        public string Notes { get; set; } = string.Empty;
        public int UserId { get; set; }
        public string UserName { get; set; } = string.Empty; // For display
        public string TransactionDate { get; set; } = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
    }
}
