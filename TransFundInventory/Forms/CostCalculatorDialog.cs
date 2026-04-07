using System;
using System.Drawing;
using System.Windows.Forms;

namespace TransFundInventory.Forms
{
    public class CostCalculatorDialog : Form
    {
        private NumericUpDown nudTotalCost = null!;
        private NumericUpDown nudServings = null!;
        private NumericUpDown nudSellingPrice = null!;
        private Label lblCostPerServing = null!;
        private Label lblProfitPerServing = null!;
        private Label lblProfitPercent = null!;
        private Panel panelResult = null!;
        private Button btnApply = null!;

        public decimal CalculatedCost { get; private set; }

        public CostCalculatorDialog(decimal currentSellingPrice = 0)
        {
            InitializeComponent();
            if (currentSellingPrice > 0)
                nudSellingPrice.Value = currentSellingPrice;
        }

        private void InitializeComponent()
        {
            this.Text = "🧮 Profit Calculator";
            this.Size = new Size(420, 520);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            int leftPad = 25;
            int inputWidth = 340;

            // ── Title ──
            var lblTitle = new Label
            {
                Text = "🧮 Profit Calculator",
                Font = new Font("Segoe UI", 16, FontStyle.Bold),
                ForeColor = Color.FromArgb(27, 94, 32),
                Location = new Point(leftPad, 15),
                AutoSize = true
            };

            var lblSubtitle = new Label
            {
                Text = "I-fill up kung box/pack/kilo ang puhunan mo, para ma-compute magkano sa isa.",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(120, 130, 140),
                Location = new Point(leftPad + 2, 45),
                Size = new Size(360, 20)
            };

            // ── Step 1: Total Puhunan ──
            var lbl1 = CreateStepLabel("① Magkano ang puhunan sa BUONG box / pack?", leftPad, 80);

            nudTotalCost = new NumericUpDown
            {
                Location = new Point(leftPad, 102),
                Size = new Size(inputWidth, 32),
                Font = new Font("Segoe UI", 12),
                Maximum = 9999999,
                DecimalPlaces = 2,
                ThousandsSeparator = true
            };
            nudTotalCost.Controls[0].Visible = false;
            nudTotalCost.ValueChanged += (s, e) => CalculateResult();

            // ── Step 2: Number of Servings ──
            var lbl2 = CreateStepLabel("② Ilang PIRASO ang laman ng isang box / pack?", leftPad, 148);

            nudServings = new NumericUpDown
            {
                Location = new Point(leftPad, 170),
                Size = new Size(inputWidth, 32),
                Font = new Font("Segoe UI", 12),
                Minimum = 1,
                Maximum = 99999,
                Value = 1,
                DecimalPlaces = 0
            };
            nudServings.Controls[0].Visible = false;
            nudServings.ValueChanged += (s, e) => CalculateResult();

            // ── Step 3: Selling Price ──
            var lbl3 = CreateStepLabel("③ Magkano ang bentahan mo sa bawat ISANG piraso?", leftPad, 216);

            nudSellingPrice = new NumericUpDown
            {
                Location = new Point(leftPad, 238),
                Size = new Size(inputWidth, 32),
                Font = new Font("Segoe UI", 12),
                Maximum = 9999999,
                DecimalPlaces = 2,
                ThousandsSeparator = true
            };
            nudSellingPrice.Controls[0].Visible = false;
            nudSellingPrice.ValueChanged += (s, e) => CalculateResult();

            // ── Results Panel ──
            panelResult = new Panel
            {
                Location = new Point(leftPad, 290),
                Size = new Size(inputWidth, 110),
                BackColor = Color.FromArgb(245, 248, 250),
                Padding = new Padding(15, 12, 15, 12)
            };

            // Accent bar
            var accentBar = new Panel
            {
                Dock = DockStyle.Left,
                Width = 4,
                BackColor = Color.FromArgb(27, 94, 32)
            };

            lblCostPerServing = new Label
            {
                Text = "💰 Puhunan ng bawat isa:  ₱0.00",
                Font = new Font("Segoe UI", 10, FontStyle.Bold),
                ForeColor = Color.FromArgb(231, 76, 60),
                Location = new Point(20, 12),
                AutoSize = true
            };

            lblProfitPerServing = new Label
            {
                Text = "📈 Kita mo sa bawat isa:  ₱0.00",
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                ForeColor = Color.FromArgb(39, 174, 96),
                Location = new Point(20, 42),
                AutoSize = true
            };

            lblProfitPercent = new Label
            {
                Text = "📊 Profit margin:  0%",
                Font = new Font("Segoe UI", 9),
                ForeColor = Color.FromArgb(100, 110, 130),
                Location = new Point(20, 72),
                AutoSize = true
            };

            panelResult.Controls.Add(lblProfitPercent);
            panelResult.Controls.Add(lblProfitPerServing);
            panelResult.Controls.Add(lblCostPerServing);
            panelResult.Controls.Add(accentBar);

            // ── Apply Button ──
            btnApply = new Button
            {
                Text = "✅ Apply Puhunan",
                Location = new Point(leftPad, 415),
                Size = new Size(inputWidth, 45),
                Font = new Font("Segoe UI", 11, FontStyle.Bold),
                BackColor = Color.FromArgb(27, 94, 32),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnApply.FlatAppearance.BorderSize = 0;
            btnApply.Click += (s, e) =>
            {
                CalculateResult();
                this.DialogResult = DialogResult.OK;
                this.Close();
            };

            this.Controls.AddRange(new Control[] {
                lblTitle, lblSubtitle,
                lbl1, nudTotalCost,
                lbl2, nudServings,
                lbl3, nudSellingPrice,
                panelResult,
                btnApply
            });
        }

        private Label CreateStepLabel(string text, int x, int y)
        {
            var lbl = new Label
            {
                Text = text,
                Font = new Font("Segoe UI", 9, FontStyle.Bold),
                ForeColor = Color.FromArgb(50, 60, 80),
                Location = new Point(x, y),
                AutoSize = true
            };
            return lbl;
        }

        private void CalculateResult()
        {
            decimal totalCost = nudTotalCost.Value;
            decimal servings = nudServings.Value;
            decimal sellingPrice = nudSellingPrice.Value;

            if (servings <= 0) servings = 1;

            // Cost per serving = total cost / number of servings
            CalculatedCost = Math.Round(totalCost / servings, 2);
            lblCostPerServing.Text = $"💰 Puhunan ng bawat isa:  ₱{CalculatedCost:N2}";

            // Profit per serving
            decimal profit = sellingPrice - CalculatedCost;
            lblProfitPerServing.Text = $"📈 Kita mo sa bawat isa:  ₱{profit:N2}";
            lblProfitPerServing.ForeColor = profit < 0
                ? Color.FromArgb(231, 76, 60)
                : Color.FromArgb(39, 174, 96);

            // Profit margin %
            if (sellingPrice > 0)
            {
                decimal margin = (profit / sellingPrice) * 100;
                lblProfitPercent.Text = $"📊 Profit margin:  {margin:N1}%";
                lblProfitPercent.ForeColor = margin < 0
                    ? Color.FromArgb(231, 76, 60)
                    : Color.FromArgb(100, 110, 130);
            }
            else
            {
                lblProfitPercent.Text = "📊 Profit margin:  —";
            }

            // Update accent bar color based on profit
            var accentBar = panelResult.Controls[panelResult.Controls.Count - 1];
            accentBar.BackColor = profit < 0
                ? Color.FromArgb(231, 76, 60)
                : Color.FromArgb(39, 174, 96);
        }
    }
}
