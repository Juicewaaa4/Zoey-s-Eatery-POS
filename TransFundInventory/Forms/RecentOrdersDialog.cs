using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using TransFundInventory.Data;
using TransFundInventory.Models;
using TransFundInventory.Helpers;

namespace TransFundInventory.Forms
{
    public class RecentOrdersDialog : Form
    {
        private DataGridView dgvOrders = null!;
        private readonly SalesRepository _salesRepo = new();

        public RecentOrdersDialog()
        {
            InitializeComponent();
            LoadRecentOrders();
        }

        private void InitializeComponent()
        {
            this.Text = "Recent Orders - Zoeys POS";
            this.Size = new Size(800, 500);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.BackColor = Color.White;

            var pnlHeader = new Panel { Dock = DockStyle.Top, Height = 50, BackColor = Color.FromArgb(27, 94, 32), Padding = new Padding(10) };
            var lblTitle = new Label { Text = "📋 Recent Transactions", Font = new Font("Segoe UI", 14, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
            pnlHeader.Controls.Add(lblTitle);

            dgvOrders = new DataGridView
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
                Font = new Font("Segoe UI", 10),
                GridColor = Color.FromArgb(240, 240, 240)
            };
            dgvOrders.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(240, 240, 240);
            dgvOrders.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            dgvOrders.EnableHeadersVisualStyles = false;

            dgvOrders.CellContentClick += DgvOrders_CellContentClick;

            this.Controls.Add(dgvOrders);
            this.Controls.Add(pnlHeader);
        }

        private void LoadRecentOrders()
        {
            var orders = _salesRepo.GetRecentTransactions(30);
            dgvOrders.DataSource = orders.Select(o => new
            {
                o.Id,
                Date = DateTime.Parse(o.TransactionDate).ToString("yyyy-MM-dd HH:mm"),
                OrderNumber = o.OrderNumber,
                Total = o.TotalAmount,
                Cashier = o.UserName,
                Status = o.IsCancelled ? "❌ CANCELLED" : "✅ ACTIVE"
            }).ToList();

            dgvOrders.Columns["Id"].Visible = false;
            dgvOrders.Columns["Total"].DefaultCellStyle.Format = "₱#,##0.00";
            
            if (!dgvOrders.Columns.Contains("VoidButton"))
            {
                var voidCol = new DataGridViewButtonColumn
                {
                    Name = "VoidButton",
                    HeaderText = "Action",
                    Text = "VOID",
                    UseColumnTextForButtonValue = true,
                    Width = 80,
                    FlatStyle = FlatStyle.Flat
                };
                voidCol.DefaultCellStyle.BackColor = Color.FromArgb(231, 76, 60);
                voidCol.DefaultCellStyle.ForeColor = Color.White;
                dgvOrders.Columns.Add(voidCol);
            }
        }

        private void DgvOrders_CellContentClick(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            if (dgvOrders.Columns[e.ColumnIndex].Name == "VoidButton")
            {
                int transactionId = (int)dgvOrders.Rows[e.RowIndex].Cells["Id"].Value;
                string status = dgvOrders.Rows[e.RowIndex].Cells["Status"].Value.ToString()!;

                if (status.Contains("CANCELLED"))
                {
                    MessageBox.Show("Order is already cancelled.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                if (MessageBox.Show("Are you sure you want to VOID this order?\n\nItems will be returned to stock.", "Confirm Void", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                {
                    try
                    {
                        _salesRepo.VoidTransaction(transactionId, SessionManager.CurrentUser!.Id);
                        MessageBox.Show("Order voided successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        LoadRecentOrders();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Error voiding order: " + ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }
    }
}
