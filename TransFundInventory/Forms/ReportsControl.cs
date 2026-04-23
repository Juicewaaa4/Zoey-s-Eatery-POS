using TransFundInventory.Data;
using TransFundInventory.Helpers;
using TransFundInventory.Models;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System;
using System.Globalization;

namespace TransFundInventory.Forms
{
    public class ReportsControl : UserControl
    {
        private TabControl tabControl = null!;
        private DataGridView dgvLowStock = null!;
        private DataGridView dgvSummary = null!;
        private DataGridView dgvHistory = null!;
        private DataGridView dgvStockHistory = null!;
        private DateTimePicker dtpFrom = null!;
        private DateTimePicker dtpTo = null!;
        private DateTimePicker dtpStockFrom = null!;
        private DateTimePicker dtpStockTo = null!;
        private DataGridView dgvShiftSales = null!;
        private DateTimePicker dtpShiftDate = null!;
        private DateTimePicker dtpMorningStart = null!;
        private DateTimePicker dtpNightStart = null!;
        private DateTimePicker dtpNightEnd = null!;
        private Label lblSalesTotal = null!;
        private Label lblProfitTotal = null!;
        private Label lblItemsSold = null!;

        private readonly ProductRepository _productRepo = new();
        private readonly StockTransactionRepository _transRepo = new();
        private readonly CategoryRepository _categoryRepo = new();
        private readonly SalesRepository _salesRepo = new();

