using TransFundInventory.Data;
using TransFundInventory.Helpers;
using TransFundInventory.Models;

namespace TransFundInventory.Forms
{
    public class ProductFormDialog : Form
    {
        private TextBox txtName = null!;
        private TextBox txtSKU = null!;
        private ComboBox cmbCategory = null!;
        private NumericUpDown nudPrice = null!;
        private NumericUpDown nudCostPrice = null!;
        private NumericUpDown nudQuantity = null!;
        private NumericUpDown nudMinStock = null!;
        private Label _lblProfitDisplay = null!;
        private readonly Product? _product;
        private readonly Category? _defaultCategory;
        private readonly ProductRepository _productRepo = new();
        private readonly CategoryRepository _categoryRepo = new();
        private readonly AuditLogRepository _auditRepo = new();

        public ProductFormDialog(Product? product, Category? defaultCategory = null)
        {
            _product = product;
            _defaultCategory = defaultCategory;
            InitializeComponent();
            LoadCategories();
            if (_product != null) PopulateFields();
        }

        private void InitializeComponent()
        {
            this.Text = _product == null ? "Add Product" : "Edit Product";
            this.Size = new Size(550, 710);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 250);

            bool isEatery = SessionManager.CurrentSection == "Eatery";

            var lblTitle = new Label
            {
                Text = _product == null 
                    ? (isEatery ? "🍔 Add Menu Item" : "📦 Add New Product") 
                    : (isEatery ? "✏️ Edit Menu Item" : "✏️ Edit Product"),
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32),
                Location = new Point(20, 15),
                AutoSize = true
            };

            int y = 60;
            int labelX = 20;
            int inputX = 150;
            int inputWidth = 350;

            AddLabel("Name *", labelX, y);
            txtName = AddTextBox(inputX, y, inputWidth);

            // Removed SKU manually inputted field

            y += 40;
            AddLabel("Category", labelX, y);
            cmbCategory = new ComboBox { Location = new Point(inputX, y), Size = new Size(inputWidth, 30), Font = new Font("Segoe UI", 11), DropDownStyle = ComboBoxStyle.DropDownList };
            this.Controls.Add(cmbCategory);

            y += 40;
            AddLabel("Price (₱)", labelX, y);
            nudPrice = AddNumericUpDown(inputX, y, 150, 0, 9999999, 2);
            nudPrice.ValueChanged += (s, e) => UpdateProfitDisplay();

            y += 40;
            var lblCostPrice = AddLabel("Cost Price (₱)", labelX, y);
            if (isEatery) lblCostPrice.Text = "Puhunan (₱)";
            nudCostPrice = new NumericUpDown { Location = new Point(inputX, y), Size = new Size(inputWidth - 110, 27), Font = new Font("Segoe UI", 11), Minimum = 0, Maximum = 999999, DecimalPlaces = 2 };
            nudCostPrice.ValueChanged += (s, e) => UpdateProfitDisplay();
            this.Controls.Add(nudCostPrice);

            var btnComputeCost = new Button
            {
                Text = "🧮 Compute",
                Location = new Point(inputX + inputWidth - 100, y - 2),
                Size = new Size(100, 31),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(240, 245, 250),
                ForeColor = Color.FromArgb(230, 126, 34),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnComputeCost.FlatAppearance.BorderColor = Color.FromArgb(230, 126, 34);
            btnComputeCost.Click += (s, e) =>
            {
                var dialog = new CostCalculatorDialog(nudPrice.Value);
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    nudCostPrice.Value = dialog.CalculatedCost;
                }
            };
            this.Controls.Add(btnComputeCost);

            // ── Live Profit Indicator ──
            y += 35;
            _lblProfitDisplay = new Label
            {
                Location = new Point(inputX, y),
                Size = new Size(inputWidth, 24),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(39, 174, 96),
                Text = isEatery ? "💡 I-click 'Compute' kung buong box/pack ang puhunan" : ""
            };
            this.Controls.Add(_lblProfitDisplay);

            if (isEatery)
            {
                // Eatery: Show stock field only for trackable items (drinks, etc.)
                y += 30;
                var lblQtyEatery = AddLabel("Stock (optional)", labelX, y);
                lblQtyEatery.ForeColor = Color.FromArgb(120, 130, 140);
                nudQuantity = AddNumericUpDown(inputX, y, 120, 0, 9999999, 0);
                nudQuantity.Value = 0;

                var lblQtyHint = new Label
                {
                    Text = "Para sa drinks, lagyan ng stock count",
                    Font = new Font("Segoe UI", 8),
                    ForeColor = Color.FromArgb(150, 160, 170),
                    Location = new Point(inputX + 130, y + 4),
                    AutoSize = true
                };
                this.Controls.Add(lblQtyHint);

                y += 40;
                var lblMinEatery = AddLabel("Min Stock Level", labelX, y);
                lblMinEatery.ForeColor = Color.FromArgb(120, 130, 140);
                nudMinStock = AddNumericUpDown(inputX, y, 120, 0, 9999999, 0);
                nudMinStock.Value = 0;

                var lblMinHint = new Label
                {
                    Text = "(Para sa drinks lang din)",
                    Font = new Font("Segoe UI", 8),
                    ForeColor = Color.FromArgb(150, 160, 170),
                    Location = new Point(inputX + 130, y + 4),
                    AutoSize = true
                };
                this.Controls.Add(lblMinHint);
            }
            else
            {
                // Store: Show both quantity and min stock
                y += 30;
                AddLabel("Quantity", labelX, y);
                nudQuantity = AddNumericUpDown(inputX, y, 120, 0, 9999999, 0);

                y += 40;
                AddLabel("Min Stock Level", labelX, y);
                nudMinStock = AddNumericUpDown(inputX, y, 120, 0, 9999999, 0);
                nudMinStock.Value = 10;
            }

