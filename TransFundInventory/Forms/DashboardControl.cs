using TransFundInventory.Data;
using ScottPlot.WinForms;

namespace TransFundInventory.Forms
{
    public class DashboardControl : UserControl
    {
        private Label lblTotalProducts = null!;
        private Label lblTotalValue = null!;
        private Label lblLowStock = null!;
        private DataGridView dgvRecent = null!;

        public DashboardControl()
        {
            InitializeComponent();
            LoadDashboardData();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(240, 242, 245);
            this.Padding = new Padding(10);

            var lblTitle = new Label
            {
                Text = "Dashboard",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32),
                Dock = DockStyle.Top,
                Height = 45
            };

            // Stats cards panel
            var panelStats = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 110,
                Padding = new Padding(0, 5, 0, 5),
                WrapContents = false
            };

            var card1 = CreateStatCard("📦 Total Products", "0", Color.FromArgb(52, 120, 246));
            lblTotalProducts = (Label)card1.Controls[0];
            var card2 = CreateStatCard("💰 Stock Value", "₱0.00", Color.FromArgb(39, 174, 96));
            lblTotalValue = (Label)card2.Controls[0];
            var card3 = CreateStatCard("⚠️ Low Stock", "0", Color.FromArgb(231, 76, 60));
            lblLowStock = (Label)card3.Controls[0];

            panelStats.Controls.AddRange(new Control[] { card1, card2, card3 });

            // Chart panel 
            var panelCharts = new Panel
            {
                Dock = DockStyle.Top,
                Height = 350,
                Padding = new Padding(0, 5, 0, 5)
            };

            // Category breakdown chart
            var chartPanel = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(10)
            };

            var lblChartTitle = new Label
            {
                Text = "📊 Stock by Category",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32),
                Dock = DockStyle.Top,
                Height = 25
            };

            var formsPlot = new FormsPlot
            {
                Dock = DockStyle.Fill
            };

            chartPanel.Controls.Add(formsPlot);
            chartPanel.Controls.Add(lblChartTitle);
            panelCharts.Controls.Add(chartPanel);

            // Load chart data
            try
            {
                LoadCategoryChart(formsPlot);
            }
            catch { /* Chart loading failure shouldn't break the app */ }

            // Recent transactions
            var lblRecent = new Label
            {
                Text = "📋 Recent Transactions",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32),
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(0, 5, 0, 0)
            };

            var panelGrid = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1)
            };

            dgvRecent = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(235, 240, 245),
                Font = new Font("Segoe UI", 9)
            };
            dgvRecent.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 235, 250);
            dgvRecent.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvRecent.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(27, 94, 32);
            dgvRecent.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvRecent.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvRecent.EnableHeadersVisualStyles = false;
            dgvRecent.ColumnHeadersHeight = 32;
            dgvRecent.RowTemplate.Height = 28;

            panelGrid.Controls.Add(dgvRecent);

            this.Controls.Add(panelGrid);
            this.Controls.Add(lblRecent);
            this.Controls.Add(panelCharts);
            this.Controls.Add(panelStats);
            this.Controls.Add(lblTitle);
        }

        private Panel CreateStatCard(string title, string value, Color accentColor)
        {
            var card = new Panel
            {
                Size = new Size(280, 90),
                BackColor = Color.White,
                Margin = new Padding(0, 0, 15, 0),
                Padding = new Padding(20, 10, 20, 10)
            };

            var lblCardTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(100, 110, 130),
                Dock = DockStyle.Top,
                Height = 22
            };

            var lblCardValue = new Label
            {
                Text = value,
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = accentColor,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var accentBar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 4,
                BackColor = accentColor
            };

            card.Controls.Add(lblCardValue);
            card.Controls.Add(lblCardTitle);
            card.Controls.Add(accentBar);
            return card;
        }

        private void LoadCategoryChart(FormsPlot formsPlot)
        {
            var categoryRepo = new CategoryRepository();
            var productRepo = new ProductRepository();
            var categories = categoryRepo.GetAll();
            var products = productRepo.GetAll();

            var catNames = new List<string>();
            var catValues = new List<double>();

            foreach (var cat in categories)
            {
                var catProducts = products.Where(p => p.CategoryId == cat.Id).ToList();
                if (catProducts.Count > 0)
                {
                    catNames.Add(cat.Name);
                    catValues.Add(catProducts.Sum(p => (double)(p.Price * p.Quantity)));
                }
            }

            var uncategorized = products.Where(p => p.CategoryId == 0).ToList();
            if (uncategorized.Count > 0)
            {
                catNames.Add("Uncategorized");
                catValues.Add(uncategorized.Sum(p => (double)(p.Price * p.Quantity)));
            }

            if (catValues.Count > 0)
            {
                var positions = Enumerable.Range(0, catValues.Count).Select(i => (double)i).ToArray();
                var barPlot = formsPlot.Plot.Add.Bars(positions, catValues.ToArray());
                foreach (var bar in barPlot.Bars) { bar.Size = 0.3; }
                
                formsPlot.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
                    positions.Select((pos, i) => new ScottPlot.Tick(pos, catNames[i])).ToArray()
                );
                
                // Add padding if there are very few categories so they don't stretch massively
                if (catValues.Count <= 2)
                {
                    formsPlot.Plot.Axes.SetLimitsX(-1.5, catValues.Count + 0.5);
                }

                formsPlot.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#FFFFFF");
                formsPlot.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#F8F9FA");
                formsPlot.Refresh();
            }
        }

        private void LoadDashboardData()
        {
            try
            {
                var productRepo = new ProductRepository();
                var transRepo = new StockTransactionRepository();

                lblTotalProducts.Text = productRepo.GetTotalProducts().ToString();
                lblTotalValue.Text = $"₱{productRepo.GetTotalStockValue():N2}";
                lblLowStock.Text = productRepo.GetLowStockCount().ToString();

                var recentTransactions = transRepo.GetRecent(10);
                dgvRecent.DataSource = recentTransactions.Select(t => new
                {
                    t.TransactionDate,
                    Product = t.ProductName,
                    t.Type,
                    t.Quantity,
                    t.Notes,
                    User = t.UserName
                }).ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
