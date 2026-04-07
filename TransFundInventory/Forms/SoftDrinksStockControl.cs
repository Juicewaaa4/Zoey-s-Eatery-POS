using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TransFundInventory.Data;
using TransFundInventory.Helpers;
using TransFundInventory.Models;

namespace TransFundInventory.Forms
{
    public class SoftDrinksStockControl : UserControl
    {
        private DataGridView dgvProducts = null!;
        private readonly ProductRepository _productRepo = new();
        private readonly CategoryRepository _categoryRepo = new();
        private readonly AuditLogRepository _auditRepo = new();
        private ComboBox cmbCategory = null!;
        private int _selectedCategoryId = 0;

        public SoftDrinksStockControl()
        {
            InitializeComponent();
            FindCategory();
            LoadProducts();
        }

        private void FindCategory()
        {
            var drinksCategory = _categoryRepo.GetAll()
                .FirstOrDefault(c => c.Name.Contains("Soft", StringComparison.OrdinalIgnoreCase) 
                                  || c.Name.Contains("Drink", StringComparison.OrdinalIgnoreCase));
            
            if (drinksCategory != null)
            {
                _selectedCategoryId = drinksCategory.Id;
            }
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(240, 242, 245);
            this.Dock = DockStyle.Fill;
            this.Padding = new Padding(20);

            var lblTitle = new Label
            {
                Text = "🥤 Soft Drinks Stock Management",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32),
                Dock = DockStyle.Top,
                Height = 50
            };

            var panelTop = new Panel { Dock = DockStyle.Top, Height = 45, Padding = new Padding(0, 0, 0, 10) };
            cmbCategory = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Font = new Font("Segoe UI", 12),
                Width = 300,
                Dock = DockStyle.Left
            };
            cmbCategory.SelectedIndexChanged += CmbCategory_SelectedIndexChanged;
            panelTop.Controls.Add(cmbCategory);

            var panelGrid = new Panel
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
                ReadOnly = true,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                RowHeadersVisible = false,
                CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal,
                GridColor = Color.FromArgb(235, 240, 245),
                Font = new Font("Segoe UI", 11),
                MultiSelect = false
            };
            dgvProducts.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 235, 250);
            dgvProducts.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvProducts.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(27, 94, 32);
            dgvProducts.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvProducts.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            dgvProducts.EnableHeadersVisualStyles = false;
            dgvProducts.ColumnHeadersHeight = 40;
            dgvProducts.RowTemplate.Height = 40;
            
            dgvProducts.CellFormatting += DgvProducts_CellFormatting;
            dgvProducts.CellContentClick += DgvProducts_CellContentClick;

            panelGrid.Controls.Add(dgvProducts);

            this.Controls.Add(panelGrid);
            this.Controls.Add(panelTop);
            this.Controls.Add(lblTitle);
        }

        private void LoadCategories()
        {
            cmbCategory.Items.Clear();
            cmbCategory.Items.Add(new Category { Id = 0, Name = "All Categories" });
            
            var categories = _categoryRepo.GetAll();
            int selectedIndex = 0;
            
            for (int i = 0; i < categories.Count; i++)
            {
                cmbCategory.Items.Add(categories[i]);
                if (categories[i].Id == _selectedCategoryId)
                {
                    selectedIndex = i + 1; // +1 because "All" is at index 0
                }
            }
            
            cmbCategory.DisplayMember = "Name";
            
            // Temporary detach event to prevent double load
            cmbCategory.SelectedIndexChanged -= CmbCategory_SelectedIndexChanged;
            cmbCategory.SelectedIndex = selectedIndex;
            cmbCategory.SelectedIndexChanged += CmbCategory_SelectedIndexChanged;
        }

        private void CmbCategory_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (cmbCategory.SelectedItem is Category cat)
            {
                _selectedCategoryId = cat.Id;
                LoadProducts();
            }
        }

        private void LoadProducts()
        {
            var products = _selectedCategoryId > 0 
                ? _productRepo.Search("", _selectedCategoryId) 
                : _productRepo.GetAll();
                
            dgvProducts.DataSource = products.Select(p => new
            {
                p.Id,
                Product = p.Name,
                Qty = p.Quantity,
                MinStock = p.MinStockLevel,
                Status = p.Quantity <= p.MinStockLevel ? "⚠️ LOW" : "✅ OK"
            }).ToList();

            if (dgvProducts.Columns.Count > 0)
            {
                dgvProducts.Columns["Id"].Visible = false;

                // Add button columns if they don't exist
                if (!dgvProducts.Columns.Contains("BtnSub"))
                {
                    var btnSub = new DataGridViewButtonColumn
                    {
                        Name = "BtnSub",
                        HeaderText = "Sub",
                        Text = "-",
                        UseColumnTextForButtonValue = true,
                        Width = 60,
                        FlatStyle = FlatStyle.Flat
                    };
                    btnSub.DefaultCellStyle.BackColor = Color.FromArgb(231, 76, 60);
                    btnSub.DefaultCellStyle.ForeColor = Color.White;
                    btnSub.DefaultCellStyle.Font = new Font("Segoe UI", 12, FontStyle.Bold);
                    dgvProducts.Columns.Add(btnSub);
                }

                // Add button columns if they don't exist
                if (!dgvProducts.Columns.Contains("BtnAdd"))
                {
                    var btnAdd = new DataGridViewButtonColumn
                    {
                        Name = "BtnAdd",
                        HeaderText = "Add",
                        Text = "+",
                        UseColumnTextForButtonValue = true,
                        Width = 60,
                        FlatStyle = FlatStyle.Flat
                    };
                    btnAdd.DefaultCellStyle.BackColor = Color.FromArgb(39, 174, 96);
                    btnAdd.DefaultCellStyle.ForeColor = Color.White;
                    btnAdd.DefaultCellStyle.Font = new Font("Segoe UI", 12, FontStyle.Bold);
                    dgvProducts.Columns.Add(btnAdd);
                }
            }
        }

        private void DgvProducts_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (dgvProducts.Columns[e.ColumnIndex].Name == "Status" && e.Value?.ToString() == "⚠️ LOW")
            {
                e.CellStyle!.ForeColor = Color.FromArgb(231, 76, 60);
                e.CellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            }
        }

        private void DgvProducts_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0 && e.ColumnIndex >= 0)
            {
                string colName = dgvProducts.Columns[e.ColumnIndex].Name;

                if (colName == "BtnAdd" || colName == "BtnSub")
                {
                    var id = (int)dgvProducts.Rows[e.RowIndex].Cells["Id"].Value;

                    if (colName == "BtnAdd")
                    {
                        AdjustStock(id, 1);
                    }
                    else if (colName == "BtnSub")
                    {
                        AdjustStock(id, -1);
                    }
                }
            }
        }

        private void AdjustStock(int productId, int change)
        {
            var product = _productRepo.GetById(productId);
            if (product != null)
            {
                if (change < 0 && product.Quantity <= 0)
                {
                    return; // Can't go below 0
                }

                product.Quantity += change;
                _productRepo.Update(product);

                string actionType = change > 0 ? "Stock In (Quick)" : "Stock Out (Quick)";
                _auditRepo.Log(SessionManager.CurrentUser?.Id ?? 1, actionType, 
                    $"{actionType} recorded {Math.Abs(change)} unit(s) for {product.Name}");

                LoadProducts(); 
            }
        }
    }
}
