using TransFundInventory.Data;
using TransFundInventory.Helpers;
using TransFundInventory.Models;

namespace TransFundInventory.Forms
{
    public class EmailSettingsForm : Form
    {
        private TextBox txtApiKey = null!;
        private TextBox txtOwnerEmail = null!;
        private TextBox txtOwnerName = null!;
        private CheckBox chkNotifyLogin = null!;
        private CheckBox chkNotifyLowStock = null!;
        private CheckBox chkEnabled = null!;
        private Button btnSave = null!;
        private Button btnTestEmail = null!;
        private Button btnToggleKey = null!;
        private Label lblStatus = null!;

        public EmailSettingsForm()
        {
            InitializeComponent();
            LoadSettings();
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();

            this.Text = "📧 Email Notification Settings";
            this.Size = new Size(550, 620);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.FromArgb(245, 247, 250);

            // Header
            var panelHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.FromArgb(27, 94, 32)
            };

            var lblTitle = new Label
            {
                Text = "📧  Email Notification Settings",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.White,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelHeader.Controls.Add(lblTitle);

            // Main content
            var panelContent = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(30, 20, 30, 20)
            };

            int y = 10;

            // --- Enable toggle ---
            chkEnabled = new CheckBox
            {
                Text = "  Enable Email Notifications",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32),
                Location = new Point(20, y),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            panelContent.Controls.Add(chkEnabled);
            y += 45;

            // --- Section: Resend API ---
            var lblApiSection = CreateSectionLabel("── RESEND API KEY ──", y);
            panelContent.Controls.Add(lblApiSection);
            y += 25;

            var lblApiHelp = new Label
            {
                Text = "Go to resend.com → Sign Up (free) → API Keys → Copy your key",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(120, 130, 150),
                Location = new Point(20, y),
                Size = new Size(460, 20)
            };
            panelContent.Controls.Add(lblApiHelp);
            y += 25;

            panelContent.Controls.Add(CreateFieldLabel("API Key", y));
            y += 20;

            var pnlApiKey = new Panel
            {
                Location = new Point(20, y),
                Size = new Size(460, 32),
                BackColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            txtApiKey = new TextBox
            {
                Location = new Point(5, 3),
                Size = new Size(410, 25),
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.None,
                BackColor = Color.White,
                UseSystemPasswordChar = true,
                PlaceholderText = "re_xxxxxxxxxx..."
            };

            btnToggleKey = new Button
            {
                Text = "👁",
                Dock = DockStyle.Right,
                Width = 40,
                Font = new Font("Segoe UI", 10),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.White,
                Cursor = Cursors.Hand,
                TabStop = false
            };
            btnToggleKey.FlatAppearance.BorderSize = 0;
            btnToggleKey.Click += (s, e) =>
            {
                txtApiKey.UseSystemPasswordChar = !txtApiKey.UseSystemPasswordChar;
                btnToggleKey.Text = txtApiKey.UseSystemPasswordChar ? "👁" : "🙈";
            };

            pnlApiKey.Controls.Add(btnToggleKey);
            pnlApiKey.Controls.Add(txtApiKey);
            panelContent.Controls.Add(pnlApiKey);
            y += 50;

            // --- Section: Owner/Receiver ---
            var lblOwnerSection = CreateSectionLabel("── OWNER / RECEIVER ──", y);
            panelContent.Controls.Add(lblOwnerSection);
            y += 25;

            var lblOwnerHelp = new Label
            {
                Text = "The owner's Gmail — this is where notifications will be sent.",
                Font = new Font("Segoe UI", 8),
                ForeColor = Color.FromArgb(120, 130, 150),
                Location = new Point(20, y),
                Size = new Size(460, 20)
            };
            panelContent.Controls.Add(lblOwnerHelp);
            y += 25;

            panelContent.Controls.Add(CreateFieldLabel("Owner Name", y));
            y += 20;
            txtOwnerName = CreateTextBox(y, "Zoey");
            panelContent.Controls.Add(txtOwnerName);
            y += 40;

            panelContent.Controls.Add(CreateFieldLabel("Owner Email (Gmail)", y));
            y += 20;
            txtOwnerEmail = CreateTextBox(y, "owner@gmail.com");
            panelContent.Controls.Add(txtOwnerEmail);
            y += 50;

            // --- Section: Notification Options ---
            var lblNotifSection = CreateSectionLabel("── NOTIFICATION TRIGGERS ──", y);
            panelContent.Controls.Add(lblNotifSection);
            y += 30;

            chkNotifyLogin = new CheckBox
            {
                Text = "  🔔 Notify when someone logs in",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(50, 60, 80),
                Location = new Point(20, y),
                AutoSize = true,
                Checked = true,
                Cursor = Cursors.Hand
            };
            panelContent.Controls.Add(chkNotifyLogin);
            y += 30;

            chkNotifyLowStock = new CheckBox
            {
                Text = "  ⚠️ Notify when stock is low",
                Font = new Font("Segoe UI", 10),
                ForeColor = Color.FromArgb(50, 60, 80),
                Location = new Point(20, y),
                AutoSize = true,
                Checked = true,
                Cursor = Cursors.Hand
            };
            panelContent.Controls.Add(chkNotifyLowStock);
            y += 45;

            // --- Buttons ---
            btnTestEmail = new Button
            {
                Text = "📧  Send Test Email",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(25, 118, 210),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(20, y),
                Size = new Size(220, 42),
                Cursor = Cursors.Hand
            };
            btnTestEmail.FlatAppearance.BorderSize = 0;
            btnTestEmail.FlatAppearance.MouseOverBackColor = Color.FromArgb(21, 101, 192);
            btnTestEmail.Click += BtnTestEmail_Click;
            panelContent.Controls.Add(btnTestEmail);

            btnSave = new Button
            {
                Text = "💾  Save Settings",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                BackColor = Color.FromArgb(27, 94, 32),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Location = new Point(260, y),
                Size = new Size(220, 42),
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.FlatAppearance.MouseOverBackColor = Color.FromArgb(46, 125, 50);
            btnSave.Click += BtnSave_Click;
            panelContent.Controls.Add(btnSave);
            y += 50;

            // Status label
            lblStatus = new Label
            {
                Text = "",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(27, 94, 32),
                Location = new Point(20, y),
                Size = new Size(460, 25),
                TextAlign = ContentAlignment.MiddleCenter
            };
            panelContent.Controls.Add(lblStatus);

            this.Controls.Add(panelContent);
            this.Controls.Add(panelHeader);

            this.ResumeLayout(false);
        }

        private Label CreateSectionLabel(string text, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 8, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32),
                Location = new Point(20, y),
                AutoSize = true
            };
        }

