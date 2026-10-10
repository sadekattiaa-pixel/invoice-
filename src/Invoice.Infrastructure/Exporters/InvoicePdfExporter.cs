using Invoice.Application.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using InvoiceEntity = Invoice.Domain.Entities.Invoice;

namespace Invoice.Infrastructure.Exporters;

/// <summary>
/// يُولّد ملف PDF للفاتورة بالعربية باستخدام QuestPDF.
/// صفحة 1: الشعار + بيانات الشركة + بيانات المورد + الأصناف + الملاحظات
/// صفحة 2: المدفوعات + الخصومات + المصاريف + الملخص
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
    public static void Export(InvoiceEntity invoice, CompanyInfoDto company, string filePath)
    {
        var items = invoice.Items.OrderBy(i => i.LineNo).ToList();
        var payments = invoice.Payments.OrderBy(p => p.LineNo).ToList();
        var discounts = invoice.Discounts.OrderBy(d => d.LineNo).ToList();
        var expenses = invoice.Expenses.OrderBy(e => e.LineNo).ToList();
        var supplier = invoice.Supplier;

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                // ===================================================
                // ============ المحتوى (صفحة 1 + صفحة 2) ============
                // ===================================================
                page.Content().Column(contentCol =>
                {
                    // ============================================
                    // ========== الصفحة 1 ==========
                    // ============================================

                    // ----- الشعار (يسار، بدون إطار) + بيانات الشركة (يمين) -----
                    contentCol.Item().Row(row =>
                    {
                        // الشعار — بدون إطار، محاذى للأعلى
                        if (company.LogoBytes != null && company.LogoBytes.Length > 0)
                        {
                            row.ConstantItem(120)
                                .AlignTop().AlignLeft()
                                .Height(90)
                                .Image(company.LogoBytes).FitArea();
                        }
                        else
                        {
                            row.ConstantItem(120);
                        }

                        // بيانات الشركة (يمين) — القيم فقط، محاذاة للأعلى
                        row.RelativeItem().PaddingLeft(15).AlignTop().Column(col =>
                        {
                            if (!string.IsNullOrWhiteSpace(company.Name))
                                col.Item().Text(company.Name).FontSize(16).Bold();

                            if (!string.IsNullOrWhiteSpace(company.Address))
                                col.Item().Text(company.Address).FontSize(10);

                            if (!string.IsNullOrWhiteSpace(company.Phone))
                                col.Item().Text(company.Phone).FontSize(10);

                            if (!string.IsNullOrWhiteSpace(company.Email))
                                col.Item().Text(company.Email).FontSize(10);

                            if (!string.IsNullOrWhiteSpace(company.Website))
                                col.Item().Text(company.Website).FontSize(10);
                        });
                    });

                    // ----- بيانات المورد (يسار) + بيانات الفاتورة (يمين) -----
                    contentCol.Item().PaddingTop(20).Row(row =>
                    {
                        // المورد (يسار) — القيم فقط، محاذاة للأعلى
                        row.RelativeItem().AlignTop().Column(col =>
                        {
                            if (supplier != null)
                            {
                                if (!string.IsNullOrWhiteSpace(supplier.Name))
                                    col.Item().Text(supplier.Name).FontSize(10);
                                if (!string.IsNullOrWhiteSpace(supplier.Address))
                                    col.Item().Text(supplier.Address).FontSize(10);
                                if (!string.IsNullOrWhiteSpace(supplier.Phone))
                                    col.Item().Text(supplier.Phone).FontSize(10);
                                if (!string.IsNullOrWhiteSpace(supplier.Email))
                                    col.Item().Text(supplier.Email).FontSize(10);
                            }
                        });

                        // الفاتورة (يمين) — القيم فقط، محاذاة يمين + أعلى
                        row.RelativeItem().AlignTop().Column(col =>
                        {
                            col.Item().AlignRight()
                                .Text(invoice.InvoiceDate.ToString("yyyy-MM-dd")).FontSize(10);

                            col.Item().AlignRight()
                                .Text(invoice.InvoiceNo).FontSize(10);

                            if (!string.IsNullOrWhiteSpace(invoice.SupplierInvoiceNo))
                                col.Item().AlignRight()
                                    .Text(invoice.SupplierInvoiceNo).FontSize(10);
                        });
                    });

                    // ----- جدول الأصناف -----
                    contentCol.Item().PaddingTop(20).Text("جدول الأصناف").FontSize(13).Bold();

                    contentCol.Item().PaddingTop(6).Table(table =>
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
                                    .Border(0.5f).BorderColor(Colors.Black)
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
                                table.Cell()
                                    .Border(0.5f).BorderColor(Colors.Black)
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

                    // ----- ملاحظات -----
                    if (!string.IsNullOrWhiteSpace(invoice.Notes))
                    {
                        contentCol.Item().PaddingTop(15).Text("ملاحظات").FontSize(12).Bold();
                        contentCol.Item().PaddingTop(4).Text(invoice.Notes).FontSize(10);
                    }

                    // ----- فاصل صفحات -----
                    contentCol.Item().PageBreak();

                    // ============================================
                    // ========== الصفحة 2 ==========
                    // ============================================

                    // ----- المدفوعات -----
                    if (payments.Any())
                    {
                        contentCol.Item().Text("المدفوعات").FontSize(13).Bold();

                        contentCol.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                            });

                            foreach (var p in payments)
                            {
                                table.Cell().Padding(3).Text(p.Label ?? "—").FontSize(10);
                                table.Cell().Padding(3).AlignRight()
                                    .Text($"{p.Amount:N2} د.ل").FontSize(10);
                            }

                            table.Cell().Padding(3).Text("الإجمالي").Bold().FontSize(10);
                            table.Cell().Padding(3).AlignRight()
                                .Text($"{invoice.TotalPayments:N2} د.ل").Bold().FontSize(10);
                        });

                        contentCol.Item().PaddingTop(12);
                    }

                    // ----- الخصومات -----
                    if (discounts.Any())
                    {
                        contentCol.Item().Text("الخصومات").FontSize(13).Bold();

                        contentCol.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                            });

                            foreach (var d in discounts)
                            {
                                table.Cell().Padding(3).Text(d.Label ?? "—").FontSize(10);
                                table.Cell().Padding(3).AlignRight()
                                    .Text($"{d.Amount:N2} د.ل").FontSize(10);
                            }

                            table.Cell().Padding(3).Text("الإجمالي").Bold().FontSize(10);
                            table.Cell().Padding(3).AlignRight()
                                .Text($"{invoice.TotalDiscounts:N2} د.ل").Bold().FontSize(10);
                        });

                        contentCol.Item().PaddingTop(12);
                    }

                    // ----- المصاريف -----
                    if (expenses.Any())
                    {
                        contentCol.Item().Text("المصاريف").FontSize(13).Bold();

                        contentCol.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                            });

                            foreach (var e in expenses)
                            {
                                table.Cell().Padding(3).Text(e.Label).FontSize(10);
                                table.Cell().Padding(3).AlignRight()
                                    .Text($"{e.Amount:N2} د.ل").FontSize(10);
                            }

                            table.Cell().Padding(3).Text("الإجمالي").Bold().FontSize(10);
                            table.Cell().Padding(3).AlignRight()
                                .Text($"{invoice.TotalExpenses:N2} د.ل").Bold().FontSize(10);
                        });

                        contentCol.Item().PaddingTop(12);
                    }

                    // ----- الملخص -----
                    contentCol.Item().PaddingTop(8).Text("الملخص").FontSize(13).Bold();

                    contentCol.Item().PaddingTop(6).Table(table =>
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
                });

                // ============ Footer: أرقام الصفحات ============
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