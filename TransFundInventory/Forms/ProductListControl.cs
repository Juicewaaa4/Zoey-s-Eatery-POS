using TransFundInventory.Data;
using TransFundInventory.Helpers;
using TransFundInventory.Models;

namespace TransFundInventory.Forms
{
    public class ProductListControl : UserControl
    {
        private DataGridView dgvProducts = null!;
        private TextBox txtSearch = null!;
        private ComboBox cmbCategory = null!;
        private FlowLayoutPanel panelCategoryTiles = null!;
        private Panel panelToolbar = null!;
        private Panel panelGrid = null!;
        private Button btnBackToTiles = null!;
        private Label lblTitle = null!;

        private readonly ProductRepository _productRepo = new();
        private readonly CategoryRepository _categoryRepo = new();
        private readonly AuditLogRepository _auditRepo = new();

        public ProductListControl()
        {
            InitializeComponent();
            LoadCategories();
            LoadProducts();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(240, 242, 245);

            bool isEatery = SessionManager.CurrentSection == "Eatery";

            lblTitle = new Label
            {
                Text = isEatery ? "Menu Categories" : "Products",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32),
                Dock = DockStyle.Top,
                Height = 50
            };

            // Toolbar panel
            panelToolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                Padding = new Padding(0, 5, 0, 5)
            };

            txtSearch = new TextBox
            {
                Location = new Point(0, 10),
                Size = new Size(250, 30),
                Font = new Font("Segoe UI", 10),
                PlaceholderText = isEatery ? "🔍 Search by name..." : "🔍 Search by name, ID...",
                BorderStyle = BorderStyle.FixedSingle
            };
            txtSearch.TextChanged += (s, e) => LoadProducts();

