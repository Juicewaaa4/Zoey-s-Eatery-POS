using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TransFundInventory.Models;

namespace TransFundInventory.Helpers
{
    public static class ExportHelper
    {
        static ExportHelper()
        {
            QuestPDF.Settings.License = LicenseType.Community;
        }

        // ============ EXCEL EXPORTS ============

        public static void ExportToExcel<T>(List<T> data, string sheetName, string filePath)
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(sheetName);

            // Get properties
            var properties = typeof(T).GetProperties();

            // Headers
            for (int i = 0; i < properties.Length; i++)
            {
                var cell = worksheet.Cell(1, i + 1);
                cell.Value = properties[i].Name;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.FromArgb(27, 94, 32);
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Data rows
            for (int row = 0; row < data.Count; row++)
            {
                for (int col = 0; col < properties.Length; col++)
                {
                    var value = properties[col].GetValue(data[row]);
                    var cell = worksheet.Cell(row + 2, col + 1);
                    if (value is decimal d)
                        cell.Value = (double)d;
                    else if (value is int n)
                        cell.Value = n;
                    else
                        cell.Value = value?.ToString() ?? "";
                }
            }

            worksheet.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        // ============ PDF EXPORTS ============

        public static void ExportProductsToPdf(List<Product> products, string filePath)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(headerCol =>
                    {
                        headerCol.Item().Row(row =>
                        {
                            var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "zoeyslogo.png");
                            if (File.Exists(logoPath))
                            {
                                row.ConstantItem(70).Height(50).Image(logoPath);
                                row.ConstantItem(15);
                            }
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Zoey's Billiard House").Bold().FontSize(20)
                                    .FontColor(QuestPDF.Helpers.Colors.Green.Darken3);
                                col.Item().Text("Product Inventory Report").FontSize(12);
                                col.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(9)
                                    .FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                            });
                        });
                        headerCol.Item().PaddingTop(10).PaddingBottom(10).LineHorizontal(1)
                            .LineColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
                    });

                    page.Content().Table(table =>
                    {
                        bool isEatery = SessionManager.CurrentSection == "Eatery";

                        table.ColumnsDefinition(cols =>
                        {
                            if (!isEatery) cols.RelativeColumn(1.5f); // SKU
                            cols.RelativeColumn(3);    // Name
                            cols.RelativeColumn(2);    // Category
                            cols.RelativeColumn(1.5f); // Price
                            cols.RelativeColumn(1.5f); // Cost
                            cols.RelativeColumn(1);    // Qty
                            if (!isEatery) cols.RelativeColumn(1);    // Unit
                            cols.RelativeColumn(1.2f); // Status
                        });

                        // Header
                        table.Header(header =>
                        {
                            var headers = isEatery 
                                ? new[] { "Item", "Category", "Price", "Cost", "Qty", "Status" }
                                : new[] { "SKU", "Name", "Category", "Price", "Cost", "Qty", "Unit", "Status" };

                            foreach (var h in headers)
                            {
                                header.Cell().Background(QuestPDF.Helpers.Colors.Green.Darken3)
                                    .Padding(5).Text(h).FontColor(QuestPDF.Helpers.Colors.White).Bold();
                            }
                        });

                        foreach (var p in products)
                        {
                            var bgColor = p.Quantity <= p.MinStockLevel
                                ? QuestPDF.Helpers.Colors.Red.Lighten5
                                : QuestPDF.Helpers.Colors.White;

                            if (!isEatery) table.Cell().Background(bgColor).BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(p.SKU);
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(p.Name);
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(p.CategoryName);
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text($"₱{p.Price:N2}");
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text($"₱{p.CostPrice:N2}");
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(p.Quantity.ToString());
                            if (!isEatery) table.Cell().Background(bgColor).BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(p.Unit);
                            table.Cell().Background(bgColor).BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4)
                                .Text(p.Quantity <= p.MinStockLevel ? "⚠ LOW" : "OK")
                                .FontColor(p.Quantity <= p.MinStockLevel ? QuestPDF.Helpers.Colors.Red.Medium : QuestPDF.Helpers.Colors.Green.Medium);
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            }).GeneratePdf(filePath);
        }

        public static void ExportTransactionsToPdf(List<StockTransaction> transactions, string filePath)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(headerCol =>
                    {
                        headerCol.Item().Row(row =>
                        {
                            var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "zoeyslogo.png");
                            if (File.Exists(logoPath))
                            {
                                row.ConstantItem(70).Height(50).Image(logoPath);
                                row.ConstantItem(15);
                            }
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Zoey's Billiard House").Bold().FontSize(20)
                                    .FontColor(QuestPDF.Helpers.Colors.Green.Darken3);
                                col.Item().Text("Transaction History Report").FontSize(12);
                                col.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(9)
                                    .FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                            });
                        });
                        headerCol.Item().PaddingTop(10).PaddingBottom(10).LineHorizontal(1)
                            .LineColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(2);    // Date
                            cols.RelativeColumn(3);    // Product
                            cols.RelativeColumn(1);    // Type
                            cols.RelativeColumn(1);    // Qty
                            cols.RelativeColumn(3);    // Notes
                            cols.RelativeColumn(2);    // User
                        });

                        table.Header(header =>
                        {
                            foreach (var h in new[] { "Date", "Product", "Type", "Qty", "Notes", "User" })
                            {
                                header.Cell().Background(QuestPDF.Helpers.Colors.Green.Darken3)
                                    .Padding(5).Text(h).FontColor(QuestPDF.Helpers.Colors.White).Bold();
                            }
                        });

                        foreach (var t in transactions)
                        {
                            table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(t.TransactionDate);
                            table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(t.ProductName);
                            table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4)
                                .Text(t.Type).FontColor(t.Type == "IN" ? QuestPDF.Helpers.Colors.Green.Medium : QuestPDF.Helpers.Colors.Red.Medium);
                            table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(t.Quantity.ToString());
                            table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(t.Notes);
                            table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(t.UserName);
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            }).GeneratePdf(filePath);
        }

        public static void ExportStockReceiptPdf(StockTransaction transaction, string filePath)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(25);

                    page.Header().Column(col =>
                    {
                        var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "zoeyslogo.png");
                        if (File.Exists(logoPath))
                        {
                            col.Item().AlignCenter().Height(50).Image(logoPath);
                        }
                        col.Item().AlignCenter().Text("Zoey's Billiard House").Bold().FontSize(18)
                            .FontColor(QuestPDF.Helpers.Colors.Green.Darken3);
                        col.Item().AlignCenter().Text("Stock Transaction Receipt").FontSize(11);
                        col.Item().PaddingBottom(10).LineHorizontal(1);
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().PaddingBottom(5).Text($"Date: {transaction.TransactionDate}").FontSize(10);
                        col.Item().PaddingBottom(5).Text($"Type: {transaction.Type}").FontSize(10).Bold();
                        col.Item().PaddingBottom(5).Text($"Product: {transaction.ProductName}").FontSize(10);
                        col.Item().PaddingBottom(5).Text($"Quantity: {transaction.Quantity}").FontSize(10);
                        col.Item().PaddingBottom(5).Text($"Processed by: {transaction.UserName}").FontSize(10);
                        if (!string.IsNullOrEmpty(transaction.Notes))
                            col.Item().PaddingBottom(5).Text($"Notes: {transaction.Notes}").FontSize(10);
                        col.Item().PaddingTop(20).LineHorizontal(1);
                        col.Item().PaddingTop(5).AlignCenter().Text("This is a system-generated receipt.").FontSize(7)
                            .FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                    });
                });
            }).GeneratePdf(filePath);
        }

        public static void ExportSalesReceiptToPdf(SalesTransaction sale, List<SalesItem> items, string filePath)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A5);
                    page.Margin(25);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(col =>
                    {
                        var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "zoeyslogo.png");
                        if (File.Exists(logoPath))
                        {
                            col.Item().AlignCenter().Height(50).Image(logoPath);
                        }
                        col.Item().AlignCenter().Text("Zoey's Billiard House").Bold().FontSize(18)
                            .FontColor(QuestPDF.Helpers.Colors.Green.Darken3);
                        col.Item().AlignCenter().Text("Sales Invoice / Receipt").FontSize(11);
                        col.Item().PaddingBottom(10).LineHorizontal(1);
                    });

                    page.Content().Column(col =>
                    {
                        col.Item().PaddingBottom(5).Text($"Receipt #: {sale.Id:D6}").FontSize(10).Bold();
                        col.Item().PaddingBottom(5).Text($"Date: {sale.TransactionDate}").FontSize(10);
                        if (!string.IsNullOrEmpty(sale.CustomerName))
                        {
                            col.Item().PaddingBottom(5).Text($"Customer: {sale.CustomerName}").FontSize(10);
                        }
                        col.Item().PaddingBottom(15).Text($"Cashier: {SessionManager.CurrentUser?.FullName ?? "Cashier"}").FontSize(10);

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3); // Item desc
                                cols.RelativeColumn(1); // Qty
                                cols.RelativeColumn(2); // Price
                                cols.RelativeColumn(2); // Subtotal
                            });

                            table.Header(header =>
                            {
                                header.Cell().Text("Item").Bold();
                                header.Cell().AlignRight().Text("Qty").Bold();
                                header.Cell().AlignRight().Text("Price").Bold();
                                header.Cell().AlignRight().Text("Subtotal").Bold();
                            });

                            foreach (var item in items)
                            {
                                table.Cell().Text(item.ProductName);
                                table.Cell().AlignRight().Text(item.Quantity.ToString());
                                table.Cell().AlignRight().Text($"P{item.PriceAtSale:N2}");
                                table.Cell().AlignRight().Text($"P{item.Subtotal:N2}");
                            }
                        });

                        col.Item().PaddingTop(15).LineHorizontal(1);

                        col.Item().Row(row =>
                        {
                            row.RelativeItem().AlignRight().Text("Total Amount Due:").Bold().FontSize(12);
                            row.ConstantItem(100).AlignRight().Text($"P{sale.TotalAmount:N2}").Bold().FontSize(12);
                        });
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().AlignRight().Text("Cash Tendered:");
                            row.ConstantItem(100).AlignRight().Text($"P{sale.CashTendered:N2}");
                        });
                        col.Item().Row(row =>
                        {
                            row.RelativeItem().AlignRight().Text("Change:").Bold();
                            row.ConstantItem(100).AlignRight().Text($"P{sale.ChangeAmount:N2}").Bold();
                        });

                        col.Item().PaddingTop(20).AlignCenter().Text("Thank you for your business!").Italic();
                    });

                });
            }).GeneratePdf(filePath);
        }

        public static void ExportAuditLogsToPdf(List<AuditLog> logs, string filePath)
        {
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4.Landscape());
                    page.Margin(30);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(headerCol =>
                    {
                        headerCol.Item().Row(row =>
                        {
                            var logoPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Images", "zoeyslogo.png");
                            if (File.Exists(logoPath))
                            {
                                row.ConstantItem(70).Height(50).Image(logoPath);
                                row.ConstantItem(15);
                            }
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text("Zoey's Billiard House").Bold().FontSize(20)
                                    .FontColor(QuestPDF.Helpers.Colors.Green.Darken3);
                                col.Item().Text("Activity Log Report").FontSize(12);
                                col.Item().Text($"Generated: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(9)
                                    .FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                            });
                        });
                        headerCol.Item().PaddingTop(10).PaddingBottom(10).LineHorizontal(1)
                            .LineColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
                    });

                    page.Content().Table(table =>
                    {
                        table.ColumnsDefinition(cols =>
                        {
                            cols.RelativeColumn(1.5f); // Date
                            cols.RelativeColumn(2);    // User
                            cols.RelativeColumn(1.5f); // Action
                            cols.RelativeColumn(4);    // Details
                        });

                        table.Header(header =>
                        {
                            foreach (var h in new[] { "Date", "User", "Action", "Details" })
                            {
                                header.Cell().Background(QuestPDF.Helpers.Colors.Green.Darken3)
                                    .Padding(5).Text(h).FontColor(QuestPDF.Helpers.Colors.White).Bold();
                            }
                        });

                        foreach (var l in logs)
                        {
                            table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(l.Timestamp);
                            table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(l.UserName);
                            table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(l.Action)
                                .FontColor(l.Action == "Login" || l.Action == "Logout" ? QuestPDF.Helpers.Colors.Grey.Darken2 : QuestPDF.Helpers.Colors.Blue.Medium);
                            table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(l.Details);
                        }
                    });

                    page.Footer().AlignCenter().Text(x =>
                    {
                        x.Span("Page ");
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            }).GeneratePdf(filePath);
        }

        // Show save dialog and return the selected path (null if cancelled)
        public static string? ShowSaveDialog(string filter, string defaultName)
        {
            using var dialog = new SaveFileDialog
            {
                Filter = filter,
                FileName = defaultName,
                Title = "Export File"
            };
            return dialog.ShowDialog() == DialogResult.OK ? dialog.FileName : null;
        }
    }
}
