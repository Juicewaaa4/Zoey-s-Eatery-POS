using TransFundInventory.Data;
using TransFundInventory.Helpers;

namespace TransFundInventory.Forms
{
    public class MainForm : Form
    {
        private Panel panelSidebar = null!;
        private Panel panelContent = null!;
        private Panel panelHeader = null!;
        private Label lblCurrentUser = null!;
        private Label lblHeaderTitle = null!;
        private Button? activeButton;

        // Sidebar colors — Green professional theme
        private readonly Color sidebarColor = Color.FromArgb(27, 94, 32);
        private readonly Color sidebarHover = Color.FromArgb(46, 125, 50);
        private readonly Color sidebarActive = Color.FromArgb(56, 142, 60);

        public MainForm()
        {
            InitializeComponent();
            if (SessionManager.IsAdmin)
                ShowDashboard();
            else
                ShowPOS();
            CheckLowStockAlert();
        }

        private void CheckLowStockAlert()
        {
            try
            {
                var repo = new ProductRepository();
                int lowStockCount = repo.GetLowStockCount();
                if (lowStockCount > 0)
                {
                    // Send email notification to owner (background)
                    EmailService.SendLowStockNotification(lowStockCount, SessionManager.CurrentSection);

                    MessageBox.Show($"ATTENTION!\n\nYou have {lowStockCount} item(s) running low on stock in the '{SessionManager.CurrentSection}' section.\nPlease review your Inventory immediately to avoid running out.", "🚨 Low Stock Alert", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch { }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            var section = SessionManager.CurrentSection;
            var sectionIcon = section == "Store" ? "🏬" : "🍽️";

            this.Text = $"Zoey's Billiard House - {section}";
            this.Size = new Size(1280, 750);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(240, 242, 245);
            this.MinimumSize = new Size(1100, 650);

            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "appicon.ico");
            if (File.Exists(iconPath)) this.Icon = new Icon(iconPath);

            // Header
            panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 50,
                BackColor = Color.White,
                Padding = new Padding(10, 0, 10, 0)
            };

            var headerBorder = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 1,
                BackColor = Color.FromArgb(220, 225, 230)
            };

            // Logo in header
            var headerLogoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "zoeyslogo.png");
            if (File.Exists(headerLogoPath))
            {
                var picHeaderLogo = new PictureBox
                {
                    Image = Image.FromFile(headerLogoPath),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Dock = DockStyle.Left,
                    Width = 40,
                    Padding = new Padding(220, 5, 0, 5)
                };
                panelHeader.Controls.Add(picHeaderLogo);
            }

            lblHeaderTitle = new Label
            {
                Text = $"  {sectionIcon}  Zoey's Billiard House — {section}",
                Font = new Font("Segoe UI", 13, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32),
                Dock = DockStyle.Left,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(225, 12, 0, 0)
            };

            lblCurrentUser = new Label
            {
                Text = $"👤 {SessionManager.CurrentUser?.FullName} ({SessionManager.CurrentUser?.Role})",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(80, 90, 110),
                Dock = DockStyle.Right,
                AutoSize = true,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 15, 10, 0)
            };

            panelHeader.Controls.Add(lblHeaderTitle);
            panelHeader.Controls.Add(lblCurrentUser);
            panelHeader.Controls.Add(headerBorder);