            cmbCategory = new ComboBox
            {
                Location = new Point(260, 10),
                Size = new Size(200, 30),
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbCategory.SelectedIndexChanged += (s, e) => LoadProducts();

            btnBackToTiles = CreateButton("⏪ Back", Color.FromArgb(100, 110, 140), 10);
            btnBackToTiles.Size = new Size(80, 30);
            btnBackToTiles.Click += (s, e) => ShowCategoryTiles();
            btnBackToTiles.Visible = isEatery;

            if (isEatery)
            {
                txtSearch.Location = new Point(100, 10);
                cmbCategory.Visible = false; // Hidden in Eatery as tiles control this
            }

            int btnStartX = isEatery ? 370 : 480;

            var btnAdd = CreateButton(isEatery ? "➕ Add Menu Item" : "➕ Add Product", Color.FromArgb(39, 174, 96), btnStartX);
            btnAdd.Click += BtnAdd_Click;

            var btnEdit = CreateButton("✏️ Edit", Color.FromArgb(52, 120, 246), btnStartX + 140);
            btnEdit.Click += BtnEdit_Click;

            var btnDelete = CreateButton("🗑️ Delete", Color.FromArgb(231, 76, 60), 720);
            btnDelete.Click += BtnDelete_Click;

            var btnExportExcel = CreateButton("📊 Excel", Color.FromArgb(34, 139, 34), 860);
            btnExportExcel.Size = new Size(90, 32);
            btnExportExcel.Click += BtnExportExcel_Click;

            var btnExportPdf = CreateButton("📄 PDF", Color.FromArgb(200, 50, 50), 958);
            btnExportPdf.Size = new Size(80, 32);
            btnExportPdf.Click += BtnExportPdf_Click;

            panelToolbar.Controls.AddRange(new Control[] { btnBackToTiles, txtSearch, cmbCategory, btnAdd, btnEdit, btnDelete, btnExportExcel, btnExportPdf });

            // DataGridView
            panelGrid = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1)
            };

            dgvProducts = new DataGridView
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
                Font = new Font("Segoe UI", 9),
                MultiSelect = false
            };
            dgvProducts.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 235, 250);
            dgvProducts.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvProducts.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(27, 94, 32);
            dgvProducts.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvProducts.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvProducts.EnableHeadersVisualStyles = false;
            dgvProducts.ColumnHeadersHeight = 35;
            dgvProducts.RowTemplate.Height = 30;
            dgvProducts.CellDoubleClick += (s, e) => BtnEdit_Click(s, e);

            // Row formatting for low stock
            dgvProducts.CellFormatting += DgvProducts_CellFormatting;

            panelGrid.Controls.Add(dgvProducts);

            this.Controls.Add(panelGrid);
            
            // Category Tiles Panel (For Eatery)
            panelCategoryTiles = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(20),
                AutoScroll = true,
                Visible = false
            };
            this.Controls.Add(panelCategoryTiles);

            this.Controls.Add(panelToolbar);
            this.Controls.Add(lblTitle);

            if (isEatery)
            {
                ShowCategoryTiles();
            }
        }

        private Button CreateButton(string text, Color bgColor, int x)
        {
            var btn = new Button
            {
                Text = text,
                Location = new Point(x, 8),
                Size = new Size(130, 32),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = bgColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            return btn;
        }

        private void ShowCategoryTiles()
        {
            lblTitle.Text = "🍕 MENU CATEGORIES";
            panelToolbar.Visible = false;
            panelGrid.Visible = false;
            panelCategoryTiles.Visible = true;
            panelCategoryTiles.Controls.Clear();

            var categories = _categoryRepo.GetAll();
            foreach (var cat in categories)
            {
                var btnCat = new Button
                {
                    Text = cat.Name.ToUpper(),
                    Size = new Size(200, 150),
                    Font = new Font("Segoe UI", 16, FontStyle.Bold),
                    BackColor = Color.White,
                    ForeColor = Color.FromArgb(27, 94, 32),
                    FlatStyle = FlatStyle.Flat,
                    Cursor = Cursors.Hand,
                    Margin = new Padding(15)
                };
                btnCat.FlatAppearance.BorderColor = Color.FromArgb(46, 125, 50);
                btnCat.FlatAppearance.BorderSize = 2;
                
                // Add hover effect
                btnCat.MouseEnter += (s, e) => { btnCat.BackColor = Color.FromArgb(27, 94, 32); btnCat.ForeColor = Color.White; };
                btnCat.MouseLeave += (s, e) => { btnCat.BackColor = Color.White; btnCat.ForeColor = Color.FromArgb(27, 94, 32); };

                btnCat.Click += (s, e) =>
                {
                    // Select this category in the hidden combo box
                    for (int i = 1; i < cmbCategory.Items.Count; i++)
                    {
                        if (((Category)cmbCategory.Items[i]).Id == cat.Id)
                        {
                            cmbCategory.SelectedIndex = i;
                            break;
                        }
                    }
                    ShowItemGrid(cat.Name);
                };
                panelCategoryTiles.Controls.Add(btnCat);
            }
            
            // "All Items" tile
            var btnAll = new Button
            {
                Text = "🔍 ALL ITEMS",
                Size = new Size(200, 150),
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(100, 110, 120),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                Margin = new Padding(15)
            };
            btnAll.FlatAppearance.BorderColor = Color.FromArgb(100, 110, 120);
            btnAll.FlatAppearance.BorderSize = 2;
            btnAll.Click += (s, e) =>
            {
                cmbCategory.SelectedIndex = 0;
                ShowItemGrid("All Menu Items");
            };
            panelCategoryTiles.Controls.Add(btnAll);
        }

        private void ShowItemGrid(string categoryName)
        {
            lblTitle.Text = $"📋 {categoryName.ToUpper()}";
            panelCategoryTiles.Visible = false;
            panelToolbar.Visible = true;
            panelGrid.Visible = true;
            LoadProducts();
        }

        private void LoadCategories()
        {
            cmbCategory.Items.Clear();
            cmbCategory.Items.Add("All Categories");
            var categories = _categoryRepo.GetAll();
            foreach (var cat in categories)
            {
                cmbCategory.Items.Add(cat);
            }
            cmbCategory.DisplayMember = "Name";
            cmbCategory.SelectedIndex = 0;
        }

        private void LoadProducts()
        {
            int? categoryId = null;
            if (cmbCategory.SelectedIndex > 0 && cmbCategory.SelectedItem is Category cat)
            {
                categoryId = cat.Id;
            }

            var products = _productRepo.Search(txtSearch.Text, categoryId);
            dgvProducts.DataSource = products.Select(p => new
            {
                p.Id,
                ID = p.SKU,
                p.Name,
                Category = p.CategoryName,
                Price = $"₱{p.Price:N2}",
                Cost = $"₱{p.CostPrice:N2}",
                Qty = p.Quantity,
                MinStock = p.MinStockLevel,
                p.Unit,
                Status = p.Quantity <= p.MinStockLevel ? "⚠️ LOW" : "✅ OK"
            }).ToList();

            if (dgvProducts.Columns.Count > 0)
            {
                dgvProducts.Columns["Id"].Visible = false;
                if (SessionManager.CurrentSection == "Eatery")
                {
                    if (dgvProducts.Columns.Contains("ID")) dgvProducts.Columns["ID"].Visible = false;
                    if (dgvProducts.Columns.Contains("Unit")) dgvProducts.Columns["Unit"].Visible = false;
                    if (dgvProducts.Columns.Contains("Qty")) dgvProducts.Columns["Qty"].Visible = false;
                    if (dgvProducts.Columns.Contains("MinStock")) dgvProducts.Columns["MinStock"].Visible = false;
                    if (dgvProducts.Columns.Contains("Status")) dgvProducts.Columns["Status"].Visible = false;
                }
            }
        }

        private void DgvProducts_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvProducts.Columns[e.ColumnIndex].Name == "Status" && e.Value?.ToString() == "⚠️ LOW")
            {
                e.CellStyle!.ForeColor = Color.FromArgb(231, 76, 60);
                e.CellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            }
        }

        private int GetSelectedProductId()
        {
            if (dgvProducts.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a product.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return -1;
            }
            return (int)dgvProducts.SelectedRows[0].Cells["Id"].Value;
        }

        private void BtnAdd_Click(object? sender, EventArgs e)
        {
            Category? selectedCat = null;
            if (SessionManager.CurrentSection == "Eatery" && cmbCategory.SelectedIndex > 0 && cmbCategory.SelectedItem is Category cat)
            {
                selectedCat = cat;
            }

            var form = new ProductFormDialog(null, selectedCat);
            if (form.ShowDialog() == DialogResult.OK)
            {
                LoadProducts();
            }
        }

        private void BtnEdit_Click(object? sender, EventArgs e)
        {
            var id = GetSelectedProductId();
            if (id < 0) return;

            var product = _productRepo.GetById(id);
            var form = new ProductFormDialog(product);
            if (form.ShowDialog() == DialogResult.OK)
            {
                LoadProducts();
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            var id = GetSelectedProductId();
            if (id < 0) return;

            var product = _productRepo.GetById(id);
            var result = MessageBox.Show($"Are you sure you want to delete '{product?.Name}'?",
                "Confirm Delete", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                if (_productRepo.Delete(id))
                {
                    LoadProducts();
                }
                else
                {
                    MessageBox.Show("Hindi mabura ang item. Malamang ay mayroong nabenta o naka-record na stock para dito dati pa. I-edit mo nalang ang item o kaya gawing 0 ang quantity.", "Deletion Blocked",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void BtnExportExcel_Click(object? sender, EventArgs e)
        {
            var path = ExportHelper.ShowSaveDialog("Excel Files|*.xlsx", "Products_Report.xlsx");
            if (path != null)
            {
                var products = _productRepo.GetAll();
                if (SessionManager.CurrentSection == "Eatery")
                {
                    var data = products.Select(p => new { p.Name, Category = p.CategoryName, Price = p.Price, Cost = p.CostPrice, Qty = p.Quantity, MinStock = p.MinStockLevel }).ToList();
                    ExportHelper.ExportToExcel(data, "Menu_Items", path);
                }
                else
                {
                    var data = products.Select(p => new { ID = p.SKU, p.Name, Category = p.CategoryName, Price = p.Price, Cost = p.CostPrice, Qty = p.Quantity, MinStock = p.MinStockLevel, p.Unit }).ToList();
                    ExportHelper.ExportToExcel(data, "Products", path);
                }
                
                _auditRepo.Log(SessionManager.CurrentUser?.Id ?? 1, "Export", "Exported products to Excel");
                MessageBox.Show("Export complete!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnExportPdf_Click(object? sender, EventArgs e)
        {
            var path = ExportHelper.ShowSaveDialog("PDF Files|*.pdf", "Products_Report.pdf");
            if (path != null)
            {
                var products = _productRepo.GetAll();
                ExportHelper.ExportProductsToPdf(products, path);
                _auditRepo.Log(SessionManager.CurrentUser?.Id ?? 1, "Export", "Exported products to PDF");
                MessageBox.Show("Export complete!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
