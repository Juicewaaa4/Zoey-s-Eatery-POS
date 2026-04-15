using TransFundInventory.Data;
using TransFundInventory.Helpers;
using TransFundInventory.Models;

namespace TransFundInventory.Forms
{
    public class StockTransactionControl : UserControl
    {
        private ComboBox cmbProduct = null!;
        private ComboBox cmbType = null!;
        private NumericUpDown nudQuantity = null!;
        private TextBox txtNotes = null!;
        private DataGridView dgvTransactions = null!;
        private Label lblCurrentStock = null!;
        private readonly ProductRepository _productRepo = new();
        private readonly CategoryRepository _categoryRepo = new();
        private readonly StockTransactionRepository _transRepo = new();
        private readonly AuditLogRepository _auditRepo = new();
        private ComboBox cmbCategory = null!;
        private TextBox txtSearchProduct = null!;
        private TextBox txtSearchHistory = null!;
        private DateTimePicker dtpHistoryDate = null!;

        public StockTransactionControl()
        {
            InitializeComponent();
            LoadCategories();
            LoadProducts();
            LoadTransactions();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(240, 242, 245);

            var lblTitle = new Label
            {
                Text = "Stock In / Out",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32),
                Dock = DockStyle.Top,
                Height = 50
            };

            // Form panel
            var panelForm = new Panel
            {
                Dock = DockStyle.Top,
                Height = 160,
                BackColor = Color.White,
                Padding = new Padding(20, 15, 20, 15)
            };

            var lblSearchProd = new Label
            {
                Text = "🔍 Search Product",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 90),
                Location = new Point(20, 15),
                AutoSize = true
            };

            txtSearchProduct = new TextBox
            {
                Location = new Point(20, 38),
                Size = new Size(160, 28),
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "Type name/Code...",
                BorderStyle = BorderStyle.FixedSingle
            };
            txtSearchProduct.TextChanged += TxtSearchProduct_TextChanged;

