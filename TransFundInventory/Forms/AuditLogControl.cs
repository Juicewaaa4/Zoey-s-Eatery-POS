using TransFundInventory.Data;
using TransFundInventory.Helpers;

namespace TransFundInventory.Forms
{
    public class AuditLogControl : UserControl
    {
        private DataGridView dgvLogs = null!;
        private DateTimePicker dtpFrom = null!;
        private DateTimePicker dtpTo = null!;
        private ComboBox cmbAction = null!;
        private readonly AuditLogRepository _auditRepo = new();

        public AuditLogControl()
        {
            InitializeComponent();
            LoadActions();
            LoadLogs();
        }

        private void InitializeComponent()
        {
            this.BackColor = Color.FromArgb(240, 242, 245);

            var lblTitle = new Label
            {
                Text = "Activity Log",
                Font = new Font("Segoe UI", 20, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32),
                Dock = DockStyle.Top,
                Height = 50
            };

            // Filter panel
            var panelFilter = new Panel
            {
                Dock = DockStyle.Top,
                Height = 55,
                BackColor = Color.White,
                Padding = new Padding(15, 10, 15, 10)
            };

            var lblFrom = new Label
            {
                Text = "From:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 90),
                Location = new Point(15, 18),
                AutoSize = true
            };

            dtpFrom = new DateTimePicker
            {
                Location = new Point(60, 14),
                Size = new Size(180, 28),
                Font = new Font("Segoe UI", 9),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now.AddMonths(-1)
            };
            dtpFrom.ValueChanged += (s, e) => LoadLogs();

            var lblTo = new Label
            {
                Text = "To:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 90),
                Location = new Point(255, 18),
                AutoSize = true
            };

            dtpTo = new DateTimePicker
            {
                Location = new Point(280, 14),
                Size = new Size(180, 28),
                Font = new Font("Segoe UI", 9),
                Format = DateTimePickerFormat.Short,
                Value = DateTime.Now
            };
            dtpTo.ValueChanged += (s, e) => LoadLogs();

            var lblAction = new Label
            {
                Text = "Action:",
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(60, 70, 90),
                Location = new Point(475, 18),
                AutoSize = true
            };

            cmbAction = new ComboBox
            {
                Location = new Point(530, 14),
                Size = new Size(160, 28),
                Font = new Font("Segoe UI", 9),
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cmbAction.SelectedIndexChanged += (s, e) => LoadLogs();

            var btnExportExcel = new Button
            {
                Text = "📊 Export Excel",
                Location = new Point(705, 10),
                Size = new Size(110, 32),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(39, 174, 96),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnExportExcel.FlatAppearance.BorderSize = 0;
            btnExportExcel.Click += BtnExport_Click;

            var btnExportPdf = new Button
            {
                Text = "📄 Export PDF",
                Location = new Point(825, 10),
                Size = new Size(110, 32),
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                BackColor = Color.FromArgb(231, 76, 60),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnExportPdf.FlatAppearance.BorderSize = 0;
            btnExportPdf.Click += BtnExportPdf_Click;

            panelFilter.Controls.AddRange(new Control[] { lblFrom, dtpFrom, lblTo, dtpTo, lblAction, cmbAction, btnExportExcel, btnExportPdf });

            // DataGridView
            var panelGrid = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.White,
                Padding = new Padding(1)
            };

            dgvLogs = new DataGridView
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
                Font = new Font("Segoe UI", 9)
            };
            dgvLogs.DefaultCellStyle.SelectionBackColor = Color.FromArgb(220, 235, 250);
            dgvLogs.DefaultCellStyle.SelectionForeColor = Color.Black;
            dgvLogs.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(27, 94, 32);
            dgvLogs.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dgvLogs.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            dgvLogs.EnableHeadersVisualStyles = false;
            dgvLogs.ColumnHeadersHeight = 35;
            dgvLogs.RowTemplate.Height = 28;

            panelGrid.Controls.Add(dgvLogs);

            this.Controls.Add(panelGrid);
            this.Controls.Add(panelFilter);
            this.Controls.Add(lblTitle);
        }

        private void LoadActions()
        {
            var actions = _auditRepo.GetDistinctActions();
            cmbAction.Items.AddRange(actions.ToArray());
            cmbAction.SelectedIndex = 0;
        }

        private void LoadLogs()
        {
            var actionFilter = cmbAction.SelectedItem?.ToString();
            var logs = _auditRepo.GetAll(dtpFrom.Value, dtpTo.Value, actionFilter);
            dgvLogs.DataSource = logs.Select(l => new
            {
                l.Timestamp,
                User = l.UserName,
                l.Action,
                l.Details
            }).ToList();

            if (dgvLogs.Columns.Contains("Details"))
            {
                dgvLogs.Columns["Details"].DefaultCellStyle.WrapMode = DataGridViewTriState.True;
                dgvLogs.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.AllCellsExceptHeaders;
                dgvLogs.Columns["Details"].FillWeight = 200; // Give it more space compared to other columns
            }
        }

        private void BtnExport_Click(object? sender, EventArgs e)
        {
            var path = ExportHelper.ShowSaveDialog("Excel Files|*.xlsx", "AuditLog_Report.xlsx");
            if (path != null)
            {
                var logs = _auditRepo.GetAll(dtpFrom.Value, dtpTo.Value, cmbAction.SelectedItem?.ToString());
                var data = logs.Select(l => new { l.Timestamp, User = l.UserName, l.Action, l.Details }).ToList();
                ExportHelper.ExportToExcel(data, "Activity Log", path);
                MessageBox.Show("Export complete!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void BtnExportPdf_Click(object? sender, EventArgs e)
        {
            var path = ExportHelper.ShowSaveDialog("PDF Files|*.pdf", "AuditLog_Report.pdf");
            if (path != null)
            {
                try
                {
                    var logs = _auditRepo.GetAll(dtpFrom.Value, dtpTo.Value, cmbAction.SelectedItem?.ToString());
                    ExportHelper.ExportAuditLogsToPdf(logs, path);
                    MessageBox.Show("PDF generated successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error generating PDF: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
