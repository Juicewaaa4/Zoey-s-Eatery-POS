namespace TransFundInventory.Models
{
    public class EmailSettings
    {
        public int Id { get; set; }
        public string ResendApiKey { get; set; } = string.Empty;
        public string OwnerEmail { get; set; } = string.Empty;
        public string OwnerName { get; set; } = "Owner";
        public bool NotifyOnLogin { get; set; } = true;
        public bool NotifyOnLowStock { get; set; } = true;
        public bool IsEnabled { get; set; } = false;
    }
}