            // Sidebar
            panelSidebar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 220,
                BackColor = sidebarColor
            };

            var panelBrand = new Panel
            {
                Dock = DockStyle.Top,
                Height = 140,
                BackColor = Color.FromArgb(21, 71, 24) // Darkest green
            };

            var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "zoeyslogo.png");
            if (File.Exists(logoPath))
            {
                var picLogo = new PictureBox
                {
                    Image = Image.FromFile(logoPath),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Dock = DockStyle.Top,
                    Height = 90,
                    Padding = new Padding(0, 15, 0, 0)
                };
                panelBrand.Controls.Add(picLogo);
            }

            var lblBrand = new Label
            {
                Text = "ZOEY'S",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Bottom,
                Height = 40,
                TextAlign = ContentAlignment.TopCenter
            };
            panelBrand.Controls.Add(lblBrand);

            // Section indicator label
            var lblSectionIndicator = new Label
            {
                Text = $"  {sectionIcon}  {section.ToUpper()} SECTION",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = section == "Store" ? Color.FromArgb(46, 125, 50) : Color.FromArgb(230, 126, 34),
                Dock = DockStyle.Top,
                Height = 28,
                TextAlign = ContentAlignment.MiddleCenter
            };

            // ── Sidebar section: Operations ──
            var lblOpsSection = CreateSectionLabel("── OPERATIONS ──");

            Button? btnDashboard = null;
            if (SessionManager.IsAdmin)
            {
                btnDashboard = CreateMenuButton($"{sectionIcon}  Dashboard");
                btnDashboard.Click += (s, e) => { SetActiveButton(btnDashboard); ShowDashboard(); };
            }

            var btnPOS = CreateMenuButton("🛒  POS / Checkout");
            btnPOS.Click += (s, e) => { SetActiveButton(btnPOS); ShowPOS(); };

            Button? btnAnalytics = null;
            if (SessionManager.IsAdmin)
            {
                btnAnalytics = CreateMenuButton("📈  Sales Analytics");
                btnAnalytics.Click += (s, e) => { SetActiveButton(btnAnalytics); ShowAnalytics(); };
            }

            Button? btnProducts = null;
            Button? btnCategories = null;

            if (SessionManager.IsAdmin)
            {
                btnProducts = CreateMenuButton(section == "Eatery" ? "🍔  MENU" : "📦  Products");
                btnProducts.Click += (s, e) => { SetActiveButton(btnProducts); ShowProducts(); };

                btnCategories = CreateMenuButton(section == "Eatery" ? "📁  FOOD CATEGORIES" : "📁  Categories");
                btnCategories.Click += (s, e) => { SetActiveButton(btnCategories); ShowCategories(); };
            }

            var btnStockIO = CreateMenuButton("🔄  Stock In/Out");
            btnStockIO.Click += (s, e) => { SetActiveButton(btnStockIO); ShowStockTransactions(); };
            if (section == "Eatery") btnStockIO.Visible = false;
            
            // Removed Drinks Stock as requested

            Button? btnReports = null;
            if (SessionManager.IsAdmin)
            {
                btnReports = CreateMenuButton("📊  Reports");
                btnReports.Click += (s, e) => { SetActiveButton(btnReports); ShowReports(); };
            }

            Button? btnAuditLog = null;
            if (SessionManager.IsAdmin)
            {
                btnAuditLog = CreateMenuButton("📝  Activity Log");
                btnAuditLog.Click += (s, e) => { SetActiveButton(btnAuditLog); ShowAuditLog(); };
            }

            Button? btnUsers = null;
            if (SessionManager.IsAdmin)
            {
                btnUsers = CreateMenuButton("👥  Users");
                btnUsers.Click += (s, e) => { SetActiveButton(btnUsers); ShowUserManagement(); };
            }

            // Switch section button
            var otherSection = section == "Store" ? "Eatery" : "Store";
            var otherIcon = section == "Store" ? "🍽️" : "🏬";
            var btnSwitch = new Button
            {
                Text = $"🔀  Switch to {otherSection}",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.White,
                BackColor = section == "Store" ? Color.FromArgb(230, 126, 34) : Color.FromArgb(46, 125, 50),
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Bottom,
                Height = 35,
                TextAlign = ContentAlignment.MiddleCenter,
                Cursor = Cursors.Hand
            };
            btnSwitch.FlatAppearance.BorderSize = 0;
            btnSwitch.FlatAppearance.MouseOverBackColor = section == "Store" 
                ? Color.FromArgb(211, 84, 0) : Color.FromArgb(27, 94, 32);
            btnSwitch.Click += (s, e) =>
            {
                SessionManager.CurrentSection = otherSection;
                this.Hide();
                var newMain = new MainForm();
                newMain.FormClosed += (s2, args) => this.Close();
                newMain.Show();
            };

            // Bottom buttons
            Button? btnEmailSettings = null;
            if (SessionManager.IsAdmin)
            {
                btnEmailSettings = new Button
                {
                    Text = "📧  Email Notifications",
                    Font = new Font("Segoe UI", 9),
                    ForeColor = Color.FromArgb(180, 210, 180),
                    BackColor = sidebarColor,
                    FlatStyle = FlatStyle.Flat,
                    Dock = DockStyle.Bottom,
                    Height = 35,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(15, 0, 0, 0),
                    Cursor = Cursors.Hand
                };
                btnEmailSettings.FlatAppearance.BorderSize = 0;
                btnEmailSettings.FlatAppearance.MouseOverBackColor = sidebarHover;
                btnEmailSettings.Click += (s, e) =>
                {
                    var form = new EmailSettingsForm();
                    form.ShowDialog();
                };
            }

            Button? btnBackup = null;
            if (SessionManager.IsAdmin)
            {
                btnBackup = new Button
                {
                    Text = "💾  Backup / Restore",
                    Font = new Font("Segoe UI", 9),
                    ForeColor = Color.FromArgb(180, 210, 180),
                    BackColor = sidebarColor,
                    FlatStyle = FlatStyle.Flat,
                    Dock = DockStyle.Bottom,
                    Height = 35,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Padding = new Padding(15, 0, 0, 0),
                    Cursor = Cursors.Hand
                };
                btnBackup.FlatAppearance.BorderSize = 0;
                btnBackup.FlatAppearance.MouseOverBackColor = sidebarHover;
                btnBackup.Click += BtnBackup_Click;
            }

            var btnLogout = new Button
            {
                Text = "🚪  Logout",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(200, 220, 200),
                BackColor = sidebarColor,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Bottom,
                Height = 35,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btnLogout.FlatAppearance.BorderSize = 0;
            btnLogout.FlatAppearance.MouseOverBackColor = Color.FromArgb(150, 40, 40);
            btnLogout.Click += BtnLogout_Click;

            panelSidebar.Controls.Add(btnLogout);
            if (btnEmailSettings != null) panelSidebar.Controls.Add(btnEmailSettings);
            if (btnBackup != null) panelSidebar.Controls.Add(btnBackup);
            panelSidebar.Controls.Add(btnSwitch);
            if (btnUsers != null) panelSidebar.Controls.Add(btnUsers);
            if (btnAuditLog != null) panelSidebar.Controls.Add(btnAuditLog);
            if (btnReports != null) panelSidebar.Controls.Add(btnReports);
            // btnSoftDrinksStock removed
            panelSidebar.Controls.Add(btnStockIO);
            if (btnCategories != null) panelSidebar.Controls.Add(btnCategories);
            if (btnProducts != null) panelSidebar.Controls.Add(btnProducts);
            if (btnAnalytics != null) panelSidebar.Controls.Add(btnAnalytics);
            panelSidebar.Controls.Add(btnPOS);
            if (btnDashboard != null) panelSidebar.Controls.Add(btnDashboard);
            panelSidebar.Controls.Add(lblOpsSection);
            panelSidebar.Controls.Add(lblSectionIndicator);
            panelSidebar.Controls.Add(panelBrand);

            // Content area
            panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(240, 242, 245),
                Padding = new Padding(20)
            };

            this.Controls.Add(panelContent);
            this.Controls.Add(panelSidebar);
            this.Controls.Add(panelHeader);

            if (SessionManager.IsAdmin && btnDashboard != null)
                SetActiveButton(btnDashboard);
            else
                SetActiveButton(btnPOS);

            // Log login
            var auditRepo = new AuditLogRepository();
            auditRepo.Log(SessionManager.CurrentUser?.Id ?? 1, "Section Entry",
                $"{SessionManager.CurrentUser?.FullName} entered {section} section");

            this.ResumeLayout(false);
        }

        private Label CreateSectionLabel(string text)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 7, FontStyle.Bold),
                ForeColor = Color.FromArgb(129, 199, 132),
                BackColor = sidebarColor,
                Dock = DockStyle.Top,
                Height = 25,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(0, 5, 0, 0)
            };
        }

        private Button CreateMenuButton(string text)
        {
            var btn = new Button
            {
                Text = text,
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(180, 210, 180),
                BackColor = sidebarColor,
                FlatStyle = FlatStyle.Flat,
                Dock = DockStyle.Top,
                Height = 36,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(20, 0, 0, 0),
                Cursor = Cursors.Hand
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor = sidebarHover;
            return btn;
        }

        private void SetActiveButton(Button btn)
        {
            if (activeButton != null)
            {
                activeButton.BackColor = sidebarColor;
                activeButton.ForeColor = Color.FromArgb(180, 210, 180);
            }
            activeButton = btn;
            btn.BackColor = sidebarActive;
            btn.ForeColor = Color.White;
        }

        private void SwitchContent(UserControl control)
        {
            panelContent.Controls.Clear();
            control.Dock = DockStyle.Fill;
            panelContent.Controls.Add(control);
        }

        private void ShowDashboard()
        {
            if (SessionManager.CurrentSection == "Store")
                SwitchContent(new StoreDashboardControl());
            else
                SwitchContent(new EateryDashboardControl());
        }

        private void ShowPOS() => SwitchContent(new POSControl());
        private void ShowAnalytics() => SwitchContent(new SalesAnalyticsControl());
        private void ShowProducts() => SwitchContent(new ProductListControl());
        private void ShowCategories() => SwitchContent(new CategoryControl());
        private void ShowStockTransactions() => SwitchContent(new StockTransactionControl());
        private void ShowReports() => SwitchContent(new ReportsControl());
        private void ShowAuditLog() => SwitchContent(new AuditLogControl());
        private void ShowUserManagement() => SwitchContent(new UserManagementControl());

        private void BtnBackup_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show("Choose an action:\n\nYes = Backup database\nNo = Restore from backup",
                "Backup / Restore", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);

            if (result == DialogResult.Yes)
            {
                using var dialog = new SaveFileDialog
                {
                    Filter = "SQLite Database|*.db",
                    FileName = $"ZoeysStore_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db",
                    Title = "Save Backup"
                };
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    if (BackupHelper.BackupDatabase(dialog.FileName))
                    {
                        var auditRepo = new AuditLogRepository();
                        auditRepo.Log(SessionManager.CurrentUser?.Id ?? 1, "Backup",
                            $"Database backed up to {dialog.FileName}");
                        MessageBox.Show("Backup created successfully!", "Success",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            else if (result == DialogResult.No)
            {
                var confirm = MessageBox.Show(
                    "⚠️ Restoring will replace ALL current data!\n\nAre you sure?",
                    "Confirm Restore", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (confirm == DialogResult.Yes)
                {
                    using var dialog = new OpenFileDialog
                    {
                        Filter = "SQLite Database|*.db",
                        Title = "Select Backup File"
                    };
                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        if (BackupHelper.RestoreDatabase(dialog.FileName))
                        {
                            MessageBox.Show("Database restored! The app will restart.", "Success",
                                MessageBoxButtons.OK, MessageBoxIcon.Information);
                            Application.Restart();
                        }
                    }
                }
            }
        }

        private void BtnLogout_Click(object? sender, EventArgs e)
        {
            var result = MessageBox.Show("Are you sure you want to logout?", "Confirm Logout",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                var auditRepo = new AuditLogRepository();
                auditRepo.Log(SessionManager.CurrentUser?.Id ?? 1, "Logout",
                    $"{SessionManager.CurrentUser?.FullName} logged out");
                SessionManager.Logout();
                this.Hide();
                var loginForm = new LoginForm();
                loginForm.FormClosed += (s, args) => this.Close();
                loginForm.Show();
            }
        }
    }
}
