namespace TransFundInventory.Models
{
    public class SalesItem
    {
        public int Id { get; set; }
        public int SalesTransactionId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public double PriceAtSale { get; set; }
        public double CostAtSale { get; set; }
        public double Subtotal { get; set; }

        // Associated details for UI
        public string ProductName { get; set; } = string.Empty;
        public string SKU { get; set; } = string.Empty;
    }
}
