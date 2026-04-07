using TransFundInventory.Data;
using TransFundInventory.Helpers;
using TransFundInventory.Models;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Windows.Forms;
using System;

namespace TransFundInventory.Forms
{
    public class ReportsControl : UserControl
    {
        private TabControl tabControl = null!;
        private DataGridView dgvLowStock = null!;
        private DataGridView dgvSummary = null!;
        private DataGridView dgvHistory = null!;
        private DateTimePicker dtpFrom = null!;
        private DateTimePicker dtpTo = null!;

        private readonly ProductRepository _productRepo = new();
        private readonly StockTransactionRepository _transRepo = new();
        private readonly CategoryRepository _categoryRepo = new();

        public ReportsControl()
        {
            InitializeComponent();
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

            // Tab 3: Transaction History
            var tabHistory = new TabPage("📋 Transaction History");
            tabHistory.BackColor = Color.White;
            tabHistory.Padding = new Padding(10);

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

            tabHistory.Controls.Add(dgvHistory);
            tabHistory.Controls.Add(panelDateFilter);

            tabControl.TabPages.AddRange(new TabPage[] { tabLowStock, tabSummary, tabHistory });

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
            var dgv = new DataGridView
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

        private void LoadTransactionHistory()
        {
            var transactions = _transRepo.GetAll(dtpFrom.Value, dtpTo.Value);
            dgvHistory.DataSource = transactions.Select(t => new
            {
                Date = t.TransactionDate,
                Product = t.ProductName,
                t.Type,
                t.Quantity,
                t.Notes,
                User = t.UserName
            }).ToList();
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
                    ExportHelper.ExportToExcel(summary, "Menu Summary", path);
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
                    ExportHelper.ExportToExcel(summary, "Inventory Summary", path);
                }
                
                MessageBox.Show("Excel Exported Successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ExportHistoryPdf()
        {
            var trans = _transRepo.GetAll(dtpFrom.Value, dtpTo.Value);
            if (trans.Count == 0) { MessageBox.Show("No transactions to export.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            
            var path = ExportHelper.ShowSaveDialog("PDF Files|*.pdf", "TransactionHistory.pdf");
            if (path != null)
            {
                ExportHelper.ExportTransactionsToPdf(trans, path);
                MessageBox.Show("PDF Exported Successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void ExportHistoryExcel()
        {
            var trans = _transRepo.GetAll(dtpFrom.Value, dtpTo.Value);
            if (trans.Count == 0) { MessageBox.Show("No transactions to export.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
            
            var path = ExportHelper.ShowSaveDialog("Excel Files|*.xlsx", "TransactionHistory.xlsx");
            if (path != null)
            {
                var list = trans.Select(t => new {
                    Date = t.TransactionDate,
                    Product = t.ProductName,
                    Type = t.Type,
                    Quantity = t.Quantity,
                    Notes = t.Notes,
                    ProcessedBy = t.UserName
                }).ToList();
                ExportHelper.ExportToExcel(list, "Transactions", path);
                MessageBox.Show("Excel Exported Successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