        public ReportsControl()
        {
            InitializeComponent();
            LoadSavedShiftTimes();
            LoadAllReports();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(240, 242, 245);

            var lblTitle = new Label
            {
                Text = "Reports",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32),
                Dock = DockStyle.Top,
                Height = 50
            };

            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 10)
            };

            // Tab 1: Low Stock Alert
            var tabLowStock = new TabPage("⚠️ Low Stock Alert");
            tabLowStock.BackColor = Color.White;
            tabLowStock.Padding = new Padding(10);

            var panelLowStockTop = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(245, 247, 250), Padding = new Padding(10) };
            var btnLowStockPdf = CreateExportButton("📄 Export PDF", Color.FromArgb(192, 57, 43));
            var btnLowStockExcel = CreateExportButton("🟩 Export Excel", Color.FromArgb(39, 174, 96));
            btnLowStockPdf.Location = new Point(10, 8);
            btnLowStockExcel.Location = new Point(160, 8);
            btnLowStockPdf.Click += (s, e) => ExportLowStockPdf();
            btnLowStockExcel.Click += (s, e) => ExportLowStockExcel();
            panelLowStockTop.Controls.AddRange(new Control[] { btnLowStockPdf, btnLowStockExcel });

            dgvLowStock = CreateStyledGrid();
            dgvLowStock.CellFormatting += (s, e) =>
            {
                if (dgvLowStock.Columns.Count > 0 && dgvLowStock.Columns[e.ColumnIndex].Name == "Status")
                {
                    e.CellStyle!.ForeColor = Color.FromArgb(231, 76, 60);
                    e.CellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                }
            };
            tabLowStock.Controls.Add(dgvLowStock);
            tabLowStock.Controls.Add(panelLowStockTop);

            // Tab 2: Inventory Summary
            var tabSummary = new TabPage("📊 Inventory Summary");
            tabSummary.BackColor = Color.White;
            tabSummary.Padding = new Padding(10);

            var panelSummaryStats = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(10)
            };

            var btnSummaryPdf = CreateExportButton("📄 Export PDF", Color.FromArgb(192, 57, 43));
            var btnSummaryExcel = CreateExportButton("🟩 Export Excel", Color.FromArgb(39, 174, 96));
            btnSummaryPdf.Location = new Point(10, 8);
            btnSummaryExcel.Location = new Point(160, 8);
            btnSummaryPdf.Click += (s, e) => ExportSummaryPdf();
            btnSummaryExcel.Click += (s, e) => ExportSummaryExcel();
            panelSummaryStats.Controls.AddRange(new Control[] { btnSummaryPdf, btnSummaryExcel });

            dgvSummary = CreateStyledGrid();
            tabSummary.Controls.Add(dgvSummary);
            tabSummary.Controls.Add(panelSummaryStats);

            // =============================================
            // Tab 3: Transaction History (SALES ONLY)
            // =============================================
            var tabHistory = new TabPage("💰 Transaction History");
            tabHistory.BackColor = Color.White;
            tabHistory.Padding = new Padding(10);

            // Summary cards panel
            var panelSalesCards = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(10, 5, 10, 5)
            };

            lblItemsSold = new Label
            {
                Text = "📦 Items Sold: 0",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 73, 94),
                Location = new Point(10, 17),
                AutoSize = true
            };

            lblSalesTotal = new Label
            {
                Text = "💰 Gross: ₱0.00",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(39, 174, 96),
                Location = new Point(200, 17),
                AutoSize = true
            };

            lblProfitTotal = new Label
            {
                Text = "📈 Profit: ₱0.00",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(52, 120, 246),
                Location = new Point(420, 17),
                AutoSize = true
            };

            panelSalesCards.Controls.AddRange(new Control[] { lblItemsSold, lblSalesTotal, lblProfitTotal });

            // Date filter + export
            var panelDateFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(10)
            };

            var lblFrom = new Label
            {
                Text = "From:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 90),
                Location = new Point(10, 18),
                AutoSize = true
            };

            dtpFrom = new DateTimePicker
            {
                Location = new Point(55, 14),
                Size = new Size(200, 28),
                Font = new Font("Segoe UI", 9),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now.AddMonths(-1)
            };
            dtpFrom.ValueChanged += (s, e) => LoadTransactionHistory();

            var lblTo = new Label
            {
                Text = "To:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 90),
                Location = new Point(270, 18),
                AutoSize = true
            };

            dtpTo = new DateTimePicker
            {
                Location = new Point(300, 14),
                Size = new Size(200, 28),
                Font = new Font("Segoe UI", 9),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now
            };
            dtpTo.ValueChanged += (s, e) => LoadTransactionHistory();

            var btnHistoryPdf = CreateExportButton("📄 Export PDF", Color.FromArgb(192, 57, 43));
            var btnHistoryExcel = CreateExportButton("🟩 Export Excel", Color.FromArgb(39, 174, 96));
            btnHistoryPdf.Location = new Point(530, 10);
            btnHistoryExcel.Location = new Point(680, 10);
            btnHistoryPdf.Click += (s, e) => ExportHistoryPdf();
            btnHistoryExcel.Click += (s, e) => ExportHistoryExcel();

            panelDateFilter.Controls.AddRange(new Control[] { lblFrom, dtpFrom, lblTo, dtpTo, btnHistoryPdf, btnHistoryExcel });

            dgvHistory = CreateStyledGrid();

            // Color code profit column
            dgvHistory.CellFormatting += (s, e) =>
            {
                if (dgvHistory.Columns.Count > 0 && e.ColumnIndex >= 0)
                {
                    var colName = dgvHistory.Columns[e.ColumnIndex].Name;
                    if (colName == "Profit" && e.Value is double profitVal)
                    {
                        e.CellStyle!.ForeColor = profitVal >= 0 ? Color.FromArgb(39, 174, 96) : Color.FromArgb(231, 76, 60);
                        e.CellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                    }
                    if (colName == "Payment")
                    {
                        var val = e.Value?.ToString() ?? "";
                        e.CellStyle!.ForeColor = Color.FromArgb(27, 94, 32);
                        e.CellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                    }
                }
            };

            tabHistory.Controls.Add(dgvHistory);
            tabHistory.Controls.Add(panelSalesCards);
            tabHistory.Controls.Add(panelDateFilter);

            // =============================================
            // Tab 4: Stock History (Stock IN/OUT only)
            // =============================================
            var tabStockHistory = new TabPage("🔄 Stock History");
            tabStockHistory.BackColor = Color.White;
            tabStockHistory.Padding = new Padding(10);

            var panelStockDateFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(10)
            };

            var lblStockFrom = new Label
            {
                Text = "From:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 90),
                Location = new Point(10, 18),
                AutoSize = true
            };

            dtpStockFrom = new DateTimePicker
            {
                Location = new Point(55, 14),
                Size = new Size(200, 28),
                Font = new Font("Segoe UI", 9),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now.AddMonths(-1)
            };
            dtpStockFrom.ValueChanged += (s, e) => LoadStockHistory();

            var lblStockTo = new Label
            {
                Text = "To:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 90),
                Location = new Point(270, 18),
                AutoSize = true
            };

            dtpStockTo = new DateTimePicker
            {
                Location = new Point(300, 14),
                Size = new Size(200, 28),
                Font = new Font("Segoe UI", 9),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now
            };
            dtpStockTo.ValueChanged += (s, e) => LoadStockHistory();

            var btnStockPdf = CreateExportButton("📄 Export PDF", Color.FromArgb(192, 57, 43));
            var btnStockExcel = CreateExportButton("🟩 Export Excel", Color.FromArgb(39, 174, 96));
            btnStockPdf.Location = new Point(530, 10);
            btnStockExcel.Location = new Point(680, 10);
            btnStockPdf.Click += (s, e) => ExportStockHistoryPdf();
            btnStockExcel.Click += (s, e) => ExportStockHistoryExcel();

            panelStockDateFilter.Controls.AddRange(new Control[] { lblStockFrom, dtpStockFrom, lblStockTo, dtpStockTo, btnStockPdf, btnStockExcel });

            dgvStockHistory = CreateStyledGrid();

            // Color code IN/OUT in stock history
            dgvStockHistory.CellFormatting += (s, e) =>
            {
                if (dgvStockHistory.Columns.Count > 0 && e.ColumnIndex >= 0 && dgvStockHistory.Columns[e.ColumnIndex].Name == "Type")
                {
                    if (e.Value?.ToString() == "IN")
                    {
                        e.CellStyle!.ForeColor = Color.FromArgb(39, 174, 96);
                        e.CellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                    }
                    else if (e.Value?.ToString() == "OUT")
                    {
                        e.CellStyle!.ForeColor = Color.FromArgb(231, 76, 60);
                        e.CellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
                    }
                }
            };

            tabStockHistory.Controls.Add(dgvStockHistory);
            tabStockHistory.Controls.Add(panelStockDateFilter);

            // =============================================
            // Tab 5: Shift Sales Report
            // =============================================
            var tabShiftSales = new TabPage("📊 Shift Sales Report");
            tabShiftSales.BackColor = Color.White;
            tabShiftSales.Padding = new Padding(10);

            // Row 1: Date and shift time filters
            var panelShiftFilter = new Panel { Dock = DockStyle.Top, Height = 90, BackColor = Color.FromArgb(245, 247, 250), Padding = new Padding(10) };
            
            var lblShiftDate = new Label { Text = "Date:", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(60, 70, 90), Location = new Point(10, 12), AutoSize = true };
            dtpShiftDate = new DateTimePicker { Location = new Point(55, 8), Size = new Size(130, 28), Font = new Font("Segoe UI", 9), Format = DateTimePickerFormat.Short, Value = DateTime.Today };
            dtpShiftDate.ValueChanged += (s, e) => LoadShiftSales();

            var lblMorningStart = new Label { Text = "Morning Start:", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(39, 174, 96), Location = new Point(200, 12), AutoSize = true };
            dtpMorningStart = new DateTimePicker { Location = new Point(300, 8), Size = new Size(95, 28), Font = new Font("Segoe UI", 9), Format = DateTimePickerFormat.Custom, CustomFormat = "hh:mm tt", ShowUpDown = true, Value = DateTime.Today.AddHours(8) };
            dtpMorningStart.ValueChanged += (s, e) => { 
                SettingsRepository.SaveSetting("MorningStart", dtpMorningStart.Value.ToString("HH:mm"));
                LoadShiftSales(); 
            };

            var lblNightStart = new Label { Text = "Night Start:", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(230, 126, 34), Location = new Point(410, 12), AutoSize = true };
            dtpNightStart = new DateTimePicker { Location = new Point(498, 8), Size = new Size(95, 28), Font = new Font("Segoe UI", 9), Format = DateTimePickerFormat.Custom, CustomFormat = "hh:mm tt", ShowUpDown = true, Value = DateTime.Today.AddHours(16) };
            dtpNightStart.ValueChanged += (s, e) => {
                SettingsRepository.SaveSetting("NightStart", dtpNightStart.Value.ToString("HH:mm"));
                LoadShiftSales();
            };

            var lblNightEnd = new Label { Text = "Night End:", Font = new Font("Segoe UI", 9, FontStyle.Bold), ForeColor = Color.FromArgb(231, 76, 60), Location = new Point(608, 12), AutoSize = true };
            dtpNightEnd = new DateTimePicker { Location = new Point(690, 8), Size = new Size(95, 28), Font = new Font("Segoe UI", 9), Format = DateTimePickerFormat.Custom, CustomFormat = "hh:mm tt", ShowUpDown = true, Value = DateTime.Today.AddHours(2).AddMinutes(30) };
            dtpNightEnd.ValueChanged += (s, e) => {
                SettingsRepository.SaveSetting("NightEnd", dtpNightEnd.Value.ToString("HH:mm"));
                LoadShiftSales();
            };

            // Row 2: Export buttons — Morning & Night separate
            var btnMorningExcel = new Button
            {
                Text = "☀️ Export Morning Shift",
                Width = 180, Height = 32,
                Location = new Point(10, 48),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(39, 174, 96),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnMorningExcel.FlatAppearance.BorderSize = 0;
            btnMorningExcel.Click += (s, e) => ExportShiftExcel("Morning");

            var btnNightExcel = new Button
            {
                Text = "🌙 Export Night Shift",
                Width = 180, Height = 32,
                Location = new Point(200, 48),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(230, 126, 34),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnNightExcel.FlatAppearance.BorderSize = 0;
            btnNightExcel.Click += (s, e) => ExportShiftExcel("Night");

            var btnBothExcel = new Button
            {
                Text = "📊 Export Both Shifts",
                Width = 180, Height = 32,
                Location = new Point(390, 48),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = Color.FromArgb(52, 73, 94),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnBothExcel.FlatAppearance.BorderSize = 0;
            btnBothExcel.Click += (s, e) => ExportShiftExcel("Both");

            panelShiftFilter.Controls.AddRange(new Control[] { 
                lblShiftDate, dtpShiftDate, lblMorningStart, dtpMorningStart, lblNightStart, dtpNightStart, lblNightEnd, dtpNightEnd,
                btnMorningExcel, btnNightExcel, btnBothExcel 
            });
            dgvShiftSales = CreateStyledGrid();
            tabShiftSales.Controls.Add(dgvShiftSales);
            tabShiftSales.Controls.Add(panelShiftFilter);

            tabControl.TabPages.AddRange(new TabPage[] { tabLowStock, tabSummary, tabHistory, tabStockHistory, tabShiftSales });

            this.Controls.Add(tabControl);
            this.Controls.Add(lblTitle);
        }

        private Button CreateExportButton(string text, Color backColor)
        {
            var btn = new Button
            {
                Text = text,
                Size = new Size(130, 35),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = backColor,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private DataGridView CreateStyledGrid()
        {
            var dgv = new DoubleBufferedDataGridView
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
            dgv.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 235, 250);
            dgv.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgv.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(27, 94, 32);
            dgv.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgv.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgv.EnableHeadersVisualStyles = false;
            dgv.ColumnHeadersHeight = 35;
            dgv.RowTemplate.Height = 30;
            return dgv;
        }

        private void LoadAllReports()
        {
            LoadLowStock();
            LoadInventorySummary();
            LoadTransactionHistory();
            LoadStockHistory();
            LoadShiftSales();
        }

        private void LoadSavedShiftTimes()
        {
            var morningStart = SettingsRepository.GetSetting("MorningStart");
            var nightStart = SettingsRepository.GetSetting("NightStart");
            var nightEnd = SettingsRepository.GetSetting("NightEnd");

            if (TimeSpan.TryParse(morningStart, out var mStart))
                dtpMorningStart.Value = DateTime.Today.Date.Add(mStart);
            
            if (TimeSpan.TryParse(nightStart, out var nStart))
                dtpNightStart.Value = DateTime.Today.Date.Add(nStart);

            if (TimeSpan.TryParse(nightEnd, out var nEnd))
                dtpNightEnd.Value = DateTime.Today.Date.Add(nEnd);
        }

        private void LoadLowStock()
        {
            var lowStock = _productRepo.GetLowStockProducts();
            if (SessionManager.CurrentSection == "Eatery")
            {
                dgvLowStock.DataSource = lowStock.Select(p => new
                {
                    Item = p.Name,
                    Category = p.CategoryName,
                    CurrentQty = p.Quantity,
                    MinStock = p.MinStockLevel,
                    Status = p.Quantity == 0 ? "OUT OF STOCK" : "LOW STOCK"
                }).ToList();
            }
            else
            {
                dgvLowStock.DataSource = lowStock.Select(p => new
                {
                    p.SKU,
                    p.Name,
                    Category = p.CategoryName,
                    CurrentQty = p.Quantity,
                    MinStock = p.MinStockLevel,
                    p.Unit,
                    Status = p.Quantity == 0 ? "OUT OF STOCK" : "LOW STOCK"
                }).ToList();
            }
        }

        private void LoadInventorySummary()
        {
            var categories = _categoryRepo.GetAll();
            var products = _productRepo.GetAll();
            bool isEatery = SessionManager.CurrentSection == "Eatery";

            if (isEatery)
            {
                // Eatery: Show menu items count and price info (no quantity-based totals)
                var summary = categories.Select(c =>
                {
                    var catProducts = products.Where(p => p.CategoryId == c.Id).ToList();
                    var trackedProducts = catProducts.Where(p => p.Quantity > 0).ToList();
                    return new
                    {
                        Category = c.Name,
                        MenuItems = catProducts.Count,
                        AvgPrice = catProducts.Count > 0 ? $"₱{catProducts.Average(p => (double)p.Price):N2}" : "—",
                        AvgPuhunan = catProducts.Count > 0 ? $"₱{catProducts.Average(p => (double)p.CostPrice):N2}" : "—"
                    };
                }).ToList();

                var uncategorized = products.Where(p => p.CategoryId == 0).ToList();
                if (uncategorized.Count > 0)
                {
                    var trackedUn = uncategorized.Where(p => p.Quantity > 0).ToList();
                    summary.Add(new
                    {
                        Category = "Uncategorized",
                        MenuItems = uncategorized.Count,
                        AvgPrice = uncategorized.Count > 0 ? $"₱{uncategorized.Average(p => (double)p.Price):N2}" : "—",
                        AvgPuhunan = uncategorized.Count > 0 ? $"₱{uncategorized.Average(p => (double)p.CostPrice):N2}" : "—"
                    });
                }

                dgvSummary.DataSource = summary;
            }
            else
            {
                // Store: Show full stock-based summary
                var summary = categories.Select(c =>
                {
                    var catProducts = products.Where(p => p.CategoryId == c.Id).ToList();
                    return new
                    {
                        Category = c.Name,
                        TotalItems = catProducts.Count,
                        TotalQty = catProducts.Sum(p => p.Quantity),
                        TotalValue = $"₱{catProducts.Sum(p => p.Price * p.Quantity):N2}",
                        TotalCost = $"₱{catProducts.Sum(p => p.CostPrice * p.Quantity):N2}",
                        LowStock = catProducts.Count(p => p.Quantity <= p.MinStockLevel)
                    };
                }).ToList();

                var uncategorized = products.Where(p => p.CategoryId == 0).ToList();
                if (uncategorized.Count > 0)
                {
                    summary.Add(new
                    {
                        Category = "Uncategorized",
                        TotalItems = uncategorized.Count,
                        TotalQty = uncategorized.Sum(p => p.Quantity),
                        TotalValue = $"₱{uncategorized.Sum(p => p.Price * p.Quantity):N2}",
                        TotalCost = $"₱{uncategorized.Sum(p => p.CostPrice * p.Quantity):N2}",
                        LowStock = uncategorized.Count(p => p.Quantity <= p.MinStockLevel)
                    });
                }

                dgvSummary.DataSource = summary;
            }
        }

        /// <summary>
        /// Loads ONLY sold items from SalesItems + SalesTransactions (no stock IN/OUT)
        /// </summary>
        private void LoadTransactionHistory()
        {
            var (from, to) = NormalizeDateRange(dtpFrom.Value, dtpTo.Value);
            // Fetch one extra day to include the night shift carry-over for the last day in the range
            var details = _salesRepo.GetSalesItemsDetail(from, to.AddDays(1));

            var morningCutoff = dtpMorningStart.Value.TimeOfDay;
            var nightEndCutoff = dtpNightEnd.Value.TimeOfDay;
            var actualToDate = to.Date;
            var carryOverDate = to.Date.AddDays(1);

            details = details.Where(d =>
            {
                if (!DateTime.TryParse(d.Date, out var txDate)) return true;
                
                // 1. Exclude early morning of the first day
                if (txDate.Date == from.Date && txDate.TimeOfDay < morningCutoff)
                    return false;
                
                // 2. Only include the "carry-over" night shift on the day after the range ends
                if (txDate.Date == carryOverDate)
                    return txDate.TimeOfDay <= nightEndCutoff;
                
                // 3. Keep everything else within the range
                return txDate.Date <= actualToDate;
            }).ToList();

            // Update summary cards
            int totalItems = details.Sum(d => d.QtySold);
            double totalSales = details.Sum(d => d.Subtotal);
            double totalProfit = details.Sum(d => d.Profit);

            lblItemsSold.Text = $"📦 Items Sold: {totalItems:N0}";
            lblSalesTotal.Text = $"💰 Gross: ₱{totalSales:N2}";
            lblProfitTotal.Text = $"📈 Profit: ₱{totalProfit:N2}";

            dgvHistory.DataSource = details
                .OrderBy(d => DateTime.Parse(d.Date))
                .Select(d => new
                {
                    Date = DateTime.Parse(d.Date).ToString("yyyy-MM-dd"),
                    Time = DateTime.Parse(d.Date).ToString("hh:mm tt"),
                    Order = d.OrderNumber,
                    Product = d.ProductName,
                    Qty = d.QtySold,
                    Price = d.UnitPrice,
                    Subtotal = d.Subtotal,
                    Cashier = d.Cashier,
                    Status = d.IsCancelled ? "❌ CANCELLED" : "✅ ACTIVE"
                }).ToList();

            // Format currency columns
            if (dgvHistory.Columns.Count > 0)
            {
                if (dgvHistory.Columns.Contains("Price"))
                    dgvHistory.Columns["Price"].DefaultCellStyle.Format = "₱#,##0.00";
                if (dgvHistory.Columns.Contains("Subtotal"))
                    dgvHistory.Columns["Subtotal"].DefaultCellStyle.Format = "₱#,##0.00";
            }

            // Force immediate UI refresh
            dgvHistory.Refresh();
            
            // Highlight cancelled rows in Red
            foreach (DataGridViewRow row in dgvHistory.Rows)
            {
                if (row.Cells["Status"].Value?.ToString()?.Contains("CANCELLED") == true)
                {
                    row.DefaultCellStyle.ForeColor = Color.Red;
                    row.DefaultCellStyle.SelectionForeColor = Color.Red;
                }
            }
            this.Update();
        }

        /// <summary>
        /// Loads ONLY manual stock IN/OUT records (excludes auto-generated "Sold" entries)
        /// </summary>
        private void LoadStockHistory()
        {
            var allTransactions = _transRepo.GetAll(dtpStockFrom.Value, dtpStockTo.Value);

            // Filter OUT the auto-generated "Sold (Order #...)" entries so only manual stock adjustments show
            var stockOnly = allTransactions
                .Where(t => !t.Notes.StartsWith("Sold (Order"))
                .ToList();

            dgvStockHistory.DataSource = stockOnly.Select(t => new
            {
                Date = DateTime.TryParse(t.TransactionDate, out var dt) ? dt.ToString("yyyy-MM-dd hh:mm tt") : t.TransactionDate,
                Product = t.ProductName,
                t.Type,
                t.Quantity,
                t.Notes,
                User = t.UserName
            }).ToList();

            // Force immediate UI refresh
            dgvStockHistory.Refresh();
            this.Update();
        }

        // ================== EXPORT LOGIC ==================

        private void ExportLowStockPdf()
        {
            var lowStock = _productRepo.GetLowStockProducts();
            if (lowStock.Count == 0) { MessageBox.Show("No low stock items to export.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            
            var path = ExportHelper.ShowSaveDialog("PDF Files|*.pdf", "LowStockAlert.pdf");
            if (path != null)
            {
                ExportHelper.ExportProductsToPdf(lowStock, path);
                MessageBox.Show("PDF Exported Successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ExportLowStockExcel()
        {
            if (dgvLowStock.DataSource == null) return;
            var path = ExportHelper.ShowSaveDialog("Excel Files|*.xlsx", "LowStockAlert.xlsx");
            if (path != null)
            {
                if (SessionManager.CurrentSection == "Eatery")
                {
                    var list = _productRepo.GetLowStockProducts().Select(p => new {
                        Item = p.Name, Category = p.CategoryName,
                        CurrentStock = p.Quantity, MinimumStock = p.MinStockLevel
                    }).ToList();
                    ExportHelper.ExportToExcel(list, "Low Stock", path);
                }
                else
                {
                    var list = _productRepo.GetLowStockProducts().Select(p => new {
                        SKU = p.SKU, Product = p.Name, Category = p.CategoryName,
                        CurrentStock = p.Quantity, MinimumStock = p.MinStockLevel, Unit = p.Unit
                    }).ToList();
                    ExportHelper.ExportToExcel(list, "Low Stock", path);
                }
                MessageBox.Show("Excel Exported Successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ExportSummaryPdf()
        {
            var allProducts = _productRepo.GetAll();
            if (allProducts.Count == 0) { MessageBox.Show("No inventory to export.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            
            var path = ExportHelper.ShowSaveDialog("PDF Files|*.pdf", "InventorySummary.pdf");
            if (path != null)
            {
                ExportHelper.ExportProductsToPdf(allProducts, path);
                MessageBox.Show("PDF Exported Successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ExportSummaryExcel()
        {
            if (dgvSummary.DataSource == null) return;
            var path = ExportHelper.ShowSaveDialog("Excel Files|*.xlsx", "InventorySummary.xlsx");
            if (path != null)
            {
                var categories = _categoryRepo.GetAll();
                var products = _productRepo.GetAll();
                bool isEatery = SessionManager.CurrentSection == "Eatery";
                string summarySheetName;

                if (isEatery)
                {
                    var summary = categories.Select(c => {
                        var catProducts = products.Where(p => p.CategoryId == c.Id).ToList();
                        return new {
                            Category = c.Name,
                            MenuItems = catProducts.Count,
                            AvgPrice = catProducts.Count > 0 ? catProducts.Average(p => (double)p.Price) : 0,
                            AvgPuhunan = catProducts.Count > 0 ? catProducts.Average(p => (double)p.CostPrice) : 0
                        };
                    }).ToList();
                    summarySheetName = "Menu Summary";
                    ExportHelper.ExportToExcel(summary, summarySheetName, path);
                }
                else
                {
                    var summary = categories.Select(c => {
                        var catProducts = products.Where(p => p.CategoryId == c.Id).ToList();
                        return new {
                            Category = c.Name,
                            TotalItems = catProducts.Count,
                            TotalQty = catProducts.Sum(p => p.Quantity),
                            TotalValue = catProducts.Sum(p => p.Price * p.Quantity),
                            TotalCost = catProducts.Sum(p => p.CostPrice * p.Quantity),
                            LowStockItems = catProducts.Count(p => p.Quantity <= p.MinStockLevel)
                        };
                    }).ToList();
                    summarySheetName = "Inventory Summary";
                    ExportHelper.ExportToExcel(summary, summarySheetName, path);
                }

                // Add detailed sold-items analytics sheet (all-time for current section)
                var soldDetails = _salesRepo.GetSalesItemsDetail(DateTime.Today.AddYears(-20), DateTime.Today.AddDays(1));
                var soldItemSummary = soldDetails
                    .GroupBy(d => d.ProductName)
                    .Select(g =>
                    {
                        var topCashier = g.GroupBy(x => x.Cashier)
                            .OrderByDescending(x => x.Sum(y => y.QtySold))
                            .Select(x => x.Key)
                            .FirstOrDefault() ?? "N/A";

                        DateTime lastSold = g
                            .Select(x => DateTime.TryParse(x.Date, out var parsed) ? parsed : DateTime.MinValue)
                            .Max();

                        return new
                        {
                            Product = g.Key,
                            QtySold = g.Sum(x => x.QtySold),
                            Orders = g.Select(x => x.OrderNumber).Distinct().Count(),
                            GrossSales = g.Sum(x => x.Subtotal),
                            NetProfit = g.Sum(x => x.Profit),
                            AvgUnitPrice = g.Average(x => x.UnitPrice),
                            TopCashier = topCashier,
                            LastSold = lastSold == DateTime.MinValue ? "N/A" : lastSold.ToString("yyyy-MM-dd hh:mm tt")
                        };
                    })
                    .OrderByDescending(x => x.QtySold)
                    .ToList();

                ExportHelper.AddSheetToExcel(path, "Items Sold", soldItemSummary);
                
                MessageBox.Show("Excel Exported Successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // ===== Transaction History (Sales) Export =====

        private void ExportHistoryPdf()
        {
            var (from, to) = NormalizeDateRange(dtpFrom.Value, dtpTo.Value);
            var details = _salesRepo.GetSalesItemsDetail(from, to.AddDays(1));
            
            var morningCutoff = dtpMorningStart.Value.TimeOfDay;
            var nightEndCutoff = dtpNightEnd.Value.TimeOfDay;
            var actualToDate = to.Date;
            var carryOverDate = to.Date.AddDays(1);

            details = details.Where(d =>
            {
                if (!DateTime.TryParse(d.Date, out var txDate)) return true;
                if (txDate.Date == from.Date && txDate.TimeOfDay < morningCutoff) return false;
                if (txDate.Date == carryOverDate) return txDate.TimeOfDay <= nightEndCutoff;
                return txDate.Date <= actualToDate;
            }).ToList();

            if (details.Count == 0) { MessageBox.Show("No sales data to export.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            
            var path = ExportHelper.ShowSaveDialog("PDF Files|*.pdf", $"SalesHistory_{from:yyyyMMdd}_{to:yyyyMMdd}.pdf");
            if (path != null)
            {
                ExportHelper.ExportSalesHistoryPdf(details, from, to, path);
                MessageBox.Show("PDF Exported Successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ExportHistoryExcel()
        {
            var (from, to) = NormalizeDateRange(dtpFrom.Value, dtpTo.Value);
            var details = _salesRepo.GetSalesItemsDetail(from, to.AddDays(1));
            
            var morningCutoff = dtpMorningStart.Value.TimeOfDay;
            var nightEndCutoff = dtpNightEnd.Value.TimeOfDay;
            var actualToDate = to.Date;
            var carryOverDate = to.Date.AddDays(1);

            details = details.Where(d =>
            {
                if (!DateTime.TryParse(d.Date, out var txDate)) return true;
                if (txDate.Date == from.Date && txDate.TimeOfDay < morningCutoff) return false;
                if (txDate.Date == carryOverDate) return txDate.TimeOfDay <= nightEndCutoff;
                return txDate.Date <= actualToDate;
            }).ToList();

            if (details.Count == 0) { MessageBox.Show("No sales data to export.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            
            var path = ExportHelper.ShowSaveDialog("Excel Files|*.xlsx", $"SalesHistory_{from:yyyyMMdd}_{to:yyyyMMdd}.xlsx");
            if (path != null)
            {
                ExportHelper.ExportSalesHistoryExcel(details, from, to, path);
                MessageBox.Show("Excel Exported Successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        // ===== Stock History Export =====

        private void ExportStockHistoryPdf()
        {
            var allTransactions = _transRepo.GetAll(dtpStockFrom.Value, dtpStockTo.Value);
            var stockOnly = allTransactions.Where(t => !t.Notes.StartsWith("Sold (Order")).ToList();
            if (stockOnly.Count == 0) { MessageBox.Show("No stock transactions to export.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            
            var path = ExportHelper.ShowSaveDialog("PDF Files|*.pdf", $"StockHistory_{dtpStockFrom.Value:yyyyMMdd}_{dtpStockTo.Value:yyyyMMdd}.pdf");
            if (path != null)
            {
                ExportHelper.ExportTransactionsToPdf(stockOnly, path);
                MessageBox.Show("PDF Exported Successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ExportStockHistoryExcel()
        {
            var allTransactions = _transRepo.GetAll(dtpStockFrom.Value, dtpStockTo.Value);
            var stockOnly = allTransactions.Where(t => !t.Notes.StartsWith("Sold (Order")).ToList();
            if (stockOnly.Count == 0) { MessageBox.Show("No stock transactions to export.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            
            var path = ExportHelper.ShowSaveDialog("Excel Files|*.xlsx", $"StockHistory_{dtpStockFrom.Value:yyyyMMdd}_{dtpStockTo.Value:yyyyMMdd}.xlsx");
            if (path != null)
            {
                ExportHelper.ExportStockHistoryExcel(stockOnly, dtpStockFrom.Value, dtpStockTo.Value, path);
                MessageBox.Show("Excel Exported Successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        // ===== Shift Sales Report Export =====
        private void LoadShiftSales()
        {
            if (dgvShiftSales == null) return;

            DateTime shiftDate = dtpShiftDate.Value.Date;
            DateTime nextDay = shiftDate.AddDays(1);

            var details = _salesRepo.GetShiftSalesDetails(shiftDate, nextDay);
            TimeSpan morningStart = dtpMorningStart.Value.TimeOfDay;
            TimeSpan nightStart = dtpNightStart.Value.TimeOfDay;
            var (morningDetails, nightDetails) = SplitShiftDetails(details, shiftDate, morningStart, nightStart, dtpNightEnd.Value.TimeOfDay);

            var displayList = morningDetails
                .Select(d => new { Shift = "☀️ Morning", Item = d })
                .Concat(nightDetails.Select(d => new { Shift = "🌙 Night", Item = d }))
                .Select(x => new
                {
                    Shift = x.Shift,
                    Product = x.Item.ProductName,
                    SellingPrice = x.Item.SellingPrice,
                    Sold = x.Item.QtySold,
                    GrossIncome = x.Item.GrossIncome,
                    NetIncome = x.Item.NetIncome,
                    Time = TryParseTransactionTime(x.Item.TransactionTime, out DateTime t)
                        ? t.ToString("hh:mm tt")
                        : x.Item.TransactionTime
                })
                .ToList();

            dgvShiftSales.DataSource = displayList;

            // Format currency columns and add shift color coding
            if (dgvShiftSales.Columns.Count > 0)
            {
                if (dgvShiftSales.Columns.Contains("SellingPrice"))
                    dgvShiftSales.Columns["SellingPrice"].DefaultCellStyle.Format = "₱#,##0.00";
                if (dgvShiftSales.Columns.Contains("GrossIncome"))
                    dgvShiftSales.Columns["GrossIncome"].DefaultCellStyle.Format = "₱#,##0.00";
                if (dgvShiftSales.Columns.Contains("NetIncome"))
                    dgvShiftSales.Columns["NetIncome"].DefaultCellStyle.Format = "₱#,##0.00";
            }

            // Color-code rows by shift
            foreach (DataGridViewRow row in dgvShiftSales.Rows)
            {
                if (row.IsNewRow) continue;
                var shiftVal = row.Cells["Shift"]?.Value?.ToString() ?? "";
                if (shiftVal.Contains("Night"))
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(255, 248, 235);
                }
                else
                {
                    row.DefaultCellStyle.BackColor = Color.FromArgb(235, 250, 240);
                }
            }

            // Force immediate UI refresh
            dgvShiftSales.Refresh();
            this.Update();
        }

        private void ExportShiftExcel(string shiftType)
        {
            DateTime shiftDate = dtpShiftDate.Value.Date;
            DateTime nextDay = shiftDate.AddDays(1);
            var details = _salesRepo.GetShiftSalesDetails(shiftDate, nextDay);

            if (details.Count == 0)
            {
                MessageBox.Show("No data to export.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            TimeSpan morningStart = dtpMorningStart.Value.TimeOfDay;
            TimeSpan nightStart = dtpNightStart.Value.TimeOfDay;
            var (morningDetails, nightDetails) = SplitShiftDetails(details, shiftDate, morningStart, nightStart, dtpNightEnd.Value.TimeOfDay);
            var cancelledDetails = _salesRepo.GetCancelledOrders(shiftDate, nextDay);

            if (shiftType == "Morning" && morningDetails.Count == 0)
            {
                MessageBox.Show($"No Morning shift data ({dtpMorningStart.Value:hh:mm tt} – {dtpNightStart.Value:hh:mm tt}).", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (shiftType == "Night" && nightDetails.Count == 0)
            {
                MessageBox.Show($"No Night shift data ({dtpNightStart.Value:hh:mm tt} onwards).", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string fileName = shiftType switch
            {
                "Morning" => $"MorningShift_{shiftDate:yyyyMMdd}.xlsx",
                "Night" => $"NightShift_{shiftDate:yyyyMMdd}.xlsx",
                _ => $"ShiftSalesReport_{shiftDate:yyyyMMdd}.xlsx"
            };

            var path = ExportHelper.ShowSaveDialog("Excel Files|*.xlsx", fileName);
            if (path != null)
            {
                ExportHelper.ExportShiftSalesExcel(morningDetails, nightDetails, cancelledDetails, path, shiftType);
                MessageBox.Show($"{shiftType} Shift Export Successful!\n\nMorning: {morningDetails.Count} items\nNight: {nightDetails.Count} items\nCancelled: {cancelledDetails.Count} items", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private static (List<ShiftSalesDetail> Morning, List<ShiftSalesDetail> Night) SplitShiftDetails(
            List<ShiftSalesDetail> details,
            DateTime shiftDate,
            TimeSpan morningStart,
            TimeSpan nightStart,
            TimeSpan nightEnd)
        {
            var morning = new List<ShiftSalesDetail>();
            var night = new List<ShiftSalesDetail>();
            var date = shiftDate.Date;
            var nextDate = shiftDate.AddDays(1).Date;

            foreach (var d in details)
            {
                if (!TryParseTransactionTime(d.TransactionTime, out DateTime txTime))
                    continue;

                var tod = txTime.TimeOfDay;

                if (txTime.Date == date)
                {
                    if (tod >= morningStart && tod < nightStart)
                        morning.Add(d);
                    else if (tod >= nightStart)
                        night.Add(d);
                    // before morningStart on shiftDate = ignored (prior night's carryover)
                }
                else if (txTime.Date == nextDate && tod < morningStart)
                {
                    // Only include if within the night end cutoff
                    if (tod <= nightEnd)
                        night.Add(d);
                }
            }

            return (AggregateProducts(morning), AggregateProducts(night));
        }

        private static List<ShiftSalesDetail> AggregateProducts(List<ShiftSalesDetail> raw)
        {
            return raw
                .GroupBy(d => new { d.ProductName, d.CategoryName, d.BuyingPrice, d.SellingPrice })
                .Select(g => new ShiftSalesDetail
                {
                    ProductName = g.Key.ProductName,
                    CategoryName = g.Key.CategoryName,
                    BuyingPrice = g.Key.BuyingPrice,
                    SellingPrice = g.Key.SellingPrice,
                    QtySold = g.Sum(x => x.QtySold),
                    TransactionTime = g.First().TransactionTime
                })
                .OrderBy(d => d.ProductName)
                .ToList();
        }

        private static bool TryParseTransactionTime(string raw, out DateTime parsed)
        {
            if (DateTime.TryParseExact(raw,
                    new[] { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd H:mm:ss", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ss.fff" },
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out parsed))
            {
                return true;
            }

            return DateTime.TryParse(raw, out parsed);
        }

        private static (DateTime From, DateTime To) NormalizeDateRange(DateTime from, DateTime to)
        {
            var start = from.Date;
            var end = to.Date;
            if (start > end) (start, end) = (end, start);
            return (start, end);
        }
    }

    /// <summary>
    /// A DataGridView subclass with DoubleBuffered enabled to prevent
    /// visual glitches (flickering, sticky headers) when scrolling.
    /// </summary>
    internal class DoubleBufferedDataGridView : DataGridView
    {
        public DoubleBufferedDataGridView()
        {
            this.DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
        }
    }
}