            y += 60;
            var btnSave = new Button
            {
                Text = "💾 Save",
                Location = new Point(150, y),
                Size = new Size(140, 40),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            var btnCancel = new Button
            {
                Text = "Cancel",
                Location = new Point(300, y),
                Size = new Size(100, 40),
                Font = new Font("Segoe UI", 10),
                BackColor = Color.FromArgb(180, 185, 195),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.Add(lblTitle);
            this.Controls.Add(btnSave);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnSave;
        }

        private Label AddLabel(string text, int x, int y)
        {
            var lbl = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 90),
                Location = new Point(x, y + 4),
                AutoSize = true
            };
            this.Controls.Add(lbl);
            return lbl;
        }

        private TextBox AddTextBox(int x, int y, int width)
        {
            var txt = new TextBox
            {
                Location = new Point(x, y),
                Size = new Size(width, 28),
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(txt);
            return txt;
        }

        private NumericUpDown AddNumericUpDown(int x, int y, int width, decimal min, decimal max, int decimals)
        {
            var nud = new NumericUpDown
            {
                Location = new Point(x, y),
                Size = new Size(width, 28),
                Font = new Font("Segoe UI", 10),
                Minimum = min,
                Maximum = max,
                DecimalPlaces = decimals,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(nud);
            return nud;
        }

        private void LoadCategories()
        {
            cmbCategory.Items.Clear();
            cmbCategory.Items.Add("Select Category...");
            var categories = _categoryRepo.GetAll();
            foreach (var cat in categories)
            {
                cmbCategory.Items.Add(cat);
            }
            cmbCategory.DisplayMember = "Name";
            cmbCategory.SelectedIndex = 0;

            if (_product == null && _defaultCategory != null)
            {
                for (int i = 1; i < cmbCategory.Items.Count; i++)
                {
                    if (((Category)cmbCategory.Items[i]).Id == _defaultCategory.Id)
                    {
                        cmbCategory.SelectedIndex = i;
                        break;
                    }
                }
            }
        }

        private void PopulateFields()
        {
            if (_product == null) return;
            txtName.Text = _product.Name;
            nudPrice.Value = Math.Max(nudPrice.Minimum, Math.Min(nudPrice.Maximum, _product.Price));
            nudCostPrice.Value = Math.Max(nudCostPrice.Minimum, Math.Min(nudCostPrice.Maximum, _product.CostPrice));
            nudQuantity.Value = Math.Max(nudQuantity.Minimum, Math.Min(nudQuantity.Maximum, _product.Quantity));
            nudMinStock.Value = Math.Max(nudMinStock.Minimum, Math.Min(nudMinStock.Maximum, _product.MinStockLevel));

            // Select category
            for (int i = 1; i < cmbCategory.Items.Count; i++)
            {
                if (cmbCategory.Items[i] is Category cat && cat.Id == _product.CategoryId)
                {
                    cmbCategory.SelectedIndex = i;
                    break;
                }
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Name is required.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtName.Focus();
                return;
            }

            bool isEatery = SessionManager.CurrentSection == "Eatery";
            var product = _product ?? new Product();
            product.Name = txtName.Text.Trim();
            if (_product == null)
            {
                product.SKU = $"PRD-{DateTime.Now.ToString("yyMMddHHmmss")}";
            }
            product.Description = ""; // Force empty as it's no longer used
            product.CategoryId = cmbCategory.SelectedIndex > 0 && cmbCategory.SelectedItem is Category cat
                ? cat.Id : 0;
            product.Price = nudPrice.Value;
            product.CostPrice = nudCostPrice.Value;
            product.Quantity = (int)nudQuantity.Value;
            product.MinStockLevel = (int)nudMinStock.Value;
            product.Unit = isEatery ? "serving" : "item";
            product.ImagePath = null; // Image support removed

            bool success;
            string action;
            if (_product == null)
            {
                success = _productRepo.Add(product);
                action = isEatery ? "Add Menu Item" : "Add Product";
            }
            else
            {
                success = _productRepo.Update(product);
                action = isEatery ? "Edit Menu Item" : "Edit Product";
            }

            if (success)
            {
                _auditRepo.Log(SessionManager.CurrentUser?.Id ?? 1, action,
                    $"{action}: {product.Name}");
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            else
            {
                MessageBox.Show("Failed to save. The SKU might already exist.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        private void UpdateProfitDisplay()
        {
            decimal price = nudPrice.Value;
            decimal cost = nudCostPrice.Value;

            if (price == 0 && cost == 0)
            {
                bool isEatery = SessionManager.CurrentSection == "Eatery";
                _lblProfitDisplay.Text = isEatery ? "💡 I-click 'Compute' kung buong box/pack ang puhunan" : "";
                _lblProfitDisplay.ForeColor = Color.FromArgb(150, 160, 170);
                return;
            }

            decimal profit = price - cost;

            if (cost > 0 && price > 0)
            {
                // Use markup % (profit/cost) - more intuitive than margin
                decimal markup = (profit / cost) * 100;
                _lblProfitDisplay.Text = profit >= 0
                    ? $"✅ Kita: ₱{profit:N2} per item  ({markup:N1}% markup)"
                    : $"⚠️ Lugi: ₱{Math.Abs(profit):N2} — gamitin 'Compute' kung box/pack price ito";
            }
            else if (price > 0)
            {
                _lblProfitDisplay.Text = $"✅ Benta: ₱{price:N2} per item";
            }
            else
            {
                _lblProfitDisplay.Text = "";
            }

            _lblProfitDisplay.ForeColor = profit >= 0
                ? Color.FromArgb(39, 174, 96)
                : Color.FromArgb(231, 76, 60);
        }
    }
}
