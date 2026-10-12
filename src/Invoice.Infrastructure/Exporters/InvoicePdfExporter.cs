using Invoice.Application.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using InvoiceEntity = Invoice.Domain.Entities.Invoice;
using InvoiceItemEntity = Invoice.Domain.Entities.InvoiceItem;

namespace Invoice.Infrastructure.Exporters;

/// <summary>
/// يُولّد ملف PDF للفاتورة بالعربية باستخدام QuestPDF.
/// صفحة 1: الشعار + الشركة + المورد + الفاتورة + الأصناف + الملاحظات
/// صفحة 2: المدفوعات + الخصومات + المصاريف + الملخص
///
/// يدعم إخفاء أجزاء كاملة عبر المعامل hidden (مفاتيح في Keys).
/// </summary>
public static class InvoicePdfExporter
{
    // ============================================================
    // مفاتيح الإخفاء — يجب مطابقتها مع Desktop و ExportsController
    // ============================================================
    public static class Keys
    {
        public const string Logo             = "logo";
        public const string CompanyInfo      = "company_info";
        public const string SupplierInfo     = "supplier_info";
        public const string InvoiceInfo      = "invoice_info";

        public const string ItemsLineNo      = "items.line_no";
        public const string ItemsProduct     = "items.product";
        public const string ItemsQuantity    = "items.quantity";
        public const string ItemsPurchaseUsd = "items.purchase_usd";
        public const string ItemsSaleDzd     = "items.sale_dzd";
        public const string ItemsTotalSale   = "items.total_sale";
        public const string ItemsTotalProfit = "items.total_profit";

        public const string Notes            = "notes";
        public const string Payments         = "payments";
        public const string Discounts        = "discounts";
        public const string Expenses         = "expenses";
        public const string Summary          = "summary";
    }

