using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using TransFundInventory.Models;
using TransFundInventory.Data;
using System.Globalization;

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
            WriteProfessionalWorksheet(worksheet, data, sheetName);
            workbook.SaveAs(filePath);
        }

        public static void AddSheetToExcel<T>(string filePath, string sheetName, List<T> data)
        {
            using var workbook = new XLWorkbook(filePath);
            var existing = workbook.Worksheets.FirstOrDefault(w => w.Name.Equals(sheetName, StringComparison.OrdinalIgnoreCase));
            existing?.Delete();

            var worksheet = workbook.Worksheets.Add(sheetName);
            WriteProfessionalWorksheet(worksheet, data, sheetName);
            workbook.SaveAs(filePath);
        }

        private static void WriteProfessionalWorksheet<T>(IXLWorksheet worksheet, List<T> data, string sheetName)
        {
            var properties = typeof(T).GetProperties();

            int columnCount = Math.Max(1, properties.Length);
            int titleRow = 1;
            int metaRow = 2;
            int headerRow = 4;
            int dataStartRow = 5;

            // Report heading block
            var titleRange = worksheet.Range(titleRow, 1, titleRow, columnCount);
            titleRange.Merge();
            titleRange.Value = $"Zoey's Billiard House - {sheetName}";
            titleRange.Style.Font.Bold = true;
            titleRange.Style.Font.FontSize = 16;
            titleRange.Style.Font.FontColor = XLColor.FromArgb(27, 94, 32);
            titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            var metaRange = worksheet.Range(metaRow, 1, metaRow, columnCount);
            metaRange.Merge();
            metaRange.Value = $"Generated: {DateTime.Now:MMMM dd, yyyy hh:mm tt}";
            metaRange.Style.Font.FontSize = 10;
            metaRange.Style.Font.Italic = true;
            metaRange.Style.Font.FontColor = XLColor.FromArgb(110, 120, 130);
            metaRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            // Column headers
            for (int i = 0; i < properties.Length; i++)
            {
                var headerCell = worksheet.Cell(headerRow, i + 1);
                headerCell.Value = ToReadableHeader(properties[i].Name);
                headerCell.Style.Font.Bold = true;
                headerCell.Style.Font.FontColor = XLColor.White;
                headerCell.Style.Fill.BackgroundColor = XLColor.FromArgb(27, 94, 32);
                headerCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                headerCell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                headerCell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            worksheet.Row(headerRow).Height = 24;

            // Data rows
            for (int row = 0; row < data.Count; row++)
            {
                int currentRow = dataStartRow + row;
                bool isAlt = row % 2 == 1;
                var rowData = data[row];

                for (int col = 0; col < properties.Length; col++)
                {
                    var prop = properties[col];
                    var type = GetNonNullableType(prop.PropertyType);
                    var value = prop.GetValue(rowData);
                    var cell = worksheet.Cell(currentRow, col + 1);

                    if (value is null)
                    {
                        cell.Value = string.Empty;
                    }
                    else if (type == typeof(DateTime))
                    {
                        cell.Value = Convert.ToDateTime(value);
                        cell.Style.DateFormat.Format = "yyyy-mm-dd hh:mm AM/PM";
                    }
                    else if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
                    {
                        cell.Value = Convert.ToDouble(value);
                    }
                    else if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
                    {
                        cell.Value = Convert.ToInt64(value);
                    }
                    else if (type == typeof(bool))
                    {
                        cell.Value = (bool)value ? "Yes" : "No";
                    }
                    else
                    {
                        cell.Value = value.ToString() ?? string.Empty;
                    }

                    cell.Style.Fill.BackgroundColor = isAlt ? XLColor.FromArgb(245, 250, 245) : XLColor.White;
                    cell.Style.Border.BottomBorder = XLBorderStyleValues.Hair;
                    cell.Style.Border.BottomBorderColor = XLColor.FromArgb(220, 225, 230);
                    cell.Style.Font.FontSize = 10;
                }
            }

            // Column formatting by name/type
            for (int col = 0; col < properties.Length; col++)
            {
                var prop = properties[col];
                var type = GetNonNullableType(prop.PropertyType);
                var headerName = prop.Name;
                var column = worksheet.Column(col + 1);

                if (LooksLikeCurrencyColumn(headerName))
                    column.Style.NumberFormat.Format = "_([$₱-469]* #,##0.00_);_([$₱-469]* (#,##0.00);_([$₱-469]* \"-\"??_);_(@_)";
                else if (type == typeof(decimal) || type == typeof(double) || type == typeof(float))
                    column.Style.NumberFormat.Format = "#,##0.00";
                else if (type == typeof(int) || type == typeof(long) || type == typeof(short) || type == typeof(byte))
                    column.Style.NumberFormat.Format = "#,##0";
                else if (type == typeof(DateTime))
                    column.Style.DateFormat.Format = "yyyy-mm-dd hh:mm AM/PM";

                if (LooksLikeCountColumn(headerName))
                    column.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Total row for numeric columns
            if (data.Count > 0 && properties.Length > 0)
            {
                int totalRow = dataStartRow + data.Count;
                worksheet.Cell(totalRow, 1).Value = "TOTAL";
                worksheet.Cell(totalRow, 1).Style.Font.Bold = true;
                worksheet.Cell(totalRow, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                for (int col = 0; col < properties.Length; col++)
                {
                    var prop = properties[col];
                    var type = GetNonNullableType(prop.PropertyType);
                    if (!IsNumericType(type)) continue;

                    var letter = worksheet.Column(col + 1).ColumnLetter();
                    worksheet.Cell(totalRow, col + 1).FormulaA1 = $"SUM({letter}{dataStartRow}:{letter}{totalRow - 1})";
                    worksheet.Cell(totalRow, col + 1).Style.Font.Bold = true;
                }

                var totalRange = worksheet.Range(totalRow, 1, totalRow, properties.Length);
                totalRange.Style.Fill.BackgroundColor = XLColor.FromArgb(232, 240, 232);
                totalRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                totalRange.Style.Border.TopBorderColor = XLColor.FromArgb(190, 200, 190);
            }

            // Sheet usability
            if (properties.Length > 0)
            {
                int dataEndRow = data.Count > 0 ? dataStartRow + data.Count : dataStartRow;
                worksheet.Range(headerRow, 1, dataEndRow, properties.Length).SetAutoFilter();
            }

            worksheet.SheetView.FreezeRows(headerRow);
            worksheet.Columns(1, columnCount).AdjustToContents();
        }

        private static string ToReadableHeader(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var chars = new List<char>(value.Length + 8);
            chars.Add(value[0]);
            for (int i = 1; i < value.Length; i++)
            {
                var current = value[i];
                var prev = value[i - 1];
                if ((char.IsUpper(current) && char.IsLower(prev)) || current == '_')
                {
                    if (current != '_')
                        chars.Add(' ');
                }
                if (current != '_')
                    chars.Add(current);
            }
            return new string(chars.ToArray());
        }

        private static Type GetNonNullableType(Type type) => Nullable.GetUnderlyingType(type) ?? type;

        private static bool IsNumericType(Type type)
        {
            type = GetNonNullableType(type);
            return type == typeof(decimal)
                || type == typeof(double)
                || type == typeof(float)
                || type == typeof(int)
                || type == typeof(long)
                || type == typeof(short)
                || type == typeof(byte);
        }

        private static bool LooksLikeCurrencyColumn(string name)
        {
            var n = name.ToLowerInvariant();
            return n.Contains("price")
                || n.Contains("cost")
                || n.Contains("amount")
                || n.Contains("value")
                || n.Contains("income")
                || n.Contains("total")
                || n.Contains("gross")
                || n.Contains("net")
                || n.Contains("profit")
                || n.Contains("subtotal");
        }

        private static bool LooksLikeCountColumn(string name)
        {
            var n = name.ToLowerInvariant();
            return n.Contains("qty")
                || n.Contains("quantity")
                || n.Contains("count")
                || n.Contains("stock")
                || n.Contains("items")
                || n.Contains("number");
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

        // ============ PROFESSIONAL SALES HISTORY EXCEL ============

        public static void ExportSalesHistoryExcel(List<SalesTransactionDetail> details, DateTime fromDate, DateTime toDate, string filePath)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Sales History");

            // ── Company Header ──
            ws.Cell("A1").Value = "Zoey's Billiard House";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 18;
            ws.Cell("A1").Style.Font.FontColor = XLColor.FromArgb(27, 94, 32);
            ws.Range("A1:I1").Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            ws.Cell("A2").Value = $"Transaction History Report — {SessionManager.CurrentSection} Section";
            ws.Cell("A2").Style.Font.FontSize = 12;
            ws.Cell("A2").Style.Font.FontColor = XLColor.FromArgb(80, 90, 110);
            ws.Range("A2:I2").Merge();

            ws.Cell("A3").Value = $"Period: {fromDate:MMMM dd, yyyy} — {toDate:MMMM dd, yyyy}";
            ws.Cell("A3").Style.Font.FontSize = 10;
            ws.Cell("A3").Style.Font.Italic = true;
            ws.Cell("A3").Style.Font.FontColor = XLColor.FromArgb(120, 130, 140);
            ws.Range("A3:I3").Merge();

            ws.Cell("A4").Value = $"Generated: {DateTime.Now:MMMM dd, yyyy hh:mm tt}";
            ws.Cell("A4").Style.Font.FontSize = 9;
            ws.Cell("A4").Style.Font.FontColor = XLColor.FromArgb(150, 160, 170);
            ws.Range("A4:I4").Merge();

            // Divider row
            int headerRow = 6;

            // ── Column Headers ──
            var headers = new[] { "Date", "Time", "Order #", "Product", "Qty Sold", "Unit Price", "Subtotal", "Cashier" };
            var headerBg = XLColor.FromArgb(27, 94, 32);
            var headerFont = XLColor.White;

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 10;
                cell.Style.Font.FontColor = headerFont;
                cell.Style.Fill.BackgroundColor = headerBg;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.LeftBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.RightBorder = XLBorderStyleValues.Thin;
            }
            ws.Row(headerRow).Height = 25;

            // ── Data Rows ──
            var altRowBg = XLColor.FromArgb(245, 250, 245);
            var whiteBg = XLColor.White;

            var sortedDetails = details.OrderBy(d => DateTime.Parse(d.Date)).ToList();

            for (int r = 0; r < sortedDetails.Count; r++)
            {
                var d = sortedDetails[r];
                int row = headerRow + 1 + r;
                bool isAlt = r % 2 == 1;

                ws.Cell(row, 1).Value = DateTime.Parse(d.Date).ToString("yyyy-MM-dd");
                ws.Cell(row, 2).Value = DateTime.Parse(d.Date).ToString("hh:mm tt");
                ws.Cell(row, 3).Value = d.OrderNumber;
                ws.Cell(row, 4).Value = d.ProductName;
                ws.Cell(row, 5).Value = d.QtySold;
                ws.Cell(row, 6).Value = d.UnitPrice;
                ws.Cell(row, 7).Value = d.Subtotal;
                ws.Cell(row, 8).Value = d.Cashier;

                // Formatting
                ws.Cell(row, 5).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 6).Style.NumberFormat.Format = "_([$₱-469]* #,##0.00_);_([$₱-469]* (#,##0.00);_([$₱-469]* \"-\"??_);_(@_)";
                ws.Cell(row, 7).Style.NumberFormat.Format = "_([$₱-469]* #,##0.00_);_([$₱-469]* (#,##0.00);_([$₱-469]* \"-\"??_);_(@_)";

                // Alternating row background + borders
                for (int c = 1; c <= 8; c++)
                {
                    var cell = ws.Cell(row, c);
                    cell.Style.Fill.BackgroundColor = isAlt ? altRowBg : whiteBg;
                    cell.Style.Border.BottomBorder = XLBorderStyleValues.Hair;
                    cell.Style.Border.BottomBorderColor = XLColor.FromArgb(220, 225, 230);
                    cell.Style.Border.LeftBorder = XLBorderStyleValues.Hair;
                    cell.Style.Border.LeftBorderColor = XLColor.FromArgb(220, 225, 230);
                    cell.Style.Border.RightBorder = XLBorderStyleValues.Hair;
                    cell.Style.Border.RightBorderColor = XLColor.FromArgb(220, 225, 230);
                    cell.Style.Font.FontSize = 10;
                }
            }

            // ── Summary Section ──
            int summaryStart = headerRow + details.Count + 3;
            var summaryHeaderBg = XLColor.FromArgb(52, 73, 94);

            // Summary header
            ws.Range(ws.Cell(summaryStart, 1), ws.Cell(summaryStart, 4)).Merge();
            ws.Cell(summaryStart, 1).Value = "SALES SUMMARY";
            ws.Cell(summaryStart, 1).Style.Font.Bold = true;
            ws.Cell(summaryStart, 1).Style.Font.FontSize = 12;
            ws.Cell(summaryStart, 1).Style.Font.FontColor = XLColor.White;
            ws.Cell(summaryStart, 1).Style.Fill.BackgroundColor = summaryHeaderBg;
            ws.Cell(summaryStart, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            for (int c = 1; c <= 4; c++)
            {
                ws.Cell(summaryStart, c).Style.Fill.BackgroundColor = summaryHeaderBg;
                ws.Cell(summaryStart, c).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }

            int totalQty = sortedDetails.Sum(d => d.QtySold);
            double grossSales = sortedDetails.Sum(d => d.Subtotal);

            var summaryItems = new (string Label, string Value)[]
            {
                ("Total Items Sold", totalQty.ToString("N0")),
                ("Gross Sales", $"₱{grossSales:N2}")
            };

            for (int i = 0; i < summaryItems.Length; i++)
            {
                int row = summaryStart + 1 + i;
                ws.Range(ws.Cell(row, 1), ws.Cell(row, 2)).Merge();
                ws.Cell(row, 1).Value = summaryItems[i].Label;
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Font.FontSize = 11;
                ws.Cell(row, 1).Style.Fill.BackgroundColor = XLColor.FromArgb(240, 242, 245);
                ws.Cell(row, 2).Style.Fill.BackgroundColor = XLColor.FromArgb(240, 242, 245);

                ws.Range(ws.Cell(row, 3), ws.Cell(row, 4)).Merge();
                ws.Cell(row, 3).Value = summaryItems[i].Value;
                ws.Cell(row, 3).Style.Font.Bold = true;
                ws.Cell(row, 3).Style.Font.FontSize = 11;
                ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

                if (i == 1) // Gross Sales
                {
                    ws.Cell(row, 3).Style.Font.FontColor = XLColor.FromArgb(39, 174, 96);
                    ws.Cell(row, 3).Style.Font.FontSize = 13;
                }

                for (int c = 1; c <= 4; c++)
                {
                    ws.Cell(row, c).Style.Border.OutsideBorder = XLBorderStyleValues.Hair;
                    ws.Cell(row, c).Style.Border.OutsideBorderColor = XLColor.FromArgb(200, 205, 210);
                }
            }

            // ── Column Widths ──
            ws.Column(1).Width = 20; // Date
            ws.Column(2).Width = 15; // Time
            ws.Column(3).Width = 15; // Order #
            ws.Column(4).Width = 25; // Product
            ws.Column(5).Width = 10; // Qty
            ws.Column(6).Width = 14; // Price
            ws.Column(7).Width = 14; // Subtotal
            ws.Column(8).Width = 18; // Cashier

            // Print settings
            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            ws.PageSetup.FitToPages(1, 0);

            workbook.SaveAs(filePath);
        }

        // ============ PROFESSIONAL STOCK HISTORY EXCEL ============

        public static void ExportStockHistoryExcel(List<StockTransaction> transactions, DateTime fromDate, DateTime toDate, string filePath)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Stock History");

            // ── Company Header ──
            ws.Cell("A1").Value = "Zoey's Billiard House";
            ws.Cell("A1").Style.Font.Bold = true;
            ws.Cell("A1").Style.Font.FontSize = 18;
            ws.Cell("A1").Style.Font.FontColor = XLColor.FromArgb(27, 94, 32);
            ws.Range("A1:F1").Merge().Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            ws.Cell("A2").Value = $"Stock Movement Report — {SessionManager.CurrentSection} Section";
            ws.Cell("A2").Style.Font.FontSize = 12;
            ws.Cell("A2").Style.Font.FontColor = XLColor.FromArgb(80, 90, 110);
            ws.Range("A2:F2").Merge();

            ws.Cell("A3").Value = $"Period: {fromDate:MMMM dd, yyyy} — {toDate:MMMM dd, yyyy}";
            ws.Cell("A3").Style.Font.FontSize = 10;
            ws.Cell("A3").Style.Font.Italic = true;
            ws.Cell("A3").Style.Font.FontColor = XLColor.FromArgb(120, 130, 140);
            ws.Range("A3:F3").Merge();

            ws.Cell("A4").Value = $"Generated: {DateTime.Now:MMMM dd, yyyy hh:mm tt}";
            ws.Cell("A4").Style.Font.FontSize = 9;
            ws.Cell("A4").Style.Font.FontColor = XLColor.FromArgb(150, 160, 170);
            ws.Range("A4:F4").Merge();

            int headerRow = 6;

            // ── Column Headers ──
            var headers = new[] { "Date & Time", "Product", "Type", "Quantity", "Notes / Reason", "Processed By" };
            var headerBg = XLColor.FromArgb(27, 94, 32);

            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontSize = 10;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = headerBg;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            }
            ws.Row(headerRow).Height = 25;

            // ── Data Rows ──
            var altRowBg = XLColor.FromArgb(245, 250, 245);

            for (int r = 0; r < transactions.Count; r++)
            {
                var t = transactions[r];
                int row = headerRow + 1 + r;
                bool isAlt = r % 2 == 1;

                ws.Cell(row, 1).Value = t.TransactionDate;
                ws.Cell(row, 2).Value = t.ProductName;
                ws.Cell(row, 3).Value = t.Type;
                ws.Cell(row, 4).Value = t.Quantity;
                ws.Cell(row, 5).Value = t.Notes;
                ws.Cell(row, 6).Value = t.UserName;

                // Type color
                ws.Cell(row, 3).Style.Font.Bold = true;
                ws.Cell(row, 3).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                ws.Cell(row, 3).Style.Font.FontColor = t.Type == "IN"
                    ? XLColor.FromArgb(39, 174, 96) : XLColor.FromArgb(231, 76, 60);

                ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                for (int c = 1; c <= 6; c++)
                {
                    var cell = ws.Cell(row, c);
                    cell.Style.Fill.BackgroundColor = isAlt ? altRowBg : XLColor.White;
                    cell.Style.Border.BottomBorder = XLBorderStyleValues.Hair;
                    cell.Style.Border.BottomBorderColor = XLColor.FromArgb(220, 225, 230);
                    cell.Style.Font.FontSize = 10;
                }
            }

            // ── Summary ──
            int summaryRow = headerRow + transactions.Count + 3;
            int totalIn = transactions.Where(t => t.Type == "IN").Sum(t => t.Quantity);
            int totalOut = transactions.Where(t => t.Type == "OUT").Sum(t => t.Quantity);

            ws.Range(ws.Cell(summaryRow, 1), ws.Cell(summaryRow, 3)).Merge();
            ws.Cell(summaryRow, 1).Value = "STOCK MOVEMENT SUMMARY";
            ws.Cell(summaryRow, 1).Style.Font.Bold = true;
            ws.Cell(summaryRow, 1).Style.Font.FontSize = 12;
            ws.Cell(summaryRow, 1).Style.Font.FontColor = XLColor.White;
            for (int c = 1; c <= 3; c++)
                ws.Cell(summaryRow, c).Style.Fill.BackgroundColor = XLColor.FromArgb(52, 73, 94);

            ws.Cell(summaryRow + 1, 1).Value = "Total Stock IN";
            ws.Cell(summaryRow + 1, 1).Style.Font.Bold = true;
            ws.Cell(summaryRow + 1, 2).Value = totalIn;
            ws.Cell(summaryRow + 1, 2).Style.Font.Bold = true;
            ws.Cell(summaryRow + 1, 2).Style.Font.FontColor = XLColor.FromArgb(39, 174, 96);

            ws.Cell(summaryRow + 2, 1).Value = "Total Stock OUT";
            ws.Cell(summaryRow + 2, 1).Style.Font.Bold = true;
            ws.Cell(summaryRow + 2, 2).Value = totalOut;
            ws.Cell(summaryRow + 2, 2).Style.Font.Bold = true;
            ws.Cell(summaryRow + 2, 2).Style.Font.FontColor = XLColor.FromArgb(231, 76, 60);

            ws.Cell(summaryRow + 3, 1).Value = "Total Transactions";
            ws.Cell(summaryRow + 3, 1).Style.Font.Bold = true;
            ws.Cell(summaryRow + 3, 2).Value = transactions.Count;
            ws.Cell(summaryRow + 3, 2).Style.Font.Bold = true;

            for (int i = 0; i < 4; i++)
            {
                for (int c = 1; c <= 3; c++)
                {
                    if (i > 0)
                        ws.Cell(summaryRow + i, c).Style.Fill.BackgroundColor = XLColor.FromArgb(240, 242, 245);
                    ws.Cell(summaryRow + i, c).Style.Border.OutsideBorder = XLBorderStyleValues.Hair;
                }
            }

            // ── Column Widths ──
            ws.Column(1).Width = 20;
            ws.Column(2).Width = 25;
            ws.Column(3).Width = 10;
            ws.Column(4).Width = 10;
            ws.Column(5).Width = 30;
            ws.Column(6).Width = 18;

            ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
            workbook.SaveAs(filePath);
        }

        // ============ SALES HISTORY PDF ============

        public static void ExportSalesHistoryPdf(List<SalesTransactionDetail> details, DateTime fromDate, DateTime toDate, string filePath)
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
                                col.Item().Text("Transaction History Report (Sales)").FontSize(12);
                                col.Item().Text($"Period: {fromDate:MMM dd, yyyy} — {toDate:MMM dd, yyyy}  |  Generated: {DateTime.Now:yyyy-MM-dd HH:mm}").FontSize(9)
                                    .FontColor(QuestPDF.Helpers.Colors.Grey.Medium);
                            });
                        });
                        headerCol.Item().PaddingTop(10).PaddingBottom(10).LineHorizontal(1)
                            .LineColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
                    });

                    page.Content().Column(content =>
                    {
                        // Summary bar
                        content.Item().Row(row =>
                        {
                            row.RelativeItem().Background(QuestPDF.Helpers.Colors.Green.Lighten5).Padding(8).Column(col =>
                            {
                                col.Item().Text($"Items Sold: {details.Sum(d => d.QtySold):N0}").Bold();
                            });
                            row.ConstantItem(10);
                            row.RelativeItem().Background(QuestPDF.Helpers.Colors.Green.Lighten5).Padding(8).Column(col =>
                            {
                                col.Item().Text($"Gross Sales: ₱{details.Sum(d => d.Subtotal):N2}").Bold()
                                    .FontColor(QuestPDF.Helpers.Colors.Green.Darken3);
                            });
                            row.ConstantItem(10);
                            row.RelativeItem().Background(QuestPDF.Helpers.Colors.Blue.Lighten5).Padding(8).Column(col =>
                            {
                                col.Item().Text($"Net Profit: ₱{details.Sum(d => d.Profit):N2}").Bold()
                                    .FontColor(QuestPDF.Helpers.Colors.Blue.Darken3);
                            });
                        });

                        content.Item().PaddingTop(10);

                        // Table
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(2);    // Date
                                cols.RelativeColumn(1.2f); // Order
                                cols.RelativeColumn(2.5f); // Product
                                cols.RelativeColumn(0.8f); // Qty
                                cols.RelativeColumn(1.2f); // Price
                                cols.RelativeColumn(1.2f); // Subtotal
                                cols.RelativeColumn(1.2f); // Profit
                                cols.RelativeColumn(1);    // Payment
                                cols.RelativeColumn(1.5f); // Cashier
                            });

                            table.Header(header =>
                            {
                                foreach (var h in new[] { "Date", "Order #", "Product", "Qty", "Price", "Subtotal", "Profit", "Payment", "Cashier" })
                                {
                                    header.Cell().Background(QuestPDF.Helpers.Colors.Green.Darken3)
                                        .Padding(5).Text(h).FontColor(QuestPDF.Helpers.Colors.White).Bold();
                                }
                            });

                            foreach (var d in details)
                            {
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(d.Date);
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(d.OrderNumber);
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(d.ProductName);
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(d.QtySold.ToString());
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text($"₱{d.UnitPrice:N2}");
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text($"₱{d.Subtotal:N2}");
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4)
                                    .Text($"₱{d.Profit:N2}").FontColor(d.Profit >= 0 ? QuestPDF.Helpers.Colors.Green.Medium : QuestPDF.Helpers.Colors.Red.Medium);
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(d.PaymentMethod);
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(d.Cashier);
                            }
                        });
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

        public static void ExportShiftSalesExcel(List<ShiftSalesDetail> details, DateTime shiftDate, string morningStartStr, string nightShiftStartStr, string filePath, string shiftType = "Both")
        {
            TimeSpan nightStart = TimeSpan.Parse(nightShiftStartStr);
            TimeSpan morningStart = TimeSpan.Parse(morningStartStr);
            var (morningDetails, nightDetails) = SplitShiftDetails(details, shiftDate, morningStart, nightStart);
            var cancelledDetails = new SalesRepository().GetCancelledOrders(shiftDate, shiftDate.AddDays(1));
            ExportShiftSalesExcel(morningDetails, nightDetails, cancelledDetails, filePath, shiftType, shiftDate);
        }

        public static void ExportShiftSalesExcel(List<ShiftSalesDetail> morningDetails, List<ShiftSalesDetail> nightDetails, List<ShiftSalesDetail> cancelledDetails, string filePath, string shiftType = "Both", DateTime? shiftDate = null)
        {
            using var workbook = new XLWorkbook();

            if (shiftType == "Both")
            {
                // Single sheet with Morning on top and Night below
                var ws = workbook.Worksheets.Add("Shift Sales Report");
                
                // --- Add Top Headers ---
                if (shiftDate.HasValue)
                {
                    ws.Cell(1, 4).Value = $"DATE : {shiftDate.Value:MMMM dd, yyyy}".ToUpper();
                    ws.Cell(1, 4).Style.Font.Bold = true;
                    ws.Range(1, 4, 1, 6).Merge();
                    ws.Range(1, 4, 1, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                
                string sectionName = SessionManager.CurrentSection.ToUpper();
                ws.Cell(2, 1).Value = sectionName;
                ws.Cell(2, 1).Style.Font.Bold = true;

                int nextStartRow = WriteShiftSection(ws, "Morning Shift", morningDetails, XLColor.FromHtml("#6aa84f"), 4);
                nextStartRow += 2; // Gap between sections
                int nightEndRow = WriteShiftSection(ws, "Night Shift", nightDetails, XLColor.FromHtml("#6aa84f"), nextStartRow);
                
                if (cancelledDetails != null && cancelledDetails.Count > 0)
                {
                    int cancelRow = nightEndRow + 2;
                    WriteShiftSection(ws, "Cancelled Orders", cancelledDetails, XLColor.FromHtml("#cc0000"), cancelRow);
                }

                // Print single overall totals at the bottom of the last shift
                WriteGrandTotals(ws, nightEndRow + 1, morningDetails.Concat(nightDetails).ToList());

                // Side panels with combined category breakdown
                WriteSidePanels(ws, morningDetails, nightDetails, 10);
                ws.Columns().AdjustToContents();
            }
            else if (shiftType == "Morning")
            {
                var ws = workbook.Worksheets.Add("Morning Shift Report");
                
                // --- Add Top Headers ---
                if (shiftDate.HasValue)
                {
                    ws.Cell(1, 4).Value = $"DATE : {shiftDate.Value:MMMM dd, yyyy}".ToUpper();
                    ws.Cell(1, 4).Style.Font.Bold = true;
                    ws.Range(1, 4, 1, 6).Merge();
                    ws.Range(1, 4, 1, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                ws.Cell(2, 1).Value = SessionManager.CurrentSection.ToUpper();
                ws.Cell(2, 1).Style.Font.Bold = true;

                int endRow = WriteShiftSection(ws, "Morning Shift", morningDetails, XLColor.FromHtml("#6aa84f"), 4);
                if (cancelledDetails != null && cancelledDetails.Count > 0)
                {
                    WriteShiftSection(ws, "Cancelled Orders", cancelledDetails, XLColor.FromHtml("#cc0000"), endRow + 2);
                }
                WriteSidePanels(ws, morningDetails, new List<ShiftSalesDetail>(), 10);
                ws.Columns().AdjustToContents();
            }
            else if (shiftType == "Night")
            {
                var ws = workbook.Worksheets.Add("Night Shift Report");
                
                // --- Add Top Headers ---
                if (shiftDate.HasValue)
                {
                    ws.Cell(1, 4).Value = $"DATE : {shiftDate.Value:MMMM dd, yyyy}".ToUpper();
                    ws.Cell(1, 4).Style.Font.Bold = true;
                    ws.Range(1, 4, 1, 6).Merge();
                    ws.Range(1, 4, 1, 6).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                }
                ws.Cell(2, 1).Value = SessionManager.CurrentSection.ToUpper();
                ws.Cell(2, 1).Style.Font.Bold = true;

                int endRow = WriteShiftSection(ws, "Night Shift", nightDetails, XLColor.FromHtml("#6aa84f"), 4);
                if (cancelledDetails != null && cancelledDetails.Count > 0)
                {
                    WriteShiftSection(ws, "Cancelled Orders", cancelledDetails, XLColor.FromHtml("#cc0000"), endRow + 2);
                }
                WriteSidePanels(ws, new List<ShiftSalesDetail>(), nightDetails, 10);
                ws.Columns().AdjustToContents();
            }
            
            workbook.SaveAs(filePath);
        }
        /// <summary>
        /// Writes a shift section (header + data table + totals) starting at startRow. Returns the next available row.
        /// </summary>
        private static int WriteShiftSection(IXLWorksheet ws, string sheetName, List<ShiftSalesDetail> data, XLColor headerColor, int startRow)
        {
            // Shift title row
            ws.Cell(startRow, 1).Value = sheetName;
            var titleRange = ws.Range(startRow, 1, startRow, 6);
            titleRange.Merge();
            titleRange.Style.Font.Bold = true;
            titleRange.Style.Font.FontColor = XLColor.White;
            titleRange.Style.Font.FontSize = 14;
            titleRange.Style.Fill.BackgroundColor = headerColor;
            titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            // Column headers
            int headerRow = startRow + 1;
            var headers = new[] { "Product Sold", "SOLD", "GROSS INCOME", "TOTAL COST", "NET INCOME", "PERCENTAGE" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = headerColor;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            }

            // Enable AutoFilter on header
            ws.Range(headerRow, 1, headerRow, 6).SetAutoFilter();

            // Data rows
            int row = headerRow + 1;
            double totalDist = 0, totalGross = 0, totalNet = 0;
            double totalSold = 0;

            foreach (var d in data)
            {
                bool isAlt = (row - (headerRow + 1)) % 2 == 1;
                double cost = d.BuyingPrice * d.QtySold;
                double gross = d.SellingPrice * d.QtySold;
                double net = gross - cost;

                ws.Cell(row, 1).Value = d.ProductName;
                ws.Cell(row, 2).Value = d.QtySold;
                ws.Cell(row, 3).Value = gross; 
                ws.Cell(row, 4).Value = cost; 
                ws.Cell(row, 5).Value = net; 
                double rowPerc = gross > 0 ? net / gross : 0;
                ws.Cell(row, 6).Value = rowPerc;

                ws.Range(row, 3, row, 5).Style.NumberFormat.Format = "_([$₱-469]* #,##0.00_);_([$₱-469]* (#,##0.00);_([$₱-469]* \"-\"??_);_(@_)";
                ws.Cell(row, 6).Style.NumberFormat.Format = "0%";
                ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                for (int c = 1; c <= 6; c++)
                {
                    var cell = ws.Cell(row, c);
                    cell.Style.Fill.BackgroundColor = isAlt ? XLColor.FromArgb(246, 250, 246) : XLColor.White;
                    cell.Style.Border.BottomBorder = XLBorderStyleValues.Hair;
                    cell.Style.Border.BottomBorderColor = XLColor.FromArgb(220, 225, 230);
                    cell.Style.Font.FontSize = 10;
                }

                totalSold += d.QtySold;
                totalDist += cost;
                totalGross += gross;
                totalNet += net;
                row++;
            }

            // ── Per-Shift Total Row ──
            if (data.Count > 0)
            {
                ws.Cell(row, 1).Value = "TOTAL";
                ws.Cell(row, 1).Style.Font.Bold = true;
                ws.Cell(row, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                ws.Cell(row, 2).Value = totalSold;
                ws.Cell(row, 2).Style.Font.Bold = true;
                ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                ws.Cell(row, 3).Value = totalGross;
                ws.Cell(row, 4).Value = totalDist;
                ws.Cell(row, 5).Value = totalNet;
                double perc = totalGross > 0 ? totalNet / totalGross : 0;
                ws.Cell(row, 6).Value = perc;

                ws.Range(row, 3, row, 5).Style.NumberFormat.Format = "_([$₱-469]* #,##0.00_);_([$₱-469]* (#,##0.00);_([$₱-469]* \"-\"??_);_(@_)";
                ws.Cell(row, 6).Style.NumberFormat.Format = "0%";

                var totalRowRange = ws.Range(row, 1, row, 6);
                totalRowRange.Style.Font.Bold = true;
                totalRowRange.Style.Fill.BackgroundColor = XLColor.FromArgb(232, 242, 232);
                totalRowRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
                totalRowRange.Style.Border.TopBorderColor = XLColor.FromArgb(27, 94, 32);
                row++;
            }

            return row;
        }

        private static void WriteGrandTotals(IXLWorksheet ws, int row, List<ShiftSalesDetail> data)
        {
            if (data.Count == 0) return;

            double totalCost = data.Sum(d => d.BuyingPrice * d.QtySold);
            double totalGross = data.Sum(d => d.SellingPrice * d.QtySold);
            double totalNet = totalGross - totalCost;
            double totalPerc = totalGross > 0 ? totalNet / totalGross : 0;
            double totalSold = data.Sum(d => d.QtySold);

            // Totals Header Row
            ws.Cell(row, 2).Value = "TOTAL SOLD";
            ws.Cell(row, 3).Value = "GROSS INCOME";
            ws.Cell(row, 4).Value = "COST OF SALES";
            ws.Cell(row, 5).Value = "NET INCOME";
            ws.Cell(row, 6).Value = "PERCENTAGE";
            
            var totalsHeaderRange = ws.Range(row, 2, row, 6);
            totalsHeaderRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#ed7d31"); // Orange header
            totalsHeaderRange.Style.Font.Bold = true;
            totalsHeaderRange.Style.Font.FontColor = XLColor.White;
            totalsHeaderRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            row++;

            // Totals Value Row
            ws.Cell(row, 2).Value = totalSold;
            ws.Cell(row, 3).Value = totalGross;
            ws.Cell(row, 4).Value = totalCost;
            ws.Cell(row, 5).Value = totalNet;
            ws.Cell(row, 6).Value = totalPerc;

            var totalsValueRange = ws.Range(row, 1, row, 6);
            totalsValueRange.Style.Font.Bold = true;
            ws.Range(row, 2, row, 6).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
            ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            
            ws.Range(row, 3, row, 5).Style.NumberFormat.Format = "_([$₱-469]* #,##0.00_);_([$₱-469]* (#,##0.00);_([$₱-469]* \"-\"??_);_(@_)";
            ws.Cell(row, 6).Style.NumberFormat.Format = "0%";
        }

        /// <summary>
        /// Writes side panels for both shifts combined: Category breakdown, manual entries, distributor price
        /// </summary>
        private static void WriteSidePanels(IXLWorksheet ws, List<ShiftSalesDetail> morningData, List<ShiftSalesDetail> nightData, int sideCol)
        {
            int r = 4; // Align with the start of the first table

            // ── Overall Breakdown ──
            ws.Cell(r, sideCol).Value = "Breakdown";
            ws.Range(r, sideCol, r, sideCol + 1).Merge();
            ws.Range(r, sideCol, r, sideCol + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#4a86e8");
            ws.Range(r, sideCol, r, sideCol + 1).Style.Font.FontColor = XLColor.White;
            ws.Range(r, sideCol, r, sideCol + 1).Style.Font.Bold = true;
            ws.Range(r, sideCol, r, sideCol + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            r++;

            var allData = morningData.Concat(nightData).ToList();
            var allCats = allData.GroupBy(d => d.CategoryName).Select(g => new { Cat = g.Key, Total = g.Sum(x => x.GrossIncome) }).OrderByDescending(x => x.Total).ToList();
            double grandTotal = 0;
            foreach (var c in allCats)
            {
                ws.Cell(r, sideCol).Value = c.Cat;
                ws.Cell(r, sideCol + 1).Value = c.Total;
                ws.Cell(r, sideCol + 1).Style.NumberFormat.Format = "_([$₱-469]* #,##0.00_);_([$₱-469]* (#,##0.00);_([$₱-469]* \"-\"??_);_(@_)";
                grandTotal += c.Total;
                r++;
            }
            ws.Cell(r, sideCol).Value = "TOTAL";
            ws.Cell(r, sideCol).Style.Font.Bold = true;
            ws.Cell(r, sideCol + 1).Value = grandTotal;
            ws.Cell(r, sideCol + 1).Style.Font.Bold = true;
            ws.Cell(r, sideCol + 1).Style.NumberFormat.Format = "_([$₱-469]* #,##0.00_);_([$₱-469]* (#,##0.00);_([$₱-469]* \"-\"??_);_(@_)";
            ws.Range(r, sideCol, r, sideCol + 1).Style.Fill.BackgroundColor = XLColor.FromArgb(232, 240, 248);
            var salesTotalCell = ws.Cell(r, sideCol + 1).Address;
            r += 3; // extra gap before Additional Income

            // ── Manual Entry: Additional Income ──
            if (SessionManager.CurrentSection != "Eatery")
            {
                ws.Cell(r, sideCol).Value = "Additional Income";
                ws.Range(r, sideCol, r, sideCol + 1).Merge();
                ws.Range(r, sideCol, r, sideCol + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#783f04");
                ws.Range(r, sideCol, r, sideCol + 1).Style.Font.FontColor = XLColor.White;
                ws.Range(r, sideCol, r, sideCol + 1).Style.Font.Bold = true;
                ws.Range(r, sideCol, r, sideCol + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                r++;

                var manualItems = new[] { "Tako Rent", "Kubo Rent", "Corkage Fee", "Videoke" };
                int manualStartRow = r;
                foreach (var item in manualItems)
                {
                    ws.Cell(r, sideCol).Value = item;
                    ws.Cell(r, sideCol).Style.Font.Bold = true;
                    ws.Cell(r, sideCol + 1).Style.NumberFormat.Format = "_([$₱-469]* #,##0.00_);_([$₱-469]* (#,##0.00);_([$₱-469]* \"-\"??_);_(@_)";
                    ws.Cell(r, sideCol + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#fff2cc");
                    ws.Cell(r, sideCol + 1).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin);
                    ws.Cell(r, sideCol + 1).Style.Border.OutsideBorderColor = XLColor.FromHtml("#bf9000");
                    r++;
                }
                int manualEndRow = r - 1;

                // Additional Total (sum of manual entries)
                string manualSumRange = $"{ws.Cell(manualStartRow, sideCol + 1).Address}:{ws.Cell(manualEndRow, sideCol + 1).Address}";
                ws.Cell(r, sideCol).Value = "Additional Total";
                ws.Cell(r, sideCol).Style.Font.Bold = true;
                ws.Cell(r, sideCol + 1).FormulaA1 = $"SUM({manualSumRange})";
                ws.Cell(r, sideCol + 1).Style.Font.Bold = true;
                ws.Cell(r, sideCol + 1).Style.NumberFormat.Format = "_([$₱-469]* #,##0.00_);_([$₱-469]* (#,##0.00);_([$₱-469]* \"-\"??_);_(@_)";
                ws.Range(r, sideCol, r, sideCol + 1).Style.Fill.BackgroundColor = XLColor.FromArgb(232, 240, 248);
                var additionalTotalCell = ws.Cell(r, sideCol + 1).Address;
                r += 2;

                // ── Grand Total = Sales Total + Additional Total ──
                ws.Cell(r, sideCol).Value = "GRAND TOTAL";
                ws.Cell(r, sideCol).Style.Font.Bold = true;
                ws.Cell(r, sideCol).Style.Font.FontSize = 12;
                ws.Cell(r, sideCol + 1).FormulaA1 = $"{salesTotalCell}+{additionalTotalCell}";
                ws.Cell(r, sideCol + 1).Style.Font.Bold = true;
                ws.Cell(r, sideCol + 1).Style.Font.FontSize = 12;
                ws.Cell(r, sideCol + 1).Style.Font.FontColor = XLColor.FromHtml("#27ae60");
                ws.Cell(r, sideCol + 1).Style.NumberFormat.Format = "_([$₱-469]* #,##0.00_);_([$₱-469]* (#,##0.00);_([$₱-469]* \"-\"??_);_(@_)";
                ws.Range(r, sideCol, r, sideCol + 1).Style.Fill.BackgroundColor = XLColor.FromArgb(209, 231, 209);
                ws.Range(r, sideCol, r, sideCol + 1).Style.Border.SetOutsideBorder(XLBorderStyleValues.Medium);
                ws.Range(r, sideCol, r, sideCol + 1).Style.Border.OutsideBorderColor = XLColor.FromHtml("#27ae60");
            }
        }



        private static (List<ShiftSalesDetail> Morning, List<ShiftSalesDetail> Night) SplitShiftDetails(
            List<ShiftSalesDetail> details,
            DateTime shiftDate,
            TimeSpan morningStart,
            TimeSpan nightStart)
        {
            var morningRaw = new List<ShiftSalesDetail>();
            var nightRaw = new List<ShiftSalesDetail>();
            var nextDay = shiftDate.AddDays(1).Date;
            var day = shiftDate.Date;

            foreach (var d in details)
            {
                if (!TryParseTransactionTime(d.TransactionTime, out var txTime))
                    continue;

                var tod = txTime.TimeOfDay;

                if (txTime.Date == day)
                {
                    if (tod >= morningStart && tod < nightStart)
                        morningRaw.Add(d);
                    else if (tod >= nightStart)
                        nightRaw.Add(d);
                }
                else if (txTime.Date == nextDay && tod < morningStart)
                {
                    nightRaw.Add(d);
                }
            }

            // Aggregate: combine same product into one row with totaled qty
            return (AggregateProducts(morningRaw), AggregateProducts(nightRaw));
        }

        /// <summary>
        /// Combines duplicate product rows into single rows with summed quantities.
        /// </summary>
        private static List<ShiftSalesDetail> AggregateProducts(List<ShiftSalesDetail> raw)
        {
            return raw
                .GroupBy(d => new { d.ProductName, d.CategoryName, d.BuyingPrice, d.SellingPrice })
                .Select(g => new ShiftSalesDetail
                {
                    ProductName = g.Key.ProductName,
                    CategoryName = g.Key.CategoryName,
                    BuyingPrice = g.Key.BuyingPrice,
                    SellingPrice = g.Key.SellingPrice,
                    QtySold = g.Sum(x => x.QtySold),
                    TransactionTime = g.First().TransactionTime // Keep first timestamp for reference
                })
                .OrderBy(d => d.ProductName)
                .ToList();
        }

        private static bool TryParseTransactionTime(string raw, out DateTime parsed)
        {
            if (DateTime.TryParseExact(raw,
                    new[] { "yyyy-MM-dd HH:mm:ss", "yyyy-MM-dd H:mm:ss", "yyyy-MM-ddTHH:mm:ss", "yyyy-MM-ddTHH:mm:ss.fff" },
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out parsed))
            {
                return true;
            }

            return DateTime.TryParse(raw, out parsed);
        }

        private static void CreateShiftSheet(XLWorkbook workbook, string sheetName, List<ShiftSalesDetail> data, XLColor headerColor)
        {
            var ws = workbook.Worksheets.Add(sheetName);

            // Header block
            ws.Cell("A1").Value = $"Zoey's Billiard House - {sheetName} Report";
            var titleRange = ws.Range("A1:H1");
            titleRange.Merge();
            titleRange.Style.Font.Bold = true;
            titleRange.Style.Font.FontColor = XLColor.White;
            titleRange.Style.Font.FontSize = 14;
            titleRange.Style.Fill.BackgroundColor = headerColor;
            titleRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            titleRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;

            ws.Cell("A2").Value = $"Generated: {DateTime.Now:MMMM dd, yyyy hh:mm tt}";
            ws.Range("A2:H2").Merge();
            ws.Cell("A2").Style.Font.FontSize = 9;
            ws.Cell("A2").Style.Font.Italic = true;
            ws.Cell("A2").Style.Font.FontColor = XLColor.FromArgb(95, 105, 115);

            // Product headers
            int headerRow = 4;
            var headers = new[] { "Product", "Unit Cost", "Selling Price", "Qty Sold", "Total Cost", "Gross Income", "Net Income", "Margin %" };
            for (int i = 0; i < headers.Length; i++)
            {
                var cell = ws.Cell(headerRow, i + 1);
                cell.Value = headers[i];
                cell.Style.Font.Bold = true;
                cell.Style.Font.FontColor = XLColor.White;
                cell.Style.Fill.BackgroundColor = headerColor;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            }
            ws.Row(headerRow).Height = 24;

            int row = headerRow + 1;
            double totalDist = 0, totalGross = 0, totalNet = 0;
            
            foreach (var d in data)
            {
                bool isAlt = (row - (headerRow + 1)) % 2 == 1;
                ws.Cell(row, 1).Value = d.ProductName;
                ws.Cell(row, 2).Value = d.BuyingPrice;
                ws.Cell(row, 3).Value = d.SellingPrice;
                ws.Cell(row, 4).Value = d.QtySold;
                ws.Cell(row, 5).Value = d.BuyingPrice * d.QtySold; // Explicitly multiply to ensure Total Cost is correct
                ws.Cell(row, 6).Value = d.SellingPrice * d.QtySold; // Explicitly multiply
                ws.Cell(row, 7).Value = (d.SellingPrice * d.QtySold) - (d.BuyingPrice * d.QtySold); // Explicitly calculate Net Profit
                ws.Cell(row, 8).Value = d.Percentage / 100.0;
                
                // Formatting
                ws.Range(row, 2, row, 3).Style.NumberFormat.Format = "_([$₱-469]* #,##0.00_);_([$₱-469]* (#,##0.00);_([$₱-469]* \"-\"??_);_(@_)";
                ws.Range(row, 5, row, 7).Style.NumberFormat.Format = "_([$₱-469]* #,##0.00_);_([$₱-469]* (#,##0.00);_([$₱-469]* \"-\"??_);_(@_)";
                ws.Cell(row, 8).Style.NumberFormat.Format = "0%";
                ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

                for (int c = 1; c <= 8; c++)
                {
                    var cell = ws.Cell(row, c);
                    cell.Style.Fill.BackgroundColor = isAlt ? XLColor.FromArgb(246, 250, 246) : XLColor.White;
                    cell.Style.Border.BottomBorder = XLBorderStyleValues.Hair;
                    cell.Style.Border.BottomBorderColor = XLColor.FromArgb(220, 225, 230);
                    cell.Style.Font.FontSize = 10;
                }

                totalDist += d.DistributorPrice;
                totalGross += d.GrossIncome;
                totalNet += d.NetIncome;
                
                row++;
            }

            // Put Totals
            if (data.Count > 0)
            {
                ws.Cell(row, 4).Value = "TOTAL";
                ws.Cell(row, 4).Style.Font.Bold = true;
                ws.Cell(row, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                
                ws.Cell(row, 5).Value = totalDist;
                ws.Cell(row, 6).Value = totalGross;
                ws.Cell(row, 7).Value = totalNet;
                double totalPerc = totalGross > 0 ? totalNet / totalGross : 0;
                ws.Cell(row, 8).Value = totalPerc;
                
                var totalRange = ws.Range(row, 1, row, 8);
                totalRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#f4cccc"); // Light orange base
                totalRange.Style.Font.Bold = true;
                ws.Range(row, 5, row, 7).Style.NumberFormat.Format = "_([$₱-469]* #,##0.00_);_([$₱-469]* (#,##0.00);_([$₱-469]* \"-\"??_);_(@_)";
                ws.Cell(row, 8).Style.NumberFormat.Format = "0%";
                totalRange.Style.Border.TopBorder = XLBorderStyleValues.Thin;
            }

            // AUTO FILTER
            if (data.Count > 0)
            {
                ws.Range(headerRow, 1, row - 1, 8).SetAutoFilter();
            }

            ws.SheetView.FreezeRows(headerRow);

            // === SIDE PANELS ===
            int sideCol = 10; // Column J

            // Top Sellers by Qty
            var topQty = data.OrderByDescending(d => d.QtySold).Take(10).ToList();
            ws.Cell(1, sideCol).Value = "Top Sellers by Quantity";
            ws.Range(1, sideCol, 1, sideCol + 1).Merge();
            ws.Range(1, sideCol, 1, sideCol + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#c9daf8");
            ws.Range(1, sideCol, 1, sideCol + 1).Style.Font.Bold = true;
            ws.Range(1, sideCol, 1, sideCol + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            ws.Cell(2, sideCol).Value = "Product Name";
            ws.Cell(2, sideCol + 1).Value = "Qty";
            ws.Range(2, sideCol, 2, sideCol + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#d9e1f2");
            ws.Range(2, sideCol, 2, sideCol + 1).Style.Font.Bold = true;
            
            int r = 3;
            foreach (var t in topQty)
            {
                ws.Cell(r, sideCol).Value = t.ProductName;
                ws.Cell(r, sideCol + 1).Value = t.QtySold;
                ws.Cell(r, sideCol + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                r++;
            }

            // Top Sellers by Income
            r += 2;
            var topIncome = data.OrderByDescending(d => d.GrossIncome).Take(10).ToList();
            ws.Cell(r, sideCol).Value = "Top Sellers by Income";
            ws.Range(r, sideCol, r, sideCol + 1).Merge();
            ws.Range(r, sideCol, r, sideCol + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#c9daf8");
            ws.Range(r, sideCol, r, sideCol + 1).Style.Font.Bold = true;
            ws.Range(r, sideCol, r, sideCol + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            r++;
            ws.Cell(r, sideCol).Value = "Product Name";
            ws.Cell(r, sideCol + 1).Value = "Income";
            ws.Range(r, sideCol, r, sideCol + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#d9e1f2");
            ws.Range(r, sideCol, r, sideCol + 1).Style.Font.Bold = true;
            
            r++;
            foreach (var t in topIncome)
            {
                ws.Cell(r, sideCol).Value = t.ProductName;
                ws.Cell(r, sideCol + 1).Value = t.GrossIncome;
                ws.Cell(r, sideCol + 1).Style.NumberFormat.Format = "_([$₱-469]* #,##0.00_);_([$₱-469]* (#,##0.00);_([$₱-469]* \"-\"??_);_(@_)";
                r++;
            }

            // Category Breakdown
            r += 2;
            ws.Cell(r, sideCol).Value = "Category Breakdown";
            ws.Range(r, sideCol, r, sideCol + 1).Merge();
            ws.Range(r, sideCol, r, sideCol + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#4a86e8");
            ws.Range(r, sideCol, r, sideCol + 1).Style.Font.FontColor = XLColor.White;
            ws.Range(r, sideCol, r, sideCol + 1).Style.Font.Bold = true;
            ws.Range(r, sideCol, r, sideCol + 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            r++;
            var cats = data.GroupBy(d => d.CategoryName).Select(g => new { Cat = g.Key, Total = g.Sum(x => x.GrossIncome) }).ToList();
            ws.Cell(r, sideCol).Value = "Category";
            ws.Cell(r, sideCol + 1).Value = "Total";
            var catHeaderRange = ws.Range(r, sideCol, r, sideCol + 1);
            catHeaderRange.Style.Fill.BackgroundColor = XLColor.FromHtml("#d9e1f2");
            catHeaderRange.Style.Font.FontColor = XLColor.FromArgb(45, 55, 72);
            catHeaderRange.Style.Font.Bold = true;
            
            r++;
            double catTotal = 0;
            foreach (var c in cats)
            {
                ws.Cell(r, sideCol).Value = c.Cat;
                ws.Cell(r, sideCol + 1).Value = c.Total;
                ws.Cell(r, sideCol + 1).Style.NumberFormat.Format = "_([$₱-469]* #,##0.00_);_([$₱-469]* (#,##0.00);_([$₱-469]* \"-\"??_);_(@_)";
                catTotal += c.Total;
                r++;
            }
            ws.Cell(r, sideCol).Value = "TOTAL:";
            ws.Cell(r, sideCol).Style.Font.Bold = true;
            ws.Cell(r, sideCol + 1).Value = catTotal;
            ws.Cell(r, sideCol + 1).Style.Font.Bold = true;
            ws.Cell(r, sideCol + 1).Style.NumberFormat.Format = "_([$₱-469]* #,##0.00_);_([$₱-469]* (#,##0.00);_([$₱-469]* \"-\"??_);_(@_)";
            ws.Range(r, sideCol, r, sideCol + 1).Style.Fill.BackgroundColor = XLColor.FromArgb(232, 240, 248);

            ws.Columns().AdjustToContents();
        }

        public static void ExportShiftSalesPdf(List<ShiftSalesDetail> details, DateTime shiftDate, string nightShiftStartStr, string filePath)
        {
            // Simple summary export since PDF isn't as dynamic as Excel
            Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(PageSizes.A4);
                    page.Margin(1, QuestPDF.Infrastructure.Unit.Centimetre);
                    page.PageColor(QuestPDF.Helpers.Colors.White);
                    page.DefaultTextStyle(x => x.FontSize(10).FontFamily(QuestPDF.Helpers.Fonts.Arial));

                    page.Header().Element(header =>
                    {
                        header.Column(col =>
                        {
                            col.Item().Text("Zoey's Billiard House")
                                .FontColor(QuestPDF.Helpers.Colors.Green.Darken3)
                                .FontSize(20).SemiBold();
                            col.Item().Text("Shift Sales Report").FontSize(14).Italic();
                            col.Item().PaddingBottom(5).LineHorizontal(1).LineColor(QuestPDF.Helpers.Colors.Grey.Lighten2);
                            
                            col.Item().Text($"Date: {shiftDate:MMM dd, yyyy}");
                            col.Item().Text($"Night Shift Start: {nightShiftStartStr}");
                            col.Item().Text($"Generated: {DateTime.Now:MMM dd, yyyy HH:mm}");
                            col.Item().PaddingBottom(15).Text($"Printed By: {SessionManager.CurrentUser?.FullName ?? "Admin"}");
                        });
                    });

                    page.Content().Element(content =>
                    {
                        content.Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.RelativeColumn(3); // Product
                                cols.RelativeColumn(1.5f); // Category
                                cols.RelativeColumn(1); // Qty
                                cols.RelativeColumn(2); // Gross
                                cols.RelativeColumn(2); // Net
                            });

                            table.Header(header =>
                            {
                                foreach (var h in new[] { "Product", "Category", "Qty", "Gross Sales", "Net Profit" })
                                {
                                    header.Cell().Background(QuestPDF.Helpers.Colors.Green.Darken3)
                                        .Padding(5).Text(h).FontColor(QuestPDF.Helpers.Colors.White).Bold();
                                }
                            });

                            double totalGross = 0;
                            double totalNet = 0;

                            foreach (var s in details.OrderBy(d => d.ProductName))
                            {
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(s.ProductName);
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(s.CategoryName);
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text(s.QtySold.ToString());
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4).Text($"₱{s.GrossIncome:N2}");
                                table.Cell().BorderBottom(1).BorderColor(QuestPDF.Helpers.Colors.Grey.Lighten3).Padding(4)
                                    .Text($"₱{s.NetIncome:N2}").FontColor(s.NetIncome >= 0 ? QuestPDF.Helpers.Colors.Green.Medium : QuestPDF.Helpers.Colors.Red.Medium);

                                totalGross += s.GrossIncome;
                                totalNet += s.NetIncome;
                            }

                            // Footer Row
                            table.Cell().ColumnSpan(3).PaddingTop(10).AlignRight().Text("TOTAL:").Bold();
                            table.Cell().PaddingTop(10).Text($"₱{totalGross:N2}").Bold();
                            table.Cell().PaddingTop(10).Text($"₱{totalNet:N2}").Bold();
                        });
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

