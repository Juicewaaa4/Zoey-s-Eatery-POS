namespace TransFundInventory.Models
{
    /// <summary>
    /// Flat model for displaying sold items in Transaction History.
    /// Joins SalesTransactions + SalesItems + Products + Users.
    /// </summary>
    public class SalesTransactionDetail
    {
        public int Id { get; set; }
        public int SalesTransactionId { get; set; }
        public string Date { get; set; } = string.Empty;
        public bool IsCancelled { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int QtySold { get; set; }
        public double UnitPrice { get; set; }
        public double Subtotal { get; set; }
        public double CostPrice { get; set; }
        public double Profit { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string Cashier { get; set; } = string.Empty;
    }
}
