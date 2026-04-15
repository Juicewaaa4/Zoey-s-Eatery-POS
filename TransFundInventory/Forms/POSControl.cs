using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TransFundInventory.Data;
using TransFundInventory.Helpers;
using TransFundInventory.Models;

namespace TransFundInventory.Forms
{
    public class POSControl : UserControl
    {
        private TextBox txtSearch = null!;
        private DataGridView dgvProducts = null!;
        private DataGridView dgvCart = null!;
        private Label lblTotal = null!;
        private NumericUpDown nudTendered = null!;
        private Label lblChange = null!;
        private TextBox txtCustomer = null!;
        private CheckBox chkAutoPrint = null!;
        private CheckBox chkPrintDuplicate = null!;
        private FlowLayoutPanel pnlFastCash = null!;
        private Label lblTendered = null!;

        private readonly ProductRepository _productRepo = new();
        private readonly SalesRepository _salesRepo = new();
        private readonly CategoryRepository _categoryRepo = new();
        private BindingList<CartItem> _cart = new();
        
        private ComboBox cmbCategoryFilter = null!;
        private int? _selectedCategoryId = null;

        public POSControl()
        {
            InitializeComponent();
            LoadCategoryFilters();
            LoadProducts("");
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(240, 242, 245);
            this.Dock = DockStyle.Fill;

            var tableLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 1,
                Padding = new Padding(15)
            };
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F)); // Products side
            tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F)); // Cart side

            // ========== SHORTCUT KEY GUIDE BAR ==========
            var panelShortcuts = new Panel
            {
                Dock = DockStyle.Top,
                Height = 32,
                BackColor = Color.FromArgb(21, 71, 24),
                Padding = new Padding(10, 0, 10, 0)
            };
            var lblShortcuts = new Label
            {
                Text = "⌨  F1: Search  |  ↑↓: Select Product  |  Enter: Add to Cart  |  F8: Cash Tendered  |  F12: Checkout",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(200, 230, 200),
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelShortcuts.Controls.Add(lblShortcuts);

            // ==================== LEFT SIDE (PRODUCTS) ====================
            var panelLeft = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(10) };
            
            bool isEatery = SessionManager.CurrentSection == "Eatery";

            var lblTitleLeft = new Label
            {
                Text = isEatery ? "🍔 Available Menu Items" : "📦 Available Products",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32),
                Dock = DockStyle.Top,
                Height = 40
            };

            var panelSearch = new Panel { Dock = DockStyle.Top, Height = 50, Padding = new Padding(0, 10, 0, 10) };
            
            cmbCategoryFilter = new ComboBox
            {
                Dock = DockStyle.Right,
                Width = 200,
                Font = new Font("Segoe UI", 11),
                DropDownStyle = ComboBoxStyle.DropDownList,
                Cursor = Cursors.Hand
            };
            cmbCategoryFilter.SelectedIndexChanged += (s, e) =>
            {
                if (cmbCategoryFilter.SelectedItem is CategoryDropdownItem item)
                {
                    _selectedCategoryId = item.Id;
                    LoadProducts(txtSearch.Text);
                }
            };

            var spacer = new Panel { Dock = DockStyle.Right, Width = 10 }; // Space between search and filter

            txtSearch = new TextBox
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 11),
                PlaceholderText = isEatery ? "🔍 Search item name..." : "🔍 Search product by Name or SKU..."
            };
            txtSearch.TextChanged += (s, e) => { LoadProducts(txtSearch.Text); };
            txtSearch.KeyDown += (s, e) => 
            { 
                if (e.KeyCode == Keys.Enter) 
                {
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    if (dgvProducts.Rows.Count == 1)
                    {
                        AddToCart(0);
                        txtSearch.Clear();
                    }
                    else if (dgvProducts.Rows.Count > 1)
                    {
                        dgvProducts.Focus();
                        dgvProducts.CurrentCell = dgvProducts.Rows[0].Cells[0];
                    }
                } 
            };
            panelSearch.Controls.Add(cmbCategoryFilter);
            panelSearch.Controls.Add(spacer);
            panelSearch.Controls.Add(txtSearch);

            dgvProducts = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeColumns = false,
                AllowUserToResizeRows = false,
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 10),
                GridColor = Color.FromArgb(235, 240, 245),
                Cursor = Cursors.Hand
            };
            dgvProducts.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 230, 201);
            dgvProducts.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvProducts.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(27, 94, 32);
            dgvProducts.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvProducts.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgvProducts.EnableHeadersVisualStyles = false;
            
            // Allow both DoubleClick and explicit Button click
            dgvProducts.CellDoubleClick += DgvProducts_CellDoubleClick;
            dgvProducts.CellContentClick += DgvProducts_CellContentClick;
            dgvProducts.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && dgvProducts.CurrentRow != null)
                {
                    e.Handled = true;
                    AddToCart(dgvProducts.CurrentRow.Index);
                }
            };

            panelLeft.Controls.Add(dgvProducts);
            panelLeft.Controls.Add(panelSearch);
            panelLeft.Controls.Add(lblTitleLeft);

            // ==================== RIGHT SIDE (CART & CHECKOUT) ====================
            var panelRight = new Panel { Dock = DockStyle.Fill, Margin = new Padding(15, 0, 0, 0), BackColor = Color.White, Padding = new Padding(10) };

            var lblTitleRight = new Label
            {
                Text = "🛒 Shopping Cart",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32),
                Dock = DockStyle.Top,
                Height = 40
            };

            dgvCart = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeColumns = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                Font = new Font("Segoe UI", 10),
                GridColor = Color.FromArgb(235, 240, 245)
            };
            dgvCart.DefaultCellStyle.SelectionBackColor = Color.FromArgb(250, 235, 235);
            dgvCart.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvCart.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(27, 94, 32);
            dgvCart.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvCart.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgvCart.EnableHeadersVisualStyles = false;

            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = isEatery ? "Item" : "Product", ReadOnly = true, FillWeight = 150 });
            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Price", HeaderText = "Price", ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Format = "P#,##0.00" } });
            
            var colMinus = new DataGridViewButtonColumn { Name = "btnMinus", HeaderText = "", Text = "➖", UseColumnTextForButtonValue = true, Width = 30, FlatStyle = FlatStyle.Flat };
            colMinus.DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            dgvCart.Columns.Add(colMinus);

            var colQty = new DataGridViewTextBoxColumn 
            { 
                Name = "colQty", 
                DataPropertyName = "Quantity", 
                HeaderText = "Qty ✎", 
                Width = 60, 
                ReadOnly = true,
                DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter, Font = new Font("Segoe UI", 10, FontStyle.Underline), ForeColor = Color.Blue } 
            };
            dgvCart.Columns.Add(colQty);
            
            var colPlus = new DataGridViewButtonColumn { Name = "btnPlus", HeaderText = "", Text = "➕", UseColumnTextForButtonValue = true, Width = 30, FlatStyle = FlatStyle.Flat };
            colPlus.DefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            dgvCart.Columns.Add(colPlus);

            dgvCart.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Subtotal", HeaderText = "Subtotal", ReadOnly = true, DefaultCellStyle = new DataGridViewCellStyle { Format = "P#,##0.00" } });
            
            var colBtn = new DataGridViewButtonColumn { Name = "btnRemove", HeaderText = "", Text = "❌", UseColumnTextForButtonValue = true, Width = 40, FlatStyle = FlatStyle.Flat };
            colBtn.DefaultCellStyle.BackColor = Color.FromArgb(231, 76, 60);
            colBtn.DefaultCellStyle.ForeColor = Color.White;
            dgvCart.Columns.Add(colBtn);

            dgvCart.DataSource = _cart;
            dgvCart.CellValueChanged += DgvCart_CellValueChanged;
            dgvCart.CellContentClick += DgvCart_CellContentClick;
            dgvCart.CellClick += DgvCart_CellClick;

            // Checkout Panel (Bottom of right side)
            // Checkout Panel (Bottom of right side)
            var panelCheckout = new Panel { Dock = DockStyle.Bottom, Height = 340, BackColor = Color.FromArgb(245, 247, 250), Padding = new Padding(20) };

            var lblCustomer = new Label { Text = "Customer (Opt):", Font = new Font("Segoe UI", 10), Location = new Point(20, 20), AutoSize = true };
            txtCustomer = new TextBox { Location = new Point(140, 16), Width = 190, Font = new Font("Segoe UI", 10) };

            var lblTotalText = new Label { Text = "TOTAL DUE:", Font = new Font("Segoe UI", 16, FontStyle.Bold), Location = new Point(20, 50), AutoSize = true };
            lblTotal = new Label { Text = "₱0.00", Font = new Font("Tahoma", 22, FontStyle.Bold), ForeColor = Color.FromArgb(39, 174, 96), Location = new Point(148, 45), AutoSize = true };

            lblTendered = new Label { Text = "Tendered [F8]:", Font = new Font("Segoe UI", 12), Location = new Point(20, 100), AutoSize = true };
            nudTendered = new NumericUpDown { Location = new Point(140, 98), Width = 190, Font = new Font("Segoe UI", 14), Maximum = 9999999, DecimalPlaces = 2 };
            nudTendered.ValueChanged += NudTendered_ValueChanged;

            pnlFastCash = new FlowLayoutPanel { Location = new Point(140, 135), Width = 190, Height = 40 };
            int[] fastAmounts = { 50, 100, 200, 500, 1000 };
            foreach(int amt in fastAmounts) {
                var btn = new Button { Text = amt.ToString(), Width = 35, Height = 25, FlatStyle = FlatStyle.Flat, ForeColor = Color.FromArgb(27,94,32), Cursor = Cursors.Hand, Font = new Font("Segoe UI", 7, FontStyle.Bold), Margin = new Padding(0,0,3,3) };
                btn.FlatAppearance.BorderColor = Color.FromArgb(200,230,201);
                btn.Click += (s, e) => { nudTendered.Value = amt; };
                pnlFastCash.Controls.Add(btn);
            }

            var lblChangeText = new Label { Text = "CHANGE:", Font = new Font("Segoe UI", 12, FontStyle.Bold), Location = new Point(20, 180), AutoSize = true };
            lblChange = new Label { Text = "₱0.00", Font = new Font("Tahoma", 13, FontStyle.Bold), ForeColor = Color.FromArgb(52, 120, 246), Location = new Point(105, 180), AutoSize = true };

            chkAutoPrint = new CheckBox { Text = "Auto-Print", Font = new Font("Segoe UI", 10, FontStyle.Bold), Location = new Point(20, 225), AutoSize = true, Checked = true, Cursor = Cursors.Hand, ForeColor = Color.FromArgb(27, 94, 32) };
            chkPrintDuplicate = new CheckBox 
            { 
                Text = "Print Cashier Copy (x2)", 
                Font = new Font("Segoe UI", 9, FontStyle.Bold), 
                Location = new Point(20, 255), 
                AutoSize = true, 
                Checked = SessionManager.CurrentSection == "Eatery", // Default ON for Eatery
                Cursor = Cursors.Hand, 
                ForeColor = Color.FromArgb(27, 94, 32),
                Visible = SessionManager.CurrentSection == "Eatery" // Optionally hide for Store if not needed, but keep visible
            };

            var btnCheckout = new Button { Text = "💳 Checkout (F12)", Location = new Point(140, 280), Size = new Size(190, 50), Font = new Font("Segoe UI", 12, FontStyle.Bold), BackColor = Color.FromArgb(27, 94, 32), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnCheckout.FlatAppearance.BorderSize = 0;
            btnCheckout.Click += BtnCheckout_Click;

            panelCheckout.Controls.AddRange(new Control[] { lblCustomer, txtCustomer, lblTotalText, lblTotal, lblTendered, nudTendered, pnlFastCash, lblChangeText, lblChange, chkAutoPrint, chkPrintDuplicate, btnCheckout });

            panelRight.Controls.Add(dgvCart);
            panelRight.Controls.Add(lblTitleRight);
            panelRight.Controls.Add(panelCheckout);

            tableLayout.Controls.Add(panelLeft, 0, 0);
            tableLayout.Controls.Add(panelRight, 1, 0);

            this.Controls.Add(tableLayout);
            this.Controls.Add(panelShortcuts);
        }

        private void LoadCategoryFilters()
        {
            cmbCategoryFilter.Items.Clear();
            
            cmbCategoryFilter.Items.Add(new CategoryDropdownItem { Id = null, Name = "🌟 All Categories" });

            var categories = _categoryRepo.GetAll();
            foreach (var cat in categories)
            {
                cmbCategoryFilter.Items.Add(new CategoryDropdownItem { Id = cat.Id, Name = $"📁 {cat.Name}" });
            }

            if (cmbCategoryFilter.Items.Count > 0)
            {
                cmbCategoryFilter.SelectedIndex = 0; // Triggers SelectedIndexChanged which loads products
            }
        }

        private class CategoryDropdownItem
        {
            public int? Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public override string ToString() => Name;
        }

        private void LoadProducts(string keyword)
        {
            bool isEatery = SessionManager.CurrentSection == "Eatery";
            var allProducts = string.IsNullOrWhiteSpace(keyword) 
                ? _productRepo.GetAll()
                : _productRepo.Search(keyword, null);

            if (_selectedCategoryId.HasValue)
            {
                allProducts = allProducts.Where(p => p.CategoryId == _selectedCategoryId.Value).ToList();
            }

            var products = isEatery ? allProducts : allProducts.Where(p => p.Quantity > 0).ToList();

            dgvProducts.DataSource = products.Select(p => new
            {
                p.Id,
                p.SKU,
                p.Name,
                Price = (double)p.Price,
                Stock = p.Quantity
            }).ToList();

            dgvProducts.Columns["Id"].Visible = false;
            if (dgvProducts.Columns.Contains("SKU")) dgvProducts.Columns["SKU"].Visible = false;
            
            if (SessionManager.CurrentSection == "Eatery")
            {
                if (dgvProducts.Columns.Contains("Stock")) dgvProducts.Columns["Stock"].Visible = false;
            }

            dgvProducts.Columns["Price"].DefaultCellStyle.Format = "P#,##0.00";

            // Ensure the Add to Cart button column exists
            if (!dgvProducts.Columns.Contains("AddButton"))
            {
                var addCol = new DataGridViewButtonColumn
                {
                    Name = "AddButton",
                    HeaderText = "",
                    Text = "➕ Add",
                    UseColumnTextForButtonValue = true,
                    Width = 80,
                    FlatStyle = FlatStyle.Flat
                };
                addCol.DefaultCellStyle.BackColor = Color.FromArgb(27, 94, 32);
                addCol.DefaultCellStyle.ForeColor = Color.White;
                dgvProducts.Columns.Add(addCol);
            }
        }

        private void DgvProducts_CellDoubleClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            AddToCart(e.RowIndex);
        }

        private void DgvProducts_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && dgvProducts.Columns[e.ColumnIndex].Name == "AddButton")
            {
                AddToCart(e.RowIndex);
            }
        }

        private void AddToCart(int rowIndex)
        {

            int productId = (int)dgvProducts.Rows[rowIndex].Cells["Id"].Value;
            var product = _productRepo.GetById(productId);
            bool isEatery = SessionManager.CurrentSection == "Eatery";

            if (product == null) return;
            
            // Check stock only if Store mode
            if (!isEatery && product.Quantity <= 0)
            {
                MessageBox.Show("Product is out of stock.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var existingItem = _cart.FirstOrDefault(c => c.ProductId == productId);
            if (existingItem != null)
            {
                if (!isEatery && existingItem.Quantity + 1 > product.Quantity)
                {
                    MessageBox.Show($"Only {product.Quantity} items in stock.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                existingItem.Quantity++;
                _cart.ResetBindings(); // refresh grid
            }
            else
            {
                _cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Price = (double)product.Price,
                    CostPrice = (double)product.CostPrice,
                    Quantity = 1,
                    MaxQuantity = product.Quantity
                });
            }

            UpdateTotals();
        }

        private void DgvCart_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var colName = dgvCart.Columns[e.ColumnIndex].DataPropertyName;

            if (colName == "Quantity")
            {
                bool isEatery = SessionManager.CurrentSection == "Eatery";
                var item = _cart[e.RowIndex];
                if (!isEatery && item.Quantity > item.MaxQuantity)
                {
                    MessageBox.Show($"Only {item.MaxQuantity} items in stock.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    item.Quantity = item.MaxQuantity;
                }
                if (item.Quantity < 1) item.Quantity = 1;
                
                _cart.ResetBindings();
                UpdateTotals();
            }
        }

        private void DgvCart_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                var colName = dgvCart.Columns[e.ColumnIndex].Name;
                
                if (colName == "btnRemove")
                {
                    _cart.RemoveAt(e.RowIndex);
                    UpdateTotals();
                }
                else if (colName == "btnMinus")
                {
                    var item = _cart[e.RowIndex];
                    if (item.Quantity > 1)
                    {
                        item.Quantity--;
                        _cart.ResetBindings();
                        UpdateTotals();
                    }
                }
                else if (colName == "btnPlus")
                {
                    bool isEatery = SessionManager.CurrentSection == "Eatery";
                    var item = _cart[e.RowIndex];
                    if (isEatery || item.Quantity < item.MaxQuantity)
                    {
                        item.Quantity++;
                        _cart.ResetBindings();
                        UpdateTotals();
                    }
                    else
                    {
                        MessageBox.Show($"Only {item.MaxQuantity} items in stock.", "Notice", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }

        private void DgvCart_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                var colName = dgvCart.Columns[e.ColumnIndex].Name;
                if (colName == "colQty")
                {
                    var item = _cart[e.RowIndex];
                    bool isEatery = SessionManager.CurrentSection == "Eatery";
                    int max = isEatery ? 999999 : item.MaxQuantity;
                    
                    using var prompt = new Form()
                    {
                        Width = 280,
                        Height = 160,
                        FormBorderStyle = FormBorderStyle.FixedDialog,
                        Text = "Edit Quantity",
                        StartPosition = FormStartPosition.CenterParent,
                        MaximizeBox = false,
                        MinimizeBox = false,
                        BackColor = Color.White
                    };
                    var lbl = new Label() { Left = 20, Top = 15, Text = $"Enter quantity for {item.Name}:", AutoSize = true, Font = new Font("Segoe UI", 9) };
                    var num = new NumericUpDown() { Left = 20, Top = 40, Width = 220, Minimum = 1, Maximum = max, Value = item.Quantity, Font = new Font("Segoe UI", 12) };
                    var btnOk = new Button() { Text = "OK", Left = 165, Top = 80, Width = 75, Height = 30, DialogResult = DialogResult.OK, BackColor = Color.FromArgb(27, 94, 32), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Font = new Font("Segoe UI", 9, FontStyle.Bold) };
                    btnOk.FlatAppearance.BorderSize = 0;
                    
                    prompt.Controls.Add(lbl);
                    prompt.Controls.Add(num);
                    prompt.Controls.Add(btnOk);
                    prompt.AcceptButton = btnOk;
                    
                    // Auto select the number for quick typing
                    prompt.Shown += (s, ev) => { num.Focus(); num.Select(0, num.Value.ToString().Length); };

                    if (prompt.ShowDialog() == DialogResult.OK)
                    {
                        item.Quantity = (int)num.Value;
                        _cart.ResetBindings();
                        UpdateTotals();
                    }
                }
            }
        }


        private void NudTendered_ValueChanged(object? sender, EventArgs e)
        {
            UpdateTotals();
        }

        private void UpdateTotals()
        {
            double total = _cart.Sum(c => c.Subtotal);
            lblTotal.Text = $"₱{total:N2}";

            double tendered = (double)nudTendered.Value;
            double change = tendered - total;
            
            lblChange.Text = change < 0 ? "₱0.00" : $"₱{change:N2}";
            lblChange.ForeColor = change < 0 ? Color.FromArgb(231, 76, 60) : Color.FromArgb(27, 94, 32);
        }

        private void BtnCheckout_Click(object? sender, EventArgs e)
        {
            if (_cart.Count == 0)
            {
                MessageBox.Show("Cart is empty.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            double total = _cart.Sum(c => c.Subtotal);
            double tendered = (double)nudTendered.Value;

            if (tendered < total)
            {
                MessageBox.Show($"Tendered amount (P{tendered:N2}) is less than the total due (P{total:N2}).", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var sale = new SalesTransaction
            {
                TransactionDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                TotalAmount = total,
                CashTendered = tendered,
                ChangeAmount = tendered - total,
                CustomerName = string.IsNullOrWhiteSpace(txtCustomer.Text) ? null : txtCustomer.Text,
                UserId = SessionManager.CurrentUser!.Id,
                PaymentMethod = "Cash",
                ReferenceNumber = null
            };

            var saleItems = _cart.Select(c => new SalesItem
            {
                ProductId = c.ProductId,
                ProductName = c.Name, // ADDED: include name for logging
                Quantity = c.Quantity,
                PriceAtSale = c.Price,
                CostAtSale = c.CostPrice,
                Subtotal = c.Subtotal
            }).ToList();

            try
            {
                _salesRepo.ProcessSale(sale, saleItems, SessionManager.CurrentUser.Id);
                
                // Show simple success message without PDF prompt
                MessageBox.Show($"Checkout successful!\nOrder #: {sale.OrderNumber}\nChange: P{sale.ChangeAmount:N2}", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

                if (chkAutoPrint.Checked)
                {
                    ReceiptPrinter.Print(sale, saleItems);
                    if (chkPrintDuplicate.Checked)
                    {
                        ReceiptPrinter.Print(sale, saleItems, true); // true = isCashierCopy
                    }
                }

                // Reset
                _cart.Clear();
                nudTendered.Value = 0;
                txtCustomer.Text = "";
                LoadProducts(txtSearch.Text); // Refresh inventory quantities
                UpdateTotals();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Checkout failed: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }



        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            switch (keyData)
            {
                case Keys.F1:
                    txtSearch.Focus();
                    txtSearch.SelectAll();
                    return true;
                case Keys.F2:
                    // Focus product list for arrow key navigation
                    if (dgvProducts.Rows.Count > 0)
                    {
                        dgvProducts.Focus();
                        if (dgvProducts.CurrentRow == null)
                            dgvProducts.CurrentCell = dgvProducts.Rows[0].Cells[0];
                    }
                    return true;
                case Keys.F8:
                    nudTendered.Focus();
                    nudTendered.Select(0, nudTendered.Text.Length);
                    return true;
                case Keys.F12:
                    BtnCheckout_Click(this, EventArgs.Empty);
                    return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // Inner class for binding to DataGridView
        public class CartItem : INotifyPropertyChanged
        {
            public int ProductId { get; set; }
            public string Name { get; set; } = string.Empty;
            public double Price { get; set; }
            public double CostPrice { get; set; } // Non-displayed, for internal analytics
            public int MaxQuantity { get; set; } // Non-displayed

            private int quantity;
            public int Quantity
            {
                get => quantity;
                set
                {
                    if (quantity != value)
                    {
                        quantity = value;
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Quantity)));
                        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Subtotal)));
                    }
                }
            }

            public double Subtotal => Price * Quantity;

            public event PropertyChangedEventHandler? PropertyChanged;
        }
    }
}
