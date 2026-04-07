using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using ScottPlot.WinForms;
using TransFundInventory.Data;
using TransFundInventory.Helpers;

namespace TransFundInventory.Forms
{
    public class SalesAnalyticsControl : UserControl
    {
        private Label lblGrossSales = null!;
        private Label lblNetProfit = null!;
        private DateTimePicker dtpFrom = null!;
        private DateTimePicker dtpTo = null!;
        private FormsPlot plotSalesTrend = null!;
        private DataGridView dgvTopProducts = null!;

        private readonly SalesRepository _salesRepo = new();

        public SalesAnalyticsControl()
        {
            InitializeComponent();
            LoadData(dtpFrom.Value, dtpTo.Value);
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(240, 242, 245);
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(20);

            // HEADER
            var panelHeader = new Panel { Dock = DockStyle.Top, Height = 60 };
            
            var lblTitle = new Label
            {
                Text = "📈 Sales & Profit Analytics",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32),
                Dock = DockStyle.Left,
                AutoSize = true
            };

            var panelFilter = new Panel { Dock = DockStyle.Right, Width = 450, Padding = new Padding(0, 10, 0, 0) };
            
            var lblFrom = new Label { Text = "From:", Font = new Font("Segoe UI", 10), Location = new Point(10, 14), AutoSize = true };
            dtpFrom = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Location = new Point(55, 10),
                Width = 110,
                Font = new Font("Segoe UI", 11)
            };
            dtpFrom.ValueChanged += (s, e) => LoadData(dtpFrom.Value, dtpTo.Value);

