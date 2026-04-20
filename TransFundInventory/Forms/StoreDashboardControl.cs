using TransFundInventory.Data;
using ScottPlot.WinForms;
using TransFundInventory.Helpers;
using System.Linq;
using System;
using System.Windows.Forms;
using System.Drawing;

namespace TransFundInventory.Forms
{
    public class StoreDashboardControl : UserControl
    {
        private Label lblTotalProducts = null!;
        private Label lblTotalValue = null!;
        private Label lblLowStock = null!;
        private Label lblTodaySales = null!;
        private DataGridView dgvRecent = null!;
        private DateTimePicker dtpFilterDate = null!;
        private Label lblSalesCardTitle = null!;
        private FormsPlot chartCategorySales = null!;

        public StoreDashboardControl()
        {
            InitializeComponent();
            LoadDashboardData();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(240, 242, 245);
            this.Padding = new Padding(10);

            var headerPanel = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                Padding = new Padding(0, 0, 0, 5)
            };

            var lblTitle = new Label
            {
                Text = "🏬 Store Dashboard",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32),
                Dock = DockStyle.Left,
                AutoSize = true
            };

            dtpFilterDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Width = 140,
                Font = new Font("Segoe UI", 11),
                Margin = new Padding(0, 3, 0, 0) // Push down slightly to align with button
            };
            dtpFilterDate.ValueChanged += (s, e) =>
            {
                LoadDashboardData(dtpFilterDate.Value);
                this.Refresh();
            };

            var btnExport = new Button
            {
                Text = "🟩 Export Sales",
                Width = 130,
                Height = 33,
                Margin = new Padding(10, 0, 0, 0), // 10px space between picker and button
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(39, 174, 96),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Click += (s, e) => ExportDashboardSales();

            var rightControls = new FlowLayoutPanel
            {
                Dock = DockStyle.Right,
                AutoSize = true,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0, 2, 0, 0)
            };
            rightControls.Controls.Add(dtpFilterDate);
            rightControls.Controls.Add(btnExport);

            headerPanel.Controls.Add(rightControls);
            headerPanel.Controls.Add(lblTitle);

            // Stats cards panel
            var panelStats = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 210,
                Padding = new Padding(0, 5, 0, 5),
                WrapContents = true
            };

            var card1 = CreateStatCard("📦 Total Products", "0", Color.FromArgb(27, 94, 32));
            lblTotalProducts = (Label)card1.Controls[0];
            var card2 = CreateStatCard("💰 Stock Value", "₱0.00", Color.FromArgb(56, 142, 60));
            lblTotalValue = (Label)card2.Controls[0];
            var card3 = CreateStatCard("🚨 LOW STOCK ALERT", "0", Color.FromArgb(211, 47, 47));
            lblLowStock = (Label)card3.Controls[0];
            var card4 = CreateStatCard("📅 Auto-Daily Sales", "₱0.00", Color.FromArgb(41, 128, 185)); // Blue
            lblTodaySales = (Label)card4.Controls[0];
            lblSalesCardTitle = (Label)card4.Controls[1];

            panelStats.Controls.AddRange(new Control[] { card1, card2, card3, card4 });

            // Chart panel 
            var panelCharts = new Panel
            {
                Dock = DockStyle.Top,
                Height = 450,
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
                Text = "📊 Sales by Category",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32),
                Dock = DockStyle.Top,
                Height = 25
            };

            chartCategorySales = new FormsPlot
            {
                Dock = DockStyle.Fill
            };
            chartCategorySales.UserInputProcessor.Disable();

            chartPanel.Controls.Add(chartCategorySales);
            chartPanel.Controls.Add(lblChartTitle);
            panelCharts.Controls.Add(chartPanel);

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
                AllowUserToResizeColumns = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(235, 240, 245),
                Font = new Font("Segoe UI", 9)
            };
            dgvRecent.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 230, 201);
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
            this.Controls.Add(headerPanel);
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

        private void LoadCategorySalesChart(FormsPlot formsPlot, DateTime targetDate)
        {
            var salesRepo = new SalesRepository();
            var categorySales = salesRepo.GetCategorySalesAnalytics(targetDate);

            formsPlot.Plot.Clear();

            if (categorySales.Count > 0)
            {
                var catNames = categorySales.Keys.ToList();
                var catValues = categorySales.Values.ToList();

                var positions = Enumerable.Range(0, catValues.Count).Select(i => (double)i).ToArray();
                var barPlot = formsPlot.Plot.Add.Bars(positions, catValues.ToArray());
                foreach (var bar in barPlot.Bars) { bar.Size = 0.3; }
                
                formsPlot.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
                    positions.Select((pos, i) => new ScottPlot.Tick(pos, catNames[i])).ToArray()
                );
                
                if (catValues.Count <= 2)
                {
                    formsPlot.Plot.Axes.SetLimitsX(-1.5, catValues.Count + 0.5);
                }

                formsPlot.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#FFFFFF");
                formsPlot.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#F8F9FA");
            }
            
            formsPlot.Refresh();
        }

        private void LoadDashboardData(DateTime? filterDate = null)
        {
            try
            {
                var productRepo = new ProductRepository();
                var transRepo = new StockTransactionRepository();
                var salesRepo = new SalesRepository();

                lblTotalProducts.Text = productRepo.GetTotalProducts().ToString();
                lblTotalValue.Text = $"₱{productRepo.GetTotalStockValue():N2}";
                
                int lowStockCount = productRepo.GetLowStockCount();
                lblLowStock.Text = lowStockCount.ToString();
                if (lowStockCount > 0)
                {
                    lblLowStock.ForeColor = Color.FromArgb(211, 47, 47);
                }

                var targetDate = filterDate ?? DateTime.Today;
                var todayAnalytics = salesRepo.GetSalesAnalytics(targetDate, targetDate);
                lblTodaySales.Text = $"₱{todayAnalytics.GrossSales:N2}";
                
                if (filterDate.HasValue && filterDate.Value.Date != DateTime.Today)
                {
                    lblSalesCardTitle.Text = $"📅 Sales for {targetDate:MMM dd, yyyy}";
                }
                else
                {
                    lblSalesCardTitle.Text = "📅 Today's Sales";
                }

                var recentTransactions = transRepo.GetRecent(10);
                dgvRecent.DataSource = recentTransactions.Select(t => new
                {
                    Date = DateTime.TryParse(t.TransactionDate, out var dt) ? dt.ToString("yyyy-MM-dd hh:mm tt") : t.TransactionDate,
                    Product = t.ProductName,
                    t.Type,
                    t.Quantity,
                    t.Notes,
                    User = t.UserName
                }).ToList();

                if (chartCategorySales != null)
                {
                    LoadCategorySalesChart(chartCategorySales, targetDate);
                }

                // Force UI refresh
                this.Invalidate();
                this.Update();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading dashboard: {ex.Message}", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportDashboardSales()
        {
            var salesRepo = new SalesRepository();
            var targetDate = dtpFilterDate.Value.Date;
            var sales = salesRepo.GetAllSales(targetDate, targetDate);
            
            if (sales.Count == 0)
            {
                MessageBox.Show("No sales found for the selected date.", "No Data", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var path = ExportHelper.ShowSaveDialog("Excel Files|*.xlsx", $"StoreSales_{targetDate:yyyyMMdd}.xlsx");
            if (path != null)
            {
                var exportList = sales.Select(s => new
                {
                    Date = s.TransactionDate,
                    ReceiptNo = s.OrderNumber,
                    Cashier = s.UserName,
                    Payment = s.PaymentMethod ?? "Cash",
                    RefNo = s.ReferenceNumber ?? "",
                    Total = s.TotalAmount,
                    Tendered = s.CashTendered,
                    Change = s.ChangeAmount
                }).ToList();

                ExportHelper.ExportToExcel(exportList, "Daily Sales", path);
                MessageBox.Show("Store Sales Exported Successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
