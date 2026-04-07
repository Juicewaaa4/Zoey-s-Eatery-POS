using TransFundInventory.Data;
using TransFundInventory.Models;

namespace TransFundInventory.Forms
{
    public class CategoryControl : UserControl
    {
        private DataGridView dgvCategories = null!;
        private TextBox txtName = null!;
        private Button btnSave = null!;
        private Button btnClear = null!;
        private readonly CategoryRepository _categoryRepo = new();
        private Category? _editingCategory;

        public CategoryControl()
        {
            InitializeComponent();
            LoadCategories();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(240, 242, 245);

            var lblTitle = new Label
            {
                Text = "Categories",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32),
                Dock = DockStyle.Top,
                Height = 50
            };

            // Form panel (top)
            var panelForm = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                BackColor = Color.White,
                Padding = new Padding(20, 15, 20, 15)
            };

            var lblName = new Label
            {
                Text = "Category Name *",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 90),
                Location = new Point(20, 15),
                AutoSize = true
            };

            txtName = new TextBox
            {
                Location = new Point(20, 38),
                Size = new Size(250, 28),
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle
            };

            btnSave = new Button
            {
                Text = "💾 Save",
                Location = new Point(290, 33),
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            btnClear = new Button
            {
                Text = "Clear",
                Location = new Point(400, 33),
                Size = new Size(80, 35),
                Font = new Font("Segoe UI", 9),
                BackColor = Color.FromArgb(180, 185, 195),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnClear.FlatAppearance.BorderSize = 0;
            btnClear.Click += (s, e) => ClearForm();

            var btnDelete = new Button
            {
                Text = "🗑️ Delete",
                Location = new Point(490, 33),
                Size = new Size(100, 35),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Click += BtnDelete_Click;

            panelForm.Controls.AddRange(new Control[] { lblName, txtName, btnSave, btnClear, btnDelete });

            // DataGridView
            var panelGrid = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1),
                Margin = new Padding(0, 10, 0, 0)
            };

            dgvCategories = new DataGridView
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
                Font = new Font("Segoe UI", 9),
                MultiSelect = false
            };
            dgvCategories.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 235, 250);
            dgvCategories.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvCategories.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(27, 94, 32);
            dgvCategories.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvCategories.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvCategories.EnableHeadersVisualStyles = false;
            dgvCategories.ColumnHeadersHeight = 35;
            dgvCategories.RowTemplate.Height = 30;
            dgvCategories.CellClick += DgvCategories_CellClick;

            panelGrid.Controls.Add(dgvCategories);

            this.Controls.Add(panelGrid);
            this.Controls.Add(panelForm);
            this.Controls.Add(lblTitle);
        }

        private void LoadCategories()
        {
            var categories = _categoryRepo.GetAll();
            dgvCategories.DataSource = categories.Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                Products = _categoryRepo.GetProductCount(c.Id)
            }).ToList();

            if (dgvCategories.Columns.Count > 0)
            {
                dgvCategories.Columns["Id"].Visible = false;
                if (dgvCategories.Columns.Contains("Description"))
                    dgvCategories.Columns["Description"].Visible = false;
            }
        }

        private void DgvCategories_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var id = (int)dgvCategories.Rows[e.RowIndex].Cells["Id"].Value;
            _editingCategory = _categoryRepo.GetById(id);
            if (_editingCategory != null)
            {
                txtName.Text = _editingCategory.Name;
                btnSave.Text = "✏️ Update";
            }
        }

        private void ClearForm()
        {
            _editingCategory = null;
            txtName.Clear();
            btnSave.Text = "💾 Save";
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Category name is required.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            bool success;
            if (_editingCategory != null)
            {
                _editingCategory.Name = txtName.Text.Trim();
                _editingCategory.Description = ""; // Force empty
                success = _categoryRepo.Update(_editingCategory);
            }
            else
            {
                var category = new Category
                {
                    Name = txtName.Text.Trim(),
                    Description = "" // Force empty
                };
                success = _categoryRepo.Add(category);
            }

            if (success)
            {
                ClearForm();
                LoadCategories();
            }
            else
            {
                MessageBox.Show("Failed to save category. Name might already exist.", "Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (dgvCategories.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a category to delete.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var id = (int)dgvCategories.SelectedRows[0].Cells["Id"].Value;
            var productCount = _categoryRepo.GetProductCount(id);

            if (productCount > 0)
            {
                MessageBox.Show($"Cannot delete category. It has {productCount} product(s) assigned.",
                    "Cannot Delete", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show("Delete this category?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                _categoryRepo.Delete(id);
                ClearForm();
                LoadCategories();
            }
        }
    }
}