        private Label CreateFieldLabel(string text, int y)
        {
            return new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 90),
                Location = new Point(20, y),
                AutoSize = true
            };
        }

        private TextBox CreateTextBox(int y, string placeholder)
        {
            return new TextBox
            {
                Location = new Point(20, y),
                Size = new Size(460, 30),
                Font = new Font("Segoe UI", 11),
                BorderStyle = BorderStyle.FixedSingle,
                BackColor = Color.White,
                PlaceholderText = placeholder
            };
        }

        private void LoadSettings()
        {
            try
            {
                var repo = new EmailSettingsRepository();
                var settings = repo.GetSettings();

                if (settings != null)
                {
                    txtApiKey.Text = settings.ResendApiKey;
                    txtOwnerEmail.Text = settings.OwnerEmail;
                    txtOwnerName.Text = settings.OwnerName;
                    chkNotifyLogin.Checked = settings.NotifyOnLogin;
                    chkNotifyLowStock.Checked = settings.NotifyOnLowStock;
                    chkEnabled.Checked = settings.IsEnabled;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load email settings: {ex.Message}");
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            if (chkEnabled.Checked)
            {
                if (string.IsNullOrWhiteSpace(txtApiKey.Text) ||
                    string.IsNullOrWhiteSpace(txtOwnerEmail.Text))
                {
                    MessageBox.Show("Please fill in the API Key and Owner Email.",
                        "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            try
            {
                var settings = new EmailSettings
                {
                    ResendApiKey = txtApiKey.Text.Trim(),
                    OwnerEmail = txtOwnerEmail.Text.Trim(),
                    OwnerName = string.IsNullOrWhiteSpace(txtOwnerName.Text) ? "Owner" : txtOwnerName.Text.Trim(),
                    NotifyOnLogin = chkNotifyLogin.Checked,
                    NotifyOnLowStock = chkNotifyLowStock.Checked,
                    IsEnabled = chkEnabled.Checked
                };

                var repo = new EmailSettingsRepository();
                repo.SaveSettings(settings);

                lblStatus.ForeColor = Color.FromArgb(27, 94, 32);
                lblStatus.Text = "✅ Settings saved successfully!";

                var auditRepo = new AuditLogRepository();
                auditRepo.Log(SessionManager.CurrentUser?.Id ?? 1, "Email Settings",
                    $"Email notification settings updated. Enabled: {settings.IsEnabled}");
            }
            catch (Exception ex)
            {
                lblStatus.ForeColor = Color.FromArgb(220, 50, 50);
                lblStatus.Text = $"❌ Error: {ex.Message}";
            }
        }

        private async void BtnTestEmail_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtApiKey.Text) ||
                string.IsNullOrWhiteSpace(txtOwnerEmail.Text))
            {
                MessageBox.Show("Please fill in the API Key and Owner Email before testing.",
                    "Missing Information", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnTestEmail.Enabled = false;
            btnTestEmail.Text = "⏳  Sending...";
            lblStatus.Text = "Sending test email...";
            lblStatus.ForeColor = Color.FromArgb(25, 118, 210);

            var ownerName = string.IsNullOrWhiteSpace(txtOwnerName.Text) ? "Owner" : txtOwnerName.Text.Trim();

            var (success, message) = await EmailService.SendTestEmailAsync(
                txtApiKey.Text.Trim(),
                txtOwnerEmail.Text.Trim(),
                ownerName);

            if (success)
            {
                lblStatus.ForeColor = Color.FromArgb(27, 94, 32);
                lblStatus.Text = $"✅ {message}";
            }
            else
            {
                lblStatus.ForeColor = Color.FromArgb(220, 50, 50);
                lblStatus.Text = $"❌ {message}";
            }

            btnTestEmail.Enabled = true;
            btnTestEmail.Text = "📧  Send Test Email";
        }
    }
}
