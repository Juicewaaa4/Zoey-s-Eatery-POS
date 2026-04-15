using TransFundInventory.Data;
using TransFundInventory.Models;

namespace TransFundInventory.Forms
{
    public class UserManagementControl : UserControl
    {
        private DataGridView dgvUsers = null!;
        private TextBox txtUsername = null!;
        private TextBox txtFullName = null!;
        private TextBox txtPassword = null!;
        private ComboBox cmbRole = null!;
        private Button btnSave = null!;
        private readonly UserRepository _userRepo = new();
        private User? _editingUser;

        public UserManagementControl()
        {
            InitializeComponent();
            LoadUsers();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(240, 242, 245);

            var lblTitle = new Label
            {
                Text = "User Management",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32),
                Dock = DockStyle.Top,
                Height = 50
            };

            // Form panel
            var panelForm = new Panel
            {
                Dock = DockStyle.Top,
                Height = 110,
                BackColor = Color.White,
                Padding = new Padding(20, 15, 20, 15)
            };

            var lblUser = new Label
            {
                Text = "Username *",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 90),
                Location = new Point(20, 10),
                AutoSize = true
            };

            txtUsername = new TextBox
            {
                Location = new Point(20, 32),
                Size = new Size(160, 28),
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblName = new Label
            {
                Text = "Full Name *",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 90),
                Location = new Point(200, 10),
                AutoSize = true
            };

            txtFullName = new TextBox
            {
                Location = new Point(200, 32),
                Size = new Size(200, 28),
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle
            };

