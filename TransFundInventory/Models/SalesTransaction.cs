namespace TransFundInventory.Models
{
    public class SalesTransaction
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string TransactionDate { get; set; } = string.Empty;
        public double TotalAmount { get; set; }
        public double CashTendered { get; set; }
        public double ChangeAmount { get; set; }
        public int UserId { get; set; }
        public string? CustomerName { get; set; }
        public string PaymentMethod { get; set; } = "Cash";
        public string? ReferenceNumber { get; set; }

        // Navigation properties (not directly stored in the same row but useful for UI)
        public string UserName { get; set; } = string.Empty;
    }
}