            var lblTo = new Label { Text = "To:", Font = new Font("Segoe UI", 10), Location = new Point(175, 14), AutoSize = true };
            dtpTo = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Location = new Point(205, 10),
                Width = 110,
                Font = new Font("Segoe UI", 11)
            };
            dtpTo.ValueChanged += (s, e) => LoadData(dtpFrom.Value, dtpTo.Value);

            panelFilter.Controls.AddRange(new Control[] { lblFrom, dtpFrom, lblTo, dtpTo });
            
            var btnExport = new Button
            {
                Text = "🟩 Export Sales (Excel)",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(39, 174, 96),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Location = new Point(325, 10),
                Size = new Size(150, 28) // extended filter panel to hold this? panelFilter width is 450.
            }; // We might need to adjust panelFilter.Width
            btnExport.FlatAppearance.BorderSize = 0;
            btnExport.Click += (s, e) => ExportSalesExcel();
            panelFilter.Width = 500; // Increase width to fit the button
            btnExport.Location = new Point(330, 10);
            panelFilter.Controls.Add(btnExport);
            
            panelHeader.Controls.Add(lblTitle);
            panelHeader.Controls.Add(panelFilter);

            // STAT CARDS
            var panelCards = new Panel { Dock = DockStyle.Top, Height = 120, Padding = new Padding(0, 20, 0, 20) };
            var pnlGross = CreateStatCard("Gross Sales", Color.FromArgb(52, 120, 246), out lblGrossSales);
            var pnlNet = CreateStatCard("Net Profit", Color.FromArgb(39, 174, 96), out lblNetProfit);
            
            pnlGross.Location = new Point(0, 20);
            pnlNet.Location = new Point(320, 20);

            panelCards.Controls.Add(pnlGross);
            panelCards.Controls.Add(pnlNet);

            // BOTTOM SPLIT (Chart & Top Products)
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                Orientation = Orientation.Vertical,
                SplitterDistance = 600, // Left side width for chart
                Margin = new Padding(0, 20, 0, 0)
            };

            // Chart setup
            var panelChart = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10), BorderStyle = BorderStyle.FixedSingle };
            var lblChartTitle = new Label { Text = "Sales Trend", Font = new Font("Segoe UI", 12, FontStyle.Bold), Dock = DockStyle.Top, Height = 30 };
            plotSalesTrend = new FormsPlot { Dock = DockStyle.Fill };
            plotSalesTrend.UserInputProcessor.Disable();
            panelChart.Controls.Add(plotSalesTrend);
            panelChart.Controls.Add(lblChartTitle);
            splitContainer.Panel1.Controls.Add(panelChart);
            splitContainer.Panel1.Padding = new Padding(0, 20, 10, 0);

            // Top Products setup
            var panelTop = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10), BorderStyle = BorderStyle.FixedSingle };
            var lblTopTitle = new Label { Text = "Top Selling Products", Font = new Font("Segoe UI", 12, FontStyle.Bold), Dock = DockStyle.Top, Height = 30 };
            
            dgvTopProducts = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.None,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                Font = new Font("Segoe UI", 10),
                GridColor = Color.FromArgb(235, 240, 245)
            };
            dgvTopProducts.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(27, 94, 32);
            dgvTopProducts.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvTopProducts.EnableHeadersVisualStyles = false;

            panelTop.Controls.Add(dgvTopProducts);
            panelTop.Controls.Add(lblTopTitle);
            splitContainer.Panel2.Controls.Add(panelTop);
            splitContainer.Panel2.Padding = new Padding(10, 20, 0, 0);

            // Add all to main
            this.Controls.Add(splitContainer);
            this.Controls.Add(panelCards);
            this.Controls.Add(panelHeader);
        }

        private Panel CreateStatCard(string title, Color accent, out Label valueLabel)
        {
            var card = new Panel
            {
                Size = new Size(300, 100),
                BackColor = Color.White,
                Padding = new Padding(20, 15, 20, 15),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(100, 110, 130),
                Dock = DockStyle.Top,
                Height = 25
            };

            valueLabel = new Label
            {
                Text = "₱0.00",
                Font = new Font("Segoe UI", 24, FontStyle.Bold),
                ForeColor = accent,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft
            };

            var accentBar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 5,
                BackColor = accent
            };

            card.Controls.Add(valueLabel);
            card.Controls.Add(lblTitle);
            card.Controls.Add(accentBar);

            return card;
        }

        // Replaced CmbPeriod_SelectedIndexChanged with direct calls to LoadData

        private void LoadData(DateTime fromDate, DateTime toDate)
        {
            // 1. Update Cards
            var (gross, net) = _salesRepo.GetSalesAnalytics(fromDate, toDate);
            lblGrossSales.Text = $"₱{gross:N2}";
            lblNetProfit.Text = $"₱{net:N2}";

            // 2. Fetch Data for Charts & Grid
            var sales = _salesRepo.GetAllSales(fromDate, toDate);
            var allItems = sales.SelectMany(s => _salesRepo.GetSalesItems(s.Id)).ToList();

            // 3. Top Products Grid
            var topProducts = allItems
                .GroupBy(i => i.ProductName)
                .Select(g => new
                {
                    Product = g.Key,
                    Sold = g.Sum(x => x.Quantity),
                    Revenue = g.Sum(x => x.Subtotal)
                })
                .OrderByDescending(x => x.Revenue)
                .Take(10)
                .ToList();

            dgvTopProducts.DataSource = topProducts;
            if (dgvTopProducts.Columns.Count > 0)
            {
                dgvTopProducts.Columns["Revenue"].DefaultCellStyle.Format = "₱0.00";
            }

            // 4. Sales Trend Chart
            plotSalesTrend.Plot.Clear();

            if (sales.Count > 0)
            {
                // Group sales by day
                var dailySales = sales
                    .GroupBy(s => DateTime.Parse(s.TransactionDate).Date)
                    .Select(g => new { Date = g.Key, Total = g.Sum(x => x.TotalAmount) })
                    .OrderBy(x => x.Date)
                    .ToList();

                var positions = Enumerable.Range(0, dailySales.Count).Select(i => (double)i).ToArray();
                var ys = dailySales.Select(d => d.Total).ToArray();
                var labels = dailySales.Select(d => d.Date.ToString("MMM dd")).ToArray();

                var barPlot = plotSalesTrend.Plot.Add.Bars(positions, ys);
                foreach (var bar in barPlot.Bars) { bar.Size = 0.4; }
                
                plotSalesTrend.Plot.Axes.Bottom.TickGenerator = new ScottPlot.TickGenerators.NumericManual(
                    positions.Select((pos, i) => new ScottPlot.Tick(pos, labels[i])).ToArray()
                );

                // Add padding if there are very few days so they don't stretch massively
                if (positions.Length <= 2)
                {
                    plotSalesTrend.Plot.Axes.SetLimitsX(-1.5, positions.Length + 0.5);
                }

                double maxY = ys.Length > 0 ? ys.Max() : 100;
                plotSalesTrend.Plot.Axes.SetLimitsY(0, maxY == 0 ? 100 : maxY * 1.2);

                plotSalesTrend.Plot.FigureBackground.Color = ScottPlot.Color.FromHex("#FFFFFF");
                plotSalesTrend.Plot.DataBackground.Color = ScottPlot.Color.FromHex("#F8F9FA");
            }
            
            plotSalesTrend.Plot.Title("Revenue over time");
            plotSalesTrend.Plot.XLabel("Date");
            plotSalesTrend.Plot.YLabel("Sales (₱)");
            plotSalesTrend.Refresh();
        }

        private void ExportSalesExcel()
        {
            var sales = _salesRepo.GetAllSales(dtpFrom.Value, dtpTo.Value);
            if (sales.Count == 0)
            {
                MessageBox.Show("No sales found for the selected date range.", "No Data", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            var path = ExportHelper.ShowSaveDialog("Excel Files|*.xlsx", $"{SessionManager.CurrentSection}Sales_{dtpFrom.Value:yyyyMMdd}_{dtpTo.Value:yyyyMMdd}.xlsx");
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

                ExportHelper.ExportToExcel(exportList, $"{SessionManager.CurrentSection} Sales", path);
                MessageBox.Show($"{SessionManager.CurrentSection} Sales Data Exported Successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