            var lblPass = new Label
            {
                Text = "Password *",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 90),
                Location = new Point(420, 10),
                AutoSize = true
            };

            txtPassword = new TextBox
            {
                Location = new Point(420, 32),
                Size = new Size(160, 28),
                Font = new Font("Segoe UI", 10),
                BorderStyle = BorderStyle.FixedSingle,
                UseSystemPasswordChar = true
            };

            var lblRole = new Label
            {
                Text = "Role",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 90),
                Location = new Point(600, 10),
                AutoSize = true
            };

            cmbRole = new ComboBox
            {
                Location = new Point(600, 32),
                Size = new Size(100, 28),
                Font = new Font("Segoe UI", 10),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbRole.Items.AddRange(new object[] { "Admin", "Cashier" });
            cmbRole.SelectedIndex = 1;

            btnSave = new Button
            {
                Text = "💾 Add User",
                Location = new Point(20, 68),
                Size = new Size(130, 32),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += BtnSave_Click;

            var btnClear = new Button
            {
                Text = "Clear",
                Location = new Point(160, 68),
                Size = new Size(80, 32),
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
                Location = new Point(250, 68),
                Size = new Size(100, 32),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnDelete.FlatAppearance.BorderSize = 0;
            btnDelete.Click += BtnDelete_Click;

            var btnResetPass = new Button
            {
                Text = "🔑 Reset Password",
                Location = new Point(360, 68),
                Size = new Size(150, 32),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(52, 120, 246),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnResetPass.FlatAppearance.BorderSize = 0;
            btnResetPass.Click += BtnResetPassword_Click;

            panelForm.Controls.AddRange(new Control[] {
                lblUser, txtUsername, lblName, txtFullName,
                lblPass, txtPassword, lblRole, cmbRole,
                btnSave, btnClear, btnDelete, btnResetPass
            });

            // DataGridView
            var panelGrid = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1)
            };

            dgvUsers = new DataGridView
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
            dgvUsers.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 235, 250);
            dgvUsers.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvUsers.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(27, 94, 32);
            dgvUsers.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvUsers.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvUsers.EnableHeadersVisualStyles = false;
            dgvUsers.ColumnHeadersHeight = 35;
            dgvUsers.RowTemplate.Height = 30;
            dgvUsers.CellClick += DgvUsers_CellClick;

            panelGrid.Controls.Add(dgvUsers);

            this.Controls.Add(panelGrid);
            this.Controls.Add(panelForm);
            this.Controls.Add(lblTitle);
        }

        private void LoadUsers()
        {
            var users = _userRepo.GetAll();
            dgvUsers.DataSource = users.Select(u => new
            {
                u.Id,
                u.Username,
                u.FullName,
                u.Role,
                Created = u.CreatedAt
            }).ToList();

            if (dgvUsers.Columns.Count > 0)
                dgvUsers.Columns["Id"].Visible = false;
        }

        private void DgvUsers_CellClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            var id = (int)dgvUsers.Rows[e.RowIndex].Cells["Id"].Value;
            var users = _userRepo.GetAll();
            _editingUser = users.FirstOrDefault(u => u.Id == id);

            if (_editingUser != null)
            {
                txtUsername.Text = _editingUser.Username;
                txtFullName.Text = _editingUser.FullName;
                txtPassword.Clear();
                txtPassword.PlaceholderText = "(leave blank to keep current)";
                cmbRole.SelectedItem = _editingUser.Role;
                btnSave.Text = "✏️ Update";
            }
        }

        private void ClearForm()
        {
            _editingUser = null;
            txtUsername.Clear();
            txtFullName.Clear();
            txtPassword.Clear();
            txtPassword.PlaceholderText = "";
            cmbRole.SelectedIndex = 1;
            btnSave.Text = "💾 Add User";
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtFullName.Text))
            {
                MessageBox.Show("Username and Full Name are required.", "Validation Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_editingUser != null)
            {
                _editingUser.Username = txtUsername.Text.Trim();
                _editingUser.FullName = txtFullName.Text.Trim();
                _editingUser.Role = cmbRole.SelectedItem?.ToString() ?? "Cashier";

                if (_userRepo.Update(_editingUser))
                {
                    if (!string.IsNullOrWhiteSpace(txtPassword.Text))
                    {
                        _userRepo.UpdatePassword(_editingUser.Id, txtPassword.Text);
                    }
                    ClearForm();
                    LoadUsers();
                }
                else
                {
                    MessageBox.Show("Failed to update user.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                if (string.IsNullOrWhiteSpace(txtPassword.Text))
                {
                    MessageBox.Show("Password is required for new users.", "Validation Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var user = new User
                {
                    Username = txtUsername.Text.Trim(),
                    FullName = txtFullName.Text.Trim(),
                    Password = txtPassword.Text,
                    Role = cmbRole.SelectedItem?.ToString() ?? "Cashier"
                };

                if (_userRepo.Add(user))
                {
                    ClearForm();
                    LoadUsers();
                }
                else
                {
                    MessageBox.Show("Failed to add user. Username might already exist.", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void BtnDelete_Click(object? sender, EventArgs e)
        {
            if (dgvUsers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a user.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var id = (int)dgvUsers.SelectedRows[0].Cells["Id"].Value;
            var username = dgvUsers.SelectedRows[0].Cells["Username"].Value?.ToString();

            if (username == "admin")
            {
                MessageBox.Show("Cannot delete the default admin account.", "Not Allowed",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var result = MessageBox.Show($"Delete user '{username}'?", "Confirm",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

            if (result == DialogResult.Yes)
            {
                _userRepo.Delete(id);
                ClearForm();
                LoadUsers();
            }
        }

        private void BtnResetPassword_Click(object? sender, EventArgs e)
        {
            if (dgvUsers.SelectedRows.Count == 0)
            {
                MessageBox.Show("Please select a user.", "No Selection",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var id = (int)dgvUsers.SelectedRows[0].Cells["Id"].Value;
            var username = dgvUsers.SelectedRows[0].Cells["Username"].Value?.ToString();

            var result = MessageBox.Show($"Reset password for '{username}' to 'password123'?",
                "Confirm Reset", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                _userRepo.UpdatePassword(id, "password123");
                MessageBox.Show("Password has been reset to 'password123'.", "Done",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}
