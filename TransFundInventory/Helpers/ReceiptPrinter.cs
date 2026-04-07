using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using TransFundInventory.Models;

namespace TransFundInventory.Helpers
{
    public static class ReceiptPrinter
    {
        public static void Print(SalesTransaction sale, List<SalesItem> items)
        {
            using PrintDocument pd = new PrintDocument();
            
            // Standard POS printers are usually the default Windows printer when installed.
            pd.PrintController = new StandardPrintController(); // Hide printing dialog
            pd.DocumentName = sale.OrderNumber; // Used if they "Print to PDF" without a physical printer

            pd.PrintPage += (sender, e) =>
            {
                Graphics g = e.Graphics!;
                Font fontHeader = new Font("Courier New", 12, FontStyle.Bold);
                Font fontBody = new Font("Courier New", 9);
                Font fontBold = new Font("Courier New", 9, FontStyle.Bold);
                
                int y = 10;
                int startX = 0; // Thermal printers have small margins
                int lineOffset = 15;
                int offset = 0;
                
                // Assuming standard 58mm printer width ~ 200px or 80mm ~ 280px.
                int paperWidth = 280; 

                // Helper to center text
                void DrawCenterText(string text, Font f, int currY)
                {
                    SizeF textSize = g.MeasureString(text, f);
                    float x = (paperWidth - textSize.Width) / 2;
                    if (x < startX) x = startX;
                    g.DrawString(text, f, Brushes.Black, x, currY);
                }

                DrawCenterText("Zoey's Billiard House", fontHeader, y + offset);
                offset += lineOffset + 5;
                DrawCenterText("and Billiard House", fontHeader, y + offset);
                offset += 20;

                g.DrawString(new string('-', 40), fontBody, Brushes.Black, startX, y + offset);
                offset += lineOffset;

                g.DrawString($"Receipt #: {sale.OrderNumber}", fontBody, Brushes.Black, startX, y + offset);
                offset += lineOffset;
                g.DrawString($"Date     : {DateTime.Parse(sale.TransactionDate):yy-MM-dd HH:mm}", fontBody, Brushes.Black, startX, y + offset);
                offset += lineOffset;
                if (!string.IsNullOrEmpty(sale.CustomerName))
                {
                    g.DrawString($"Customer : {sale.CustomerName}", fontBody, Brushes.Black, startX, y + offset);
                    offset += lineOffset;
                }
                g.DrawString($"Cashier  : {SessionManager.CurrentUser?.FullName ?? "Cashier"}", fontBody, Brushes.Black, startX, y + offset);
                offset += lineOffset;

                g.DrawString(new string('-', 40), fontBody, Brushes.Black, startX, y + offset);
                offset += lineOffset;

                // Header for items
                g.DrawString("ITEM", fontBold, Brushes.Black, startX, y + offset);
                g.DrawString("QTY", fontBold, Brushes.Black, startX + 130, y + offset);
                g.DrawString("PRICE", fontBold, Brushes.Black, startX + 170, y + offset);
                g.DrawString("TOTAL", fontBold, Brushes.Black, startX + 220, y + offset);
                offset += lineOffset;

                foreach (var item in items)
                {
                    // Truncate name to prevent wrapping overlap
                    string name = item.ProductName;
                    if (name.Length > 15) name = name.Substring(0, 15) + ".";

                    g.DrawString(name, fontBody, Brushes.Black, startX, y + offset);
                    g.DrawString(item.Quantity.ToString(), fontBody, Brushes.Black, startX + 130, y + offset);
                    g.DrawString(item.PriceAtSale.ToString("N0"), fontBody, Brushes.Black, startX + 170, y + offset);
                    g.DrawString(item.Subtotal.ToString("N2"), fontBody, Brushes.Black, startX + 220, y + offset);
                    offset += lineOffset;
                }

                g.DrawString(new string('-', 40), fontBody, Brushes.Black, startX, y + offset);
                offset += lineOffset;

                g.DrawString("TOTAL DUE:", fontBold, Brushes.Black, startX + 100, y + offset);
                g.DrawString(sale.TotalAmount.ToString("N2"), fontBold, Brushes.Black, startX + 210, y + offset);
                offset += lineOffset;

                if (sale.PaymentMethod == "GCash")
                {
                    g.DrawString("GCASH PAID:", fontBody, Brushes.Black, startX + 100, y + offset);
                    g.DrawString(sale.TotalAmount.ToString("N2"), fontBody, Brushes.Black, startX + 210, y + offset);
                    offset += lineOffset;
                    
                    if (!string.IsNullOrEmpty(sale.ReferenceNumber))
                    {
                        g.DrawString($"GCASH OTP: {sale.ReferenceNumber}", fontBody, Brushes.Black, startX, y + offset);
                        offset += 25;
                    }
                }
                else
                {
                    g.DrawString("CASH TEND:", fontBody, Brushes.Black, startX + 100, y + offset);
                    g.DrawString(sale.CashTendered.ToString("N2"), fontBody, Brushes.Black, startX + 210, y + offset);
                    offset += lineOffset;

                    g.DrawString("CHANGE:", fontBold, Brushes.Black, startX + 100, y + offset);
                    g.DrawString(sale.ChangeAmount.ToString("N2"), fontBold, Brushes.Black, startX + 210, y + offset);
                    offset += 25;
                }

                DrawCenterText("Thank you for your business!", fontBody, y + offset);
                offset += 20;
                DrawCenterText("System Generated Receipt", fontBody, y + offset);
                offset += lineOffset;
                
                // Extra padding for printer tear-off
                offset += 50;
            };

            try
            {
                pd.Print();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Could not print the receipt to the thermal printer.\nPlease check if the printer is powered on and set as default printer in Windows.\n\nError: " + ex.Message, "Printer Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
    }
}
