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

            var lblCategory = new Label
            {
                Text = "Category *",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 90),
                Location = new Point(20, 15),
                AutoSize = true
            };

            cmbCategory = new ComboBox
            {
                Location = new Point(20, 38),
                Size = new Size(180, 28),
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbCategory.SelectedIndexChanged += CmbCategory_Changed;

            var lblProduct = new Label
            {
                Text = "Product *",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 90),
                Location = new Point(220, 15),
                AutoSize = true
            };

            cmbProduct = new ComboBox
            {
                Location = new Point(220, 38),
                Size = new Size(350, 28),
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbProduct.SelectedIndexChanged += CmbProduct_Changed;

            lblCurrentStock = new Label
            {
                Text = "Current Stock: --",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32),
                Location = new Point(590, 40),
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
                Location = new Point(160, 75),
                AutoSize = true
            };

            nudQuantity = new NumericUpDown
            {
                Location = new Point(160, 98),
                Size = new Size(120, 28),
                Font = new Font("Segoe UI", 10),
                Minimum = 1,
                Maximum = 999999,
                Value = 1
            };

            var lblNotes = new Label
            {
                Text = "Notes",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 90),
                Location = new Point(300, 75),
                AutoSize = true
            };

            txtNotes = new TextBox
            {
                Location = new Point(300, 98),
                Size = new Size(300, 28),
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle,
                PlaceholderText = "Enter reason/remarks..."
            };

            var btnSubmit = new Button
            {
                Text = "📥 Record Transaction",
                Location = new Point(620, 93),
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
                lblCategory, cmbCategory, lblProduct, cmbProduct, lblCurrentStock,
                lblType, cmbType, lblQty, nudQuantity,
                lblNotes, txtNotes, btnSubmit
            });

            // Transaction history label
            var lblHistory = new Label
            {
                Text = "📋 Transaction History",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32),
                Dock = DockStyle.Top,
                Height = 40,
                Padding = new Padding(0, 10, 0, 0)
            };

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
                    var trans = _transRepo.GetRecent(500);
                    var data = trans.Select(t => new { t.TransactionDate, Product = t.ProductName, t.Type, t.Quantity, t.Notes, User = t.UserName }).ToList();
                    ExportHelper.ExportToExcel(data, "Transactions", path);
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
                    var trans = _transRepo.GetRecent(500);
                    ExportHelper.ExportTransactionsToPdf(trans, path);
                    MessageBox.Show("Export complete!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };

            panelExport.Controls.Add(btnExportExcel);
            panelExport.Controls.Add(btnExportPdf);

            this.Controls.Add(panelGrid);
            this.Controls.Add(panelExport);
            this.Controls.Add(lblHistory);
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
            LoadProducts();
        }

        private void LoadProducts()
        {
            cmbProduct.Items.Clear();
            cmbProduct.Items.Add("-- Select Product --");
            var products = _productRepo.GetAll();

            if (cmbCategory.SelectedIndex > 0 && cmbCategory.SelectedItem is Category cat)
            {
                products = products.Where(p => p.CategoryId == cat.Id).ToList();
            }

            foreach (var p in products)
            {
                cmbProduct.Items.Add(p);
            }
            cmbProduct.DisplayMember = "Name";
            if (cmbProduct.Items.Count > 0) cmbProduct.SelectedIndex = 0;
        }

        private void CmbProduct_Changed(object? sender, EventArgs e)
        {
            if (cmbProduct.SelectedIndex > 0 && cmbProduct.SelectedItem is Product product)
            {
                lblCurrentStock.Text = $"Current Stock: {product.Quantity} {product.Unit}";
            }
            else
            {
                lblCurrentStock.Text = "Current Stock: --";
            }
        }

        private void LoadTransactions()
        {
            var transactions = _transRepo.GetRecent(50);
            dgvTransactions.DataSource = transactions.Select(t => new
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
