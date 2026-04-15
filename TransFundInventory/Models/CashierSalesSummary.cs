namespace TransFundInventory.Models
{
    public class CashierSalesSummary
    {
        public string CashierName { get; set; } = string.Empty;
        public int TotalTransactions { get; set; }
        public int ItemsSold { get; set; }
        public double GrossSales { get; set; }
        public double NetProfit { get; set; }
    }
}