    static InvoicePdfExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
    }

    /// <summary>
    /// يُنشئ ملف PDF للفاتورة في المسار المحدد.
    /// </summary>
    /// <param name="hidden">مجموعة مفاتيح العناصر المخفية (اختياري).</param>
    public static void Export(
        InvoiceEntity invoice,
        CompanyInfoDto company,
        string filePath,
        HashSet<string>? hidden = null)
    {
        // نوحّد المقارنة بغض النظر عن حالة الأحرف
        var hiddenKeys = hidden != null
            ? new HashSet<string>(hidden, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        bool IsHidden(string key) => hiddenKeys.Contains(key);

        var items = invoice.Items.OrderBy(i => i.LineNo).ToList();
        var payments = invoice.Payments.OrderBy(p => p.LineNo).ToList();
        var discounts = invoice.Discounts.OrderBy(d => d.LineNo).ToList();
        var expenses = invoice.Expenses.OrderBy(e => e.LineNo).ToList();
        var supplier = invoice.Supplier;

        // أعمدة جدول الأصناف المتاحة (تُفلتر حسب الإخفاء)
        var allItemColumns = new List<ItemColumn>
        {
            new(Keys.ItemsLineNo,      "#",             0.5f, item => item.LineNo.ToString()),
            new(Keys.ItemsProduct,     "الصنف",         3f,   item => item.Product?.Name ?? "—"),
            new(Keys.ItemsQuantity,    "الكمية",        1f,   item => item.Quantity.ToString("0.##")),
            new(Keys.ItemsPurchaseUsd, "الشراء $",      1.5f, item => item.PurchaseUsd.ToString("N2")),
            new(Keys.ItemsSaleDzd,     "البيع د.ل",     1.5f, item => item.SaleDzd.ToString("N2")),
            new(Keys.ItemsTotalSale,   "إجمالي البيع",  2f,   item => item.TotalSale.ToString("N2")),
            new(Keys.ItemsTotalProfit, "الربح",         2f,   item => item.TotalProfit.ToString("N2")),
        };
        var visibleItemColumns = allItemColumns
            .Where(c => !IsHidden(c.Key))
            .ToList();

        // هل نحتاج الصفحة 2؟
        bool needPage2 =
            (!IsHidden(Keys.Payments)  && payments.Any())  ||
            (!IsHidden(Keys.Discounts) && discounts.Any()) ||
            (!IsHidden(Keys.Expenses)  && expenses.Any())  ||
             !IsHidden(Keys.Summary);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(11).FontFamily("Arial"));

                page.Content().Column(contentCol =>
                {
                    // ============================================
                    // ========== الصفحة 1 ==========
                    // ============================================

                    // ----- الشعار + بيانات الشركة -----
                    if (!IsHidden(Keys.Logo) || !IsHidden(Keys.CompanyInfo))
                    {
                        contentCol.Item().Row(row =>
                        {
                            if (!IsHidden(Keys.Logo))
                            {
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
                            }

                            if (!IsHidden(Keys.CompanyInfo))
                            {
                                row.RelativeItem()
                                    .PaddingLeft(IsHidden(Keys.Logo) ? 0 : 15)
                                    .AlignTop()
                                    .Column(col =>
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
                            }
                        });
                    }

                    // ----- بيانات المورد + بيانات الفاتورة -----
                    if (!IsHidden(Keys.SupplierInfo) || !IsHidden(Keys.InvoiceInfo))
                    {
                        contentCol.Item().PaddingTop(20).Row(row =>
                        {
                            if (!IsHidden(Keys.SupplierInfo))
                            {
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
                            }

                            if (!IsHidden(Keys.InvoiceInfo))
                            {
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
                            }
                        });
                    }

                    // ----- جدول الأصناف -----
                    if (visibleItemColumns.Any() && items.Any())
                    {
                        contentCol.Item().PaddingTop(20).Text("جدول الأصناف").FontSize(13).Bold();

                        contentCol.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                foreach (var c in visibleItemColumns)
                                {
                                    if (c.Key == Keys.ItemsLineNo)
                                        columns.ConstantColumn(30);
                                    else
                                        columns.RelativeColumn(c.Weight);
                                }
                            });

                            table.Header(header =>
                            {
                                void H(string text) =>
                                    header.Cell().Background(Colors.Grey.Lighten2)
                                        .Border(0.5f).BorderColor(Colors.Black)
                                        .Padding(4).AlignCenter().Text(text).Bold().FontSize(10);

                                foreach (var c in visibleItemColumns)
                                    H(c.Header);
                            });

                            foreach (var item in items)
                            {
                                void C(string text) =>
                                    table.Cell().Border(0.5f).BorderColor(Colors.Black)
                                        .Padding(4).AlignCenter().Text(text).FontSize(10);

                                foreach (var c in visibleItemColumns)
                                    C(c.Value(item));
                            }
                        });
                    }

                    // ----- الملاحظات -----
                    if (!IsHidden(Keys.Notes) && !string.IsNullOrWhiteSpace(invoice.Notes))
                    {
                        contentCol.Item().PaddingTop(15).Text("ملاحظات").FontSize(12).Bold();
                        contentCol.Item().PaddingTop(4).Text(invoice.Notes).FontSize(10);
                    }

                    // ----- فاصل الصفحات (فقط إذا الصفحة 2 لها محتوى) -----
                    if (needPage2)
                        contentCol.Item().PageBreak();

                    // ============================================
                    // ========== الصفحة 2 ==========
                    // ============================================

                    // ----- المدفوعات -----
                    if (!IsHidden(Keys.Payments) && payments.Any())
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
                    if (!IsHidden(Keys.Discounts) && discounts.Any())
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
                    if (!IsHidden(Keys.Expenses) && expenses.Any())
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
                    if (!IsHidden(Keys.Summary))
                    {
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
                                var labelCell = table.Cell().Padding(3).Text(label).FontSize(10);
                                if (bold) labelCell.Bold();

                                var valueCell = table.Cell().Padding(3).AlignRight().Text(value).FontSize(10);
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
                    }
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

    // ============ Internal Types ============

    private sealed record ItemColumn(
        string Key,
        string Header,
        float Weight,
        Func<InvoiceItemEntity, string> Value);
}