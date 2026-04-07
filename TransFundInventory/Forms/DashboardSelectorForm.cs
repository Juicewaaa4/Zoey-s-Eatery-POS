using TransFundInventory.Helpers;

namespace TransFundInventory.Forms
{
    public class DashboardSelectorForm : Form
    {
        public DashboardSelectorForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = "Zoey's Billiard House - Select Section";
            this.Size = new Size(700, 560);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.FromArgb(240, 242, 245);

            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "appicon.ico");
            if (File.Exists(iconPath)) this.Icon = new Icon(iconPath);

            // Logo at top
            var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "zoeyslogo.png");
            if (File.Exists(logoPath))
            {
                var picLogo = new PictureBox
                {
                    Image = Image.FromFile(logoPath),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = new Size(160, 160),
                    Location = new Point(270, 10),
                    BackColor = Color.Transparent
                };
                this.Controls.Add(picLogo);
            }

            var lblWelcome = new Label
            {
                Text = $"Welcome, {SessionManager.CurrentUser?.FullName}!",
                Font = new Font("Segoe UI", 14, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 180),
                Size = new Size(700, 30)
            };

            var lblChoose = new Label
            {
                Text = "Choose a section to manage:",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(80, 90, 110),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 210),
                Size = new Size(700, 25)
            };

            // Store card
            var panelStore = CreateSectionCard(
                "🏬  STORE",
                "Manage store products, inventory,\nPOS checkout, and reports.",
                Color.FromArgb(27, 94, 32),
                Color.FromArgb(240, 248, 240),
                new Point(70, 260)
            );
            panelStore.Click += (s, e) => SelectSection("Store");
            foreach (Control c in panelStore.Controls) c.Click += (s, e) => SelectSection("Store");

            // Eatery card
            var panelEatery = CreateSectionCard(
                "🍽️  EATERY",
                "Manage eatery menu, food stock,\nPOS checkout, and reports.",
                Color.FromArgb(230, 126, 34),
                Color.FromArgb(253, 245, 235),
                new Point(380, 260)
            );
            panelEatery.Click += (s, e) => SelectSection("Eatery");
            foreach (Control c in panelEatery.Controls) c.Click += (s, e) => SelectSection("Eatery");

            // Logout button
            var btnLogout = new Button
            {
                Text = "🚪 Logout",
                Location = new Point(290, 480),
                Size = new Size(120, 38),
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.White,
                ForeColor = Color.FromArgb(231, 76, 60),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLogout.FlatAppearance.BorderColor = Color.FromArgb(231, 76, 60);
            btnLogout.FlatAppearance.BorderSize = 1;

            btnLogout.MouseEnter += (s, e) => { btnLogout.BackColor = Color.FromArgb(253, 235, 235); };
            btnLogout.MouseLeave += (s, e) => { btnLogout.BackColor = Color.White; };
            btnLogout.Click += (s, e) =>
            {
                SessionManager.Logout();
                this.Hide();
                var loginForm = new LoginForm();
                loginForm.FormClosed += (s2, args2) => this.Close();
                loginForm.Show();
            };

            this.Controls.Add(lblWelcome);
            this.Controls.Add(lblChoose);
            this.Controls.Add(panelStore);
            this.Controls.Add(panelEatery);
            this.Controls.Add(btnLogout);

            this.ResumeLayout(false);
        }

        private Panel CreateSectionCard(string title, string description, Color accentColor, Color hoverColor, Point location)
        {
            var card = new Panel
            {
                Location = location,
                Size = new Size(250, 190),
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                BorderStyle = BorderStyle.FixedSingle
            };

            var accentBar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 5,
                BackColor = accentColor
            };

            var lblTitle = new Label
            {
                Text = title,
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = accentColor,
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 30),
                Size = new Size(250, 45),
                Cursor = Cursors.Hand
            };

            var lblDesc = new Label
            {
                Text = description,
                Font = new Font("Segoe UI", 10, FontStyle.Regular),
                ForeColor = Color.FromArgb(60, 60, 60),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(10, 80),
                Size = new Size(230, 50),
                Cursor = Cursors.Hand
            };

            var btnEnter = new Button
            {
                Text = "Enter →",
                Location = new Point(65, 145),
                Size = new Size(120, 32),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = accentColor,
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnEnter.FlatAppearance.BorderSize = 0;

            card.Controls.Add(btnEnter);
            card.Controls.Add(lblDesc);
            card.Controls.Add(lblTitle);
            card.Controls.Add(accentBar);

            // Hover effect
            card.MouseEnter += (s, e) => card.BackColor = hoverColor;
            card.MouseLeave += (s, e) => card.BackColor = Color.White;
            foreach (Control c in card.Controls)
            {
                c.MouseEnter += (s, e) => card.BackColor = hoverColor;
                c.MouseLeave += (s, e) => card.BackColor = Color.White;
            }

            return card;
        }

        private void SelectSection(string section)
        {
            SessionManager.CurrentSection = section;
            this.Hide();
            var mainForm = new MainForm();
            mainForm.FormClosed += (s, args) => this.Close();
            mainForm.Show();
        }
    }
}
