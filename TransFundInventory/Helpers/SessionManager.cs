using TransFundInventory.Models;

namespace TransFundInventory.Helpers
{
    public static class SessionManager
    {
        public static User? CurrentUser { get; set; }

        /// <summary>
        /// Current active section: "Store" or "Eatery"
        /// </summary>
        public static string CurrentSection { get; set; } = "Store";

        public static bool IsAdmin => CurrentUser?.Role == "Admin";

        public static void Logout()
        {
            CurrentUser = null;
            CurrentSection = "Store";
        }
    }
}
