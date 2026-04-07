using TransFundInventory.Data;
using TransFundInventory.Forms;

namespace TransFundInventory;

static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        // Initialize database and create tables if not exist
        DatabaseHelper.InitializeDatabase();

        Application.Run(new LoginForm());
    }
}