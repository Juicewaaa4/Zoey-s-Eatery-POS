using TransFundInventory.Data;
using TransFundInventory.Helpers;

namespace TransFundInventory.Forms
{
    public class LoginForm : Form
    {
        private TextBox txtUsername = null!;
        private TextBox txtPassword = null!;
        private Button btnLogin = null!;
        private Button btnTogglePassword = null!;
        private Label lblTitle = null!;
        private Label lblSubtitle = null!;
        private Panel panelLeft = null!;
        private Panel panelRight = null!;
        private Label lblError = null!;

        public LoginForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = "Zoey's Billiard House - Login";
            this.Size = new Size(900, 520);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.BackColor = Color.White;

            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "appicon.ico");
            if (File.Exists(iconPath)) this.Icon = new Icon(iconPath);

            // Left panel - branding
            panelLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 380,
                BackColor = Color.FromArgb(27, 94, 32) // Deep forest green
            };

            // Logo on left panel
            var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "zoeyslogo.png");
            PictureBox? picLogo = null;
            if (File.Exists(logoPath))
            {
                picLogo = new PictureBox
                {
                    Image = Image.FromFile(logoPath),
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Dock = DockStyle.Top,
                    Height = 240,
                    Padding = new Padding(70, 40, 70, 0),
                    BackColor = Color.Transparent
                };
            }

            var lblBrand = new Label
            {
                Text = "ZOEY'S\nBILLIARD HOUSE",
                Font = new Font("Segoe UI", 18, FontStyle.Bold),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.TopCenter,
                Dock = DockStyle.Fill,
                Padding = new Padding(0, 10, 0, 0)
            };

            var lblDeveloper = new Label
            {
                Text = "Developed by Lloyd Joshua De Lara",
                Font = new Font("Segoe UI", 8, FontStyle.Italic),
                ForeColor = Color.FromArgb(129, 199, 132),
                TextAlign = ContentAlignment.BottomCenter,
                Dock = DockStyle.Bottom,
                Height = 25,
                Padding = new Padding(0, 0, 0, 8)
            };

            var lblTagline = new Label
            {
                Text = "Paltao, Pulilan, Bulacan",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(129, 199, 132), // Light green
                TextAlign = ContentAlignment.TopCenter,
                Dock = DockStyle.Bottom,
                Height = 35,
                Padding = new Padding(0, 0, 0, 0)
            };

            panelLeft.Controls.Add(lblBrand);
            if (picLogo != null) panelLeft.Controls.Add(picLogo);
            panelLeft.Controls.Add(lblTagline);
            panelLeft.Controls.Add(lblDeveloper);

            // Right panel - login form
            panelRight = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(245, 247, 250),
                Padding = new Padding(60, 40, 60, 40)
            };

            lblTitle = new Label
            {
                Text = "Welcome Back",
                Font = new Font("Segoe UI", 22, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32),
                Location = new Point(60, 70),
                AutoSize = true
            };

            lblSubtitle = new Label
            {
                Text = "Sign in to your account",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(120, 130, 150),
                Location = new Point(60, 110),
                AutoSize = true
            };

            var lblUser = new Label
            {
                Text = "Username",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 90),
                Location = new Point(60, 170),
                AutoSize = true
            };

            // USERNAME FIELD
            var pnlUser = new Panel
            {
                Location = new Point(60, 195),
                Size = new Size(360, 35),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.IBeam
            };

            txtUsername = new TextBox
            {
                Location = new Point(5, 4),
                Size = new Size(346, 25),
                Font = new Font("Segoe UI", 12),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White
            };
            pnlUser.Click += (s, e) => txtUsername.Focus();
            pnlUser.Controls.Add(txtUsername);

            var lblPass = new Label
            {
                Text = "Password",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 90),
                Location = new Point(60, 245),
                AutoSize = true
            };

            // PASSWORD FIELD
            var pnlPass = new Panel
            {
                Location = new Point(60, 270),
                Size = new Size(360, 35),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                Cursor = Cursors.IBeam
            };

            txtPassword = new TextBox
            {
                Location = new Point(5, 4),
                Size = new Size(310, 25),
                Font = new Font("Segoe UI", 12),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                UseSystemPasswordChar = true
            };
            pnlPass.Click += (s, e) => txtPassword.Focus();

            // Eye icon toggle button for password visibility
            btnTogglePassword = new Button
            {
                Text = "👁",
                Dock = DockStyle.Right,
                Width = 40,
                Font = new Font("Segoe UI", 11),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(120, 130, 150),
                Cursor = Cursors.Hand,
                TabStop = false
            };
            btnTogglePassword.FlatAppearance.BorderSize = 0;
            btnTogglePassword.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 245, 245);
            btnTogglePassword.FlatAppearance.MouseDownBackColor = Color.FromArgb(235, 235, 235);
            btnTogglePassword.Click += (s, e) =>
            {
                txtPassword.UseSystemPasswordChar = !txtPassword.UseSystemPasswordChar;
                btnTogglePassword.Text = txtPassword.UseSystemPasswordChar ? "👁" : "🙈";
                txtPassword.Focus();
            };

            pnlPass.Controls.Add(btnTogglePassword);
            pnlPass.Controls.Add(txtPassword);

            btnLogin = new Button
            {
                Text = "SIGN IN",
                Location = new Point(60, 330),
                Size = new Size(360, 45),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = Color.FromArgb(27, 94, 32),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnLogin.FlatAppearance.BorderSize = 0;
            btnLogin.FlatAppearance.MouseOverBackColor = Color.FromArgb(46, 125, 50);
            btnLogin.Click += BtnLogin_Click;

            lblError = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(220, 50, 50),
                Location = new Point(60, 385),
                AutoSize = true
            };

            panelRight.Controls.AddRange(new Control[] {
                lblTitle, lblSubtitle, lblUser, pnlUser,
                lblPass, pnlPass, btnLogin, lblError
            });

            this.Controls.Add(panelRight);
            this.Controls.Add(panelLeft);
            this.AcceptButton = btnLogin;

            this.ResumeLayout(false);
        }

        private void BtnLogin_Click(object? sender, EventArgs e)
        {
            lblError.Text = "";

            if (string.IsNullOrWhiteSpace(txtUsername.Text) || string.IsNullOrWhiteSpace(txtPassword.Text))
            {
                lblError.Text = "Please enter both username and password.";
                return;
            }

            var userRepo = new UserRepository();
            var user = userRepo.Authenticate(txtUsername.Text.Trim(), txtPassword.Text);

            if (user != null)
            {
                SessionManager.CurrentUser = user;

                // Send email notification to owner (runs in background, non-blocking)
                EmailService.SendLoginNotification(user.Username, user.FullName, user.Role, DateTime.Now);

                this.Hide();
                // Go to dashboard selector instead of directly to MainForm
                var selectorForm = new DashboardSelectorForm();
                selectorForm.FormClosed += (s, args) => this.Close();
                selectorForm.Show();
            }
            else
            {
                lblError.Text = "Invalid username or password.";
                txtPassword.Clear();
                txtPassword.Focus();
            }
        }
    }
}
