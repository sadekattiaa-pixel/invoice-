using Invoice.Domain.Entities;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using InvoiceEntity = Invoice.Domain.Entities.Invoice;

namespace Invoice.Infrastructure.Exporters;

/// <summary>
/// يُولّد ملف PDF للفاتورة بالعربية باستخدام QuestPDF.
/// </summary>
public static class InvoicePdfExporter
{
    /// <summary>يُهيّئ QuestPDF للعمل مع خطوط النظام.</summary>
    static InvoicePdfExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
    }

    /// <summary>ينشئ ملف PDF للفاتورة في المسار المحدد.</summary>
    public static void Export(InvoiceEntity invoice, string filePath)
    {
        var items = invoice.Items.OrderBy(i => i.LineNo).ToList();
        var payments = invoice.Payments.OrderBy(p => p.LineNo).ToList();
        var discounts = invoice.Discounts.OrderBy(d => d.LineNo).ToList();
        var expenses = invoice.Expenses.OrderBy(e => e.LineNo).ToList();

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                // ========= الترويسة =========
                page.Header().Column(col =>
                {
                    col.Item().AlignCenter().Text("فاتورة")
                        .FontSize(20).Bold();

                    col.Item().PaddingTop(4).AlignCenter()
                        .Text($"رقم الفاتورة: {invoice.InvoiceNo}")
                        .FontSize(12);

                    if (!string.IsNullOrWhiteSpace(invoice.SupplierInvoiceNo))
                    {
                        col.Item().AlignCenter()
                            .Text($"رقم فاتورة المورد: {invoice.SupplierInvoiceNo}")
                            .FontSize(10);
                    }

                    col.Item().PaddingTop(8).Row(row =>
                    {
                        row.RelativeItem().Text($"التاريخ: {invoice.InvoiceDate:yyyy-MM-dd}");
                        row.RelativeItem().AlignRight()
                            .Text($"المورد: {invoice.Supplier?.Name ?? "—"}");
                    });
                });

                // ========= المحتوى =========
                page.Content().PaddingVertical(10).Column(col =>
                {
                    col.Spacing(8);

                    // جدول البنود
                    col.Item().Text("الأصناف").FontSize(13).Bold();

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.ConstantColumn(30);
                            columns.RelativeColumn(3);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(1.5f);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            void H(string text) =>
                                header.Cell().Background(Colors.Grey.Lighten2)
                                    .Padding(4).AlignCenter().Text(text).Bold().FontSize(10);

                            H("#");
                            H("الصنف");
                            H("الكمية");
                            H("الشراء $");
                            H("البيع د.ل");
                            H("إجمالي البيع");
                            H("الربح");
                        });

                        foreach (var item in items)
                        {
                            void C(string text) =>
                                table.Cell().BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten2)
                                    .Padding(4).AlignCenter().Text(text).FontSize(10);

                            C(item.LineNo.ToString());
                            C(item.Product?.Name ?? "—");
                            C(item.Quantity.ToString("0.##"));
                            C(item.PurchaseUsd.ToString("N2"));
                            C(item.SaleDzd.ToString("N2"));
                            C(item.TotalSale.ToString("N2"));
                            C(item.TotalProfit.ToString("N2"));
                        }
                    });

                    col.Item().PaddingTop(12).LineHorizontal(1).LineColor(Colors.Grey.Medium);

                    col.Item().PaddingTop(8).Text("الملخص").FontSize(13).Bold();

                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(3);
                        });

                        void Row(string label, string value, bool bold = false)
                        {
                            var labelCell = table.Cell().Padding(3)
                                .Text(label).FontSize(10);
                            if (bold) labelCell.Bold();

                            var valueCell = table.Cell().Padding(3)
                                .AlignRight().Text(value).FontSize(10);
                            if (bold) valueCell.Bold();
                        }

                        Row("إجمالي المدفوعات", $"{invoice.TotalPayments:N2} د.ل");
                        Row("إجمالي الخصومات", $"{invoice.TotalDiscounts:N2} د.ل");
                        Row("صافي المدفوعات", $"{invoice.NetPayments:N2} د.ل");
                        Row("إجمالي المصاريف", $"{invoice.TotalExpenses:N2} د.ل");
                        Row("صافي التكلفة", $"{invoice.NetCost:N2} د.ل", bold: true);
                        Row("القيمة بالدولار", $"$ {invoice.TotalUsd:N2}");
                        Row("سعر الصرف", invoice.ExchangeRate.ToString("N6"));
                        Row("إجمالي البيع", $"{invoice.TotalSales:N2} د.ل", bold: true);
                        Row("إجمالي الربح", $"{invoice.TotalProfit:N2} د.ل", bold: true);
                        Row("هامش الربح %", $"{invoice.ProfitMargin:N4} %");
                    });

                    if (payments.Any())
                    {
                        col.Item().PaddingTop(10).Text("الدفعات").FontSize(12).Bold();
                        foreach (var p in payments)
                            col.Item().Text($"• {p.Label ?? "—"}: {p.Amount:N2} د.ل").FontSize(10);
                    }

                    if (discounts.Any())
                    {
                        col.Item().PaddingTop(6).Text("الخصومات").FontSize(12).Bold();
                        foreach (var d in discounts)
                            col.Item().Text($"• {d.Label ?? "—"}: {d.Amount:N2} د.ل").FontSize(10);
                    }

                    if (expenses.Any())
                    {
                        col.Item().PaddingTop(6).Text("المصاريف").FontSize(12).Bold();
                        foreach (var e in expenses)
                            col.Item().Text($"• {e.Label}: {e.Amount:N2} د.ل").FontSize(10);
                    }

                    if (!string.IsNullOrWhiteSpace(invoice.Notes))
                    {
                        col.Item().PaddingTop(10).Text("ملاحظات").FontSize(12).Bold();
                        col.Item().Text(invoice.Notes).FontSize(10);
                    }
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.CurrentPageNumber();
                    text.Span(" / ");
                    text.TotalPages();
                });
            });
        })
        .GeneratePdf(filePath);
    }
}