            var lblCategory = new Label
            {
                Text = "Category",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 90),
                Location = new Point(190, 15),
                AutoSize = true
            };

            cmbCategory = new ComboBox
            {
                Location = new Point(190, 38),
                Size = new Size(150, 28),
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbCategory.SelectedIndexChanged += CmbCategory_Changed;

            var lblProduct = new Label
            {
                Text = "Product *",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 90),
                Location = new Point(350, 15),
                AutoSize = true
            };

            cmbProduct = new ComboBox
            {
                Location = new Point(350, 38),
                Size = new Size(300, 28),
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList // Changed from DropDown as we have a dedicated search now
            };
            cmbProduct.SelectedIndexChanged += CmbProduct_Changed;

            lblCurrentStock = new Label
            {
                Text = "Stock: --",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 90),
                Location = new Point(660, 40),
                AutoSize = true
            };

            var lblType = new Label
            {
                Text = "Type *",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 90),
                Location = new Point(20, 75),
                AutoSize = true
            };

            cmbType = new ComboBox
            {
                Location = new Point(20, 98),
                Size = new Size(120, 28),
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbType.Items.AddRange(new object[] { "IN", "OUT" });
            cmbType.SelectedIndex = 0;

            var lblQty = new Label
            {
                Text = "Quantity *",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 90),
                Location = new Point(150, 75),
                AutoSize = true
            };

            nudQuantity = new NumericUpDown
            {
                Location = new Point(150, 98),
                Size = new Size(100, 28),
                Font = new Font("Segoe UI", 10),
                Minimum = 1,
                Maximum = 999999,
                Value = 1
            };

            var lblNotes = new Label
            {
                Text = "Notes / Reason",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 90),
                Location = new Point(260, 75),
                AutoSize = true
            };

            txtNotes = new TextBox
            {
                Location = new Point(260, 98),
                Size = new Size(320, 28),
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Enter reason/remarks..."
            };

            var btnSubmit = new Button
            {
                Text = "📥 Record Transaction",
                Location = new Point(590, 93),
                Size = new Size(180, 38),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(27, 94, 32),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSubmit.FlatAppearance.BorderSize = 0;
            btnSubmit.Click += BtnSubmit_Click;

            panelForm.Controls.AddRange(new Control[] {
                lblSearchProd, txtSearchProduct,
                lblCategory, cmbCategory, lblProduct, cmbProduct, lblCurrentStock,
                lblType, cmbType, lblQty, nudQuantity,
                lblNotes, txtNotes, btnSubmit
            });

            // Transaction history panel
            var panelHistoryHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 45,
                Padding = new Padding(0, 5, 0, 5)
            };

            var lblHistory = new Label
            {
                Text = "📋 Transaction History",
                Font = new Font("Segoe UI", 12, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32),
                Dock = DockStyle.Left,
                AutoSize = true,
                Padding = new Padding(0, 5, 15, 0)
            };

            var panelFilters = new Panel
            {
                Dock = DockStyle.Right,
                Width = 450,
                Padding = new Padding(0, 5, 10, 0)
            };

            txtSearchHistory = new TextBox
            {
                Width = 230,
                Font = new Font("Segoe UI", 10),
                PlaceholderText = "🔍 Search history records...",
                BorderStyle = BorderStyle.FixedSingle,
                Location = new Point(40, 2)
            };
            txtSearchHistory.TextChanged += TxtSearchHistory_TextChanged;

            dtpHistoryDate = new DateTimePicker
            {
                Format = DateTimePickerFormat.Short,
                Font = new Font("Segoe UI", 10),
                Width = 140,
                Location = new Point(290, 1),
                ShowCheckBox = true,
                Checked = false
            };
            dtpHistoryDate.ValueChanged += DtpHistoryDate_ValueChanged;

            panelFilters.Controls.Add(txtSearchHistory);
            panelFilters.Controls.Add(dtpHistoryDate);

            panelHistoryHeader.Controls.Add(panelFilters);
            panelHistoryHeader.Controls.Add(lblHistory);

            // DataGridView
            var panelGrid = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1)
            };

            dgvTransactions = new DataGridView
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
            dgvTransactions.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 235, 250);
            dgvTransactions.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvTransactions.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(27, 94, 32);
            dgvTransactions.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvTransactions.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvTransactions.EnableHeadersVisualStyles = false;
            dgvTransactions.ColumnHeadersHeight = 35;
            dgvTransactions.RowTemplate.Height = 30;

            // Color-code IN/OUT
            dgvTransactions.CellFormatting += (s, e) =>
            {
                if (dgvTransactions.Columns[e.ColumnIndex].Name == "Type")
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

            panelGrid.Controls.Add(dgvTransactions);

            // Export buttons panel
            var panelExport = new Panel
            {
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(0, 5, 0, 0)
            };

            var btnExportExcel = new Button
            {
                Text = "📊 Export Excel",
                Location = new Point(200, 5),
                Size = new Size(130, 30),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(34, 139, 34),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnExportExcel.FlatAppearance.BorderSize = 0;
            btnExportExcel.Click += (s, e) =>
            {
                var path = ExportHelper.ShowSaveDialog("Excel Files|*.xlsx", "Transactions_Report.xlsx");
                if (path != null)
                {
                    var trans = _transRepo.GetRecent(500).Where(t => !t.Notes.StartsWith("Sold (Order")).ToList();
                    
                    DateTime fromDate = trans.Count > 0 ? DateTime.Parse(trans.Last().TransactionDate) : DateTime.Now;
                    DateTime toDate = trans.Count > 0 ? DateTime.Parse(trans.First().TransactionDate) : DateTime.Now;

                    ExportHelper.ExportStockHistoryExcel(trans, fromDate, toDate, path);
                    MessageBox.Show("Export complete!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            var btnExportPdf = new Button
            {
                Text = "📄 Export PDF",
                Location = new Point(340, 5),
                Size = new Size(120, 30),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(200, 50, 50),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnExportPdf.FlatAppearance.BorderSize = 0;
            btnExportPdf.Click += (s, e) =>
            {
                var path = ExportHelper.ShowSaveDialog("PDF Files|*.pdf", "Transactions_Report.pdf");
                if (path != null)
                {
                    var trans = _transRepo.GetRecent(500).Where(t => !t.Notes.StartsWith("Sold (Order")).ToList();
                    ExportHelper.ExportTransactionsToPdf(trans, path);
                    MessageBox.Show("Export complete!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            panelExport.Controls.Add(btnExportExcel);
            panelExport.Controls.Add(btnExportPdf);

            // Only show transaction history for Admin users
            if (SessionManager.IsAdmin)
            {
                this.Controls.Add(panelGrid);
                this.Controls.Add(panelExport);
                this.Controls.Add(panelHistoryHeader);
            }
            this.Controls.Add(panelForm);
            this.Controls.Add(lblTitle);
        }

        private void LoadCategories()
        {
            cmbCategory.Items.Clear();
            cmbCategory.Items.Add(new Category { Id = 0, Name = "All Categories" });
            var categories = _categoryRepo.GetAll();
            foreach (var cat in categories) cmbCategory.Items.Add(cat);
            cmbCategory.DisplayMember = "Name";
            cmbCategory.SelectedIndex = 0;
        }

        private void CmbCategory_Changed(object? sender, EventArgs e)
        {
            txtSearchProduct.Clear(); // Clear search when category changes
            LoadProducts();
        }

        private void TxtSearchProduct_TextChanged(object? sender, EventArgs e)
        {
            LoadProducts(txtSearchProduct.Text);
        }

        private void LoadProducts(string searchTerm = "")
        {
            cmbProduct.Items.Clear();
            cmbProduct.Items.Add("-- Select Product --");
            var products = _productRepo.GetAll();

            // Ignore category filter if there is a search term to find across all categories
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                if (cmbCategory.SelectedIndex > 0 && cmbCategory.SelectedItem is Category cat)
                {
                    products = products.Where(p => p.CategoryId == cat.Id).ToList();
                }
            }
            else
            {
                products = products.Where(p => p.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            foreach (var p in products)
            {
                cmbProduct.Items.Add(p);
            }
            cmbProduct.DisplayMember = "Name";
            
            if (cmbProduct.Items.Count > 1 && !string.IsNullOrWhiteSpace(searchTerm))
            {
                cmbProduct.SelectedIndex = 1; // Auto-select the first found item
            }
            else if (cmbProduct.Items.Count > 0)
            {
                cmbProduct.SelectedIndex = 0;
            }
        }

        private void CmbProduct_Changed(object? sender, EventArgs e)
        {
            if (cmbProduct.SelectedIndex > 0 && cmbProduct.SelectedItem is Product product)
            {
                lblCurrentStock.Text = $"Stock: {product.Quantity} {product.Unit}";
                if (product.Quantity <= product.MinStockLevel)
                {
                    lblCurrentStock.ForeColor = Color.FromArgb(231, 76, 60); // Red if low
                }
                else
                {
                    lblCurrentStock.ForeColor = Color.FromArgb(39, 174, 96); // Green if OK
                }
            }
            else
            {
                lblCurrentStock.Text = "Stock: --";
                lblCurrentStock.ForeColor = Color.FromArgb(60, 70, 90);
            }
        }

        private void TxtSearchHistory_TextChanged(object? sender, EventArgs e)
        {
            LoadTransactions();
        }

        private void DtpHistoryDate_ValueChanged(object? sender, EventArgs e)
        {
            LoadTransactions();
        }

        private void LoadTransactions()
        {
            var searchTerm = txtSearchHistory.Text.Trim().ToLower();
            var allTransactions = _transRepo.GetRecent(500); // Increased limit as we may filter
            
            var filtered = allTransactions.Where(t => !t.Notes.StartsWith("Sold (Order"));
            
            // Filter by Date
            if (dtpHistoryDate.Checked)
            {
                var selectedDate = dtpHistoryDate.Value.Date;
                filtered = filtered.Where(t => 
                {
                    if (DateTime.TryParse(t.TransactionDate, out DateTime logDate))
                    {
                        return logDate.Date == selectedDate;
                    }
                    return false;
                });
            }

            // Filter by Keyword
            if (!string.IsNullOrEmpty(searchTerm))
            {
                filtered = filtered.Where(t => 
                    t.ProductName.ToLower().Contains(searchTerm) || 
                    (t.Notes != null && t.Notes.ToLower().Contains(searchTerm)) ||
                    t.Type.ToLower().Contains(searchTerm));
            }

            dgvTransactions.DataSource = filtered.Take(100).Select(t => new
            {
                Date = t.TransactionDate,
                Product = t.ProductName,
                t.Type,
                t.Quantity,
                t.Notes,
                User = t.UserName
            }).ToList();
        }

        private void BtnSubmit_Click(object? sender, EventArgs e)
        {
            if (cmbProduct.SelectedIndex <= 0 || cmbProduct.SelectedItem is not Product product)
            {
                MessageBox.Show("Please select a product.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmbType.SelectedItem?.ToString() == "OUT" && nudQuantity.Value > product.Quantity)
            {
                MessageBox.Show($"Insufficient stock. Available: {product.Quantity} {product.Unit}",
                    "Insufficient Stock", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var transaction = new StockTransaction
            {
                ProductId = product.Id,
                Type = cmbType.SelectedItem?.ToString() ?? "IN",
                Quantity = (int)nudQuantity.Value,
                Notes = txtNotes.Text.Trim(),
                UserId = SessionManager.CurrentUser?.Id ?? 1
            };

            if (_transRepo.Add(transaction))
            {
                _auditRepo.Log(SessionManager.CurrentUser?.Id ?? 1,
                    $"Stock {transaction.Type}",
                    $"{transaction.Type} {transaction.Quantity} of {product.Name}");

                // Offer receipt print
                var printResult = MessageBox.Show("Transaction recorded! Print receipt?",
                    "Success", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
                if (printResult == DialogResult.Yes)
                {
                    transaction.ProductName = product.Name;
                    transaction.UserName = SessionManager.CurrentUser?.FullName ?? "Unknown";
                    transaction.TransactionDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                    var path = ExportHelper.ShowSaveDialog("PDF Files|*.pdf",
                        $"Receipt_{product.Name}_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");
                    if (path != null)
                    {
                        ExportHelper.ExportStockReceiptPdf(transaction, path);
                        MessageBox.Show("Receipt saved!", "Done", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }

                // Refresh
                txtNotes.Clear();
                nudQuantity.Value = 1;
                LoadProducts();
                LoadTransactions();
            }
            else
            {
                MessageBox.Show("Failed to record transaction.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
