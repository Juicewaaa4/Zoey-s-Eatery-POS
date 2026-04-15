namespace TransFundInventory.Models
{
    /// <summary>
    /// Represents a single product's aggregated sales data for shift reporting.
    /// One row per product per shift.
    /// </summary>
    public class ShiftSalesDetail
    {
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string CashierName { get; set; } = string.Empty;
        public double BuyingPrice { get; set; }   // CostAtSale (per unit)
        public double SellingPrice { get; set; }   // PriceAtSale (per unit)
        public int QtySold { get; set; }
        public double DistributorPrice => BuyingPrice * QtySold;   // Total cost
        public double GrossIncome => SellingPrice * QtySold;       // Total revenue
        public double NetIncome => GrossIncome - DistributorPrice; // Profit
        public double Percentage => GrossIncome > 0 ? (NetIncome / GrossIncome) * 100 : 0;
        public string TransactionTime { get; set; } = string.Empty; // For time-based filtering
    }
}
