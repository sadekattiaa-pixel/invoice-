using Invoice.Application.Dtos;
using Invoice.Infrastructure.Storage;
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
/// يدعم عرض صور الأصناف من مسارها على الخادم.
/// </summary>
public static class InvoicePdfExporter
{
    // ============================================================
    // مفاتيح الإخفاء — يجب مطابقتها مع Desktop و ExportsController
    // ============================================================
    public static class Keys
    {
        // ============ أجزاء الصفحة ============
        public const string Logo         = "logo";
        public const string CompanyInfo  = "company_info";
        public const string SupplierInfo = "supplier_info";
        public const string InvoiceInfo  = "invoice_info";
        public const string Notes        = "notes";
        public const string Payments     = "payments";
        public const string Discounts    = "discounts";
        public const string Expenses     = "expenses";
        public const string Summary      = "summary";

        // ============ أعمدة جدول الأصناف (21 عمودًا) ============
        public const string ItemsLineNo            = "items.line_no";
        public const string ItemsCode              = "items.code";
        public const string ItemsAh                = "items.ah";
        public const string ItemsL                 = "items.l";
        public const string ItemsW                 = "items.w";
        public const string ItemsH                 = "items.h";
        public const string ItemsOh                = "items.oh";
        public const string ItemsLayout            = "items.layout";
        public const string ItemsImage             = "items.image";
        public const string ItemsPallet            = "items.pallet";
        public const string ItemsCategory          = "items.category";
        public const string ItemsPattern           = "items.pattern";
        public const string ItemsQuantity          = "items.quantity";
        public const string ItemsPurchaseUsd       = "items.purchase_usd";
        public const string ItemsTotalPurchaseUsd  = "items.total_purchase_usd";
        public const string ItemsPurchaseDzd       = "items.purchase_dzd";
        public const string ItemsTotalDzd          = "items.total_dzd";
        public const string ItemsSaleDzd           = "items.sale_dzd";
        public const string ItemsTotalSale         = "items.total_sale";
        public const string ItemsUnitProfit        = "items.unit_profit";
        public const string ItemsTotalProfit       = "items.total_profit";

        // ============ مفاتيح قديمة (للتوافق الخلفي) ============
        public const string ItemsProduct = "items.product";
    }

    static InvoicePdfExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
    }

    /// <summary>
    /// يُنشئ ملف PDF للفاتورة في المسار المحدد.
    /// </summary>
    public static void Export(
        InvoiceEntity invoice,
        CompanyInfoDto company,
        string filePath,
        HashSet<string>? hidden = null)
    {
        var hiddenKeys = hidden != null
            ? new HashSet<string>(hidden, StringComparer.OrdinalIgnoreCase)
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        bool IsHidden(string key) => hiddenKeys.Contains(key);

        var items = invoice.Items.OrderBy(i => i.LineNo).ToList();
        var payments = invoice.Payments.OrderBy(p => p.LineNo).ToList();
        var discounts = invoice.Discounts.OrderBy(d => d.LineNo).ToList();
        var expenses = invoice.Expenses.OrderBy(e => e.LineNo).ToList();
        var supplier = invoice.Supplier;

        // ============ تحميل صور الأصناف من القرص مسبقًا ============
        var imageCache = new Dictionary<int, byte[]?>();
        foreach (var item in items)
        {
            var pid = item.ProductId;
            if (imageCache.ContainsKey(pid)) continue;

            var path = item.Product?.LayoutImageFilename;
            if (string.IsNullOrWhiteSpace(path))
            {
                imageCache[pid] = null;
                continue;
            }

            var bytes = ProductImageStorage.ReadBytesAsync(path).GetAwaiter().GetResult();
            imageCache[pid] = bytes;
        }

        // ============ أعمدة جدول الأصناف (21 عمودًا) ============
        var allItemColumns = new List<ItemColumn>
        {
            new(Keys.ItemsLineNo,           "#",                0.5f, item => item.LineNo.ToString()),
            new(Keys.ItemsCode,             "الكود",            1.8f, item => item.Product?.Code ?? "—"),
            new(Keys.ItemsAh,               "AH",               0.7f, item => item.Product?.Ah?.ToString("0.##") ?? ""),
            new(Keys.ItemsL,                "L",                0.6f, item => item.Product?.L?.ToString("0.##") ?? ""),
            new(Keys.ItemsW,                "W",                0.6f, item => item.Product?.W?.ToString("0.##") ?? ""),
            new(Keys.ItemsH,                "H",                0.6f, item => item.Product?.H?.ToString("0.##") ?? ""),
            new(Keys.ItemsOh,               "OH",               0.7f, item => item.Product?.Oh?.ToString("0.##") ?? ""),
            new(Keys.ItemsLayout,           "LAYOUT",           0.9f, item => item.Product?.Layout?.ToString() ?? ""),
            new(Keys.ItemsImage,            "الصورة",           0.9f, _ => "",      item => imageCache.GetValueOrDefault(item.ProductId)),
            new(Keys.ItemsPallet,           "PALLET",           0.8f, item => item.Product?.Pallet?.ToString() ?? ""),
            new(Keys.ItemsCategory,         "التصنيف",          1.2f, item => item.Product?.Category ?? ""),
            new(Keys.ItemsPattern,          "النقشة",           1.3f, item => item.Product?.Pattern ?? ""),
            new(Keys.ItemsQuantity,         "الكمية",           1.0f, item => item.Quantity.ToString("0.##")),
            new(Keys.ItemsPurchaseUsd,      "الشراء $",         1.3f, item => item.PurchaseUsd.ToString("N2")),
            new(Keys.ItemsTotalPurchaseUsd, "اجمالي الشراء $",  1.6f, item => (item.Quantity * item.PurchaseUsd).ToString("N2")),
            new(Keys.ItemsPurchaseDzd,      "التكلفة",          1.4f, item => item.PurchaseDzd.ToString("N2")),
            new(Keys.ItemsTotalDzd,         "اجمالي التكلفة",   1.6f, item => item.TotalDzd.ToString("N2")),
            new(Keys.ItemsSaleDzd,          "البيع",            1.4f, item => item.SaleDzd.ToString("N2")),
            new(Keys.ItemsTotalSale,        "اجمالي البيع",     1.6f, item => item.TotalSale.ToString("N2")),
            new(Keys.ItemsUnitProfit,       "الربح",            1.4f, item => item.UnitProfit.ToString("N2")),
            new(Keys.ItemsTotalProfit,      "اجمالي الربح",     1.6f, item => item.TotalProfit.ToString("N2")),
        };

        var visibleItemColumns = allItemColumns
            .Where(c => !IsHidden(c.Key))
            .ToList();

        bool needPage2 =
            (!IsHidden(Keys.Payments)  && payments.Any())  ||
            (!IsHidden(Keys.Discounts) && discounts.Any()) ||
            (!IsHidden(Keys.Expenses)  && expenses.Any())  ||
             !IsHidden(Keys.Summary);

        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4.Landscape());
                page.Margin(1f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9).FontFamily("Arial"));

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
                                            col.Item().Text(company.Name).FontSize(14).Bold();
                                        if (!string.IsNullOrWhiteSpace(company.Address))
                                            col.Item().Text(company.Address).FontSize(9);
                                        if (!string.IsNullOrWhiteSpace(company.Phone))
                                            col.Item().Text(company.Phone).FontSize(9);
                                        if (!string.IsNullOrWhiteSpace(company.Email))
                                            col.Item().Text(company.Email).FontSize(9);
                                        if (!string.IsNullOrWhiteSpace(company.Website))
                                            col.Item().Text(company.Website).FontSize(9);
                                    });
                            }
                        });
                    }

                    // ----- بيانات المورد + بيانات الفاتورة -----
                    if (!IsHidden(Keys.SupplierInfo) || !IsHidden(Keys.InvoiceInfo))
                    {
                        contentCol.Item().PaddingTop(15).Row(row =>
                        {
                            if (!IsHidden(Keys.SupplierInfo))
                            {
                                row.RelativeItem().AlignTop().Column(col =>
                                {
                                    if (supplier != null)
                                    {
                                        if (!string.IsNullOrWhiteSpace(supplier.Name))
                                            col.Item().Text(supplier.Name).FontSize(9);
                                        if (!string.IsNullOrWhiteSpace(supplier.Address))
                                            col.Item().Text(supplier.Address).FontSize(9);
                                        if (!string.IsNullOrWhiteSpace(supplier.Phone))
                                            col.Item().Text(supplier.Phone).FontSize(9);
                                        if (!string.IsNullOrWhiteSpace(supplier.Email))
                                            col.Item().Text(supplier.Email).FontSize(9);
                                    }
                                });
                            }

                            if (!IsHidden(Keys.InvoiceInfo))
                            {
                                row.RelativeItem().AlignTop().Column(col =>
                                {
                                    col.Item().AlignRight()
                                        .Text(invoice.InvoiceDate.ToString("yyyy-MM-dd")).FontSize(9);
                                    col.Item().AlignRight()
                                        .Text(invoice.InvoiceNo).FontSize(9);
                                    if (!string.IsNullOrWhiteSpace(invoice.SupplierInvoiceNo))
                                        col.Item().AlignRight()
                                            .Text(invoice.SupplierInvoiceNo).FontSize(9);
                                });
                            }
                        });
                    }

                    // ----- جدول الأصناف -----
                    if (visibleItemColumns.Any() && items.Any())
                    {
                        contentCol.Item().PaddingTop(15).Text("جدول الأصناف").FontSize(12).Bold();

                        contentCol.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                foreach (var c in visibleItemColumns)
                                {
                                    if (c.Key == Keys.ItemsLineNo)
                                        columns.ConstantColumn(25);
                                    else
                                        columns.RelativeColumn(c.Weight);
                                }
                            });

                            table.Header(header =>
                            {
                                void H(string text) =>
                                    header.Cell().Background(Colors.Grey.Lighten2)
                                        .Border(0.5f).BorderColor(Colors.Black)
                                        .Padding(3).AlignCenter().Text(text).Bold().FontSize(8);

                                foreach (var c in visibleItemColumns)
                                    H(c.Header);
                            });

                            foreach (var item in items)
                            {
                                foreach (var c in visibleItemColumns)
                                {
                                    var cell = table.Cell()
                                        .Border(0.5f).BorderColor(Colors.Black)
                                        .Padding(2)
                                        .AlignCenter();

                                    // ===== عمود الصورة =====
                                    if (c.ImageBytes != null)
                                    {
                                        var bytes = c.ImageBytes(item);
                                        if (bytes != null && bytes.Length > 0)
                                            cell.Height(40).Image(bytes).FitArea();
                                        else
                                            cell.Text("").FontSize(8);
                                    }
                                    else
                                    {
                                        cell.Padding(3).Text(c.Value(item)).FontSize(8);
                                    }
                                }
                            }
                        });
                    }

                    // ----- الملاحظات -----
                    if (!IsHidden(Keys.Notes) && !string.IsNullOrWhiteSpace(invoice.Notes))
                    {
                        contentCol.Item().PaddingTop(12).Text("ملاحظات").FontSize(11).Bold();
                        contentCol.Item().PaddingTop(4).Text(invoice.Notes).FontSize(9);
                    }

                    if (needPage2)
                        contentCol.Item().PageBreak();

                    // ============================================
                    // ========== الصفحة 2 ==========
                    // ============================================

                    // ----- المدفوعات -----
                    if (!IsHidden(Keys.Payments) && payments.Any())
                    {
                        contentCol.Item().Text("المدفوعات").FontSize(12).Bold();

                        contentCol.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                            });

                            foreach (var p in payments)
                            {
                                table.Cell().Padding(3).Text(p.Label ?? "—").FontSize(9);
                                table.Cell().Padding(3).AlignRight()
                                    .Text($"{p.Amount:N2}").FontSize(9);
                            }

                            table.Cell().Padding(3).Text("الإجمالي").Bold().FontSize(9);
                            table.Cell().Padding(3).AlignRight()
                                .Text($"{invoice.TotalPayments:N2}").Bold().FontSize(9);
                        });

                        contentCol.Item().PaddingTop(10);
                    }

                    // ----- الخصومات -----
                    if (!IsHidden(Keys.Discounts) && discounts.Any())
                    {
                        contentCol.Item().Text("الخصومات").FontSize(12).Bold();

                        contentCol.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                            });

                            foreach (var d in discounts)
                            {
                                table.Cell().Padding(3).Text(d.Label ?? "—").FontSize(9);
                                table.Cell().Padding(3).AlignRight()
                                    .Text($"{d.Amount:N2}").FontSize(9);
                            }

                            table.Cell().Padding(3).Text("الإجمالي").Bold().FontSize(9);
                            table.Cell().Padding(3).AlignRight()
                                .Text($"{invoice.TotalDiscounts:N2}").Bold().FontSize(9);
                        });

                        contentCol.Item().PaddingTop(10);
                    }

                    // ----- المصاريف -----
                    if (!IsHidden(Keys.Expenses) && expenses.Any())
                    {
                        contentCol.Item().Text("المصاريف").FontSize(12).Bold();

                        contentCol.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3);
                                columns.RelativeColumn(2);
                            });

                            foreach (var e in expenses)
                            {
                                table.Cell().Padding(3).Text(e.Label).FontSize(9);
                                table.Cell().Padding(3).AlignRight()
                                    .Text($"{e.Amount:N2}").FontSize(9);
                            }

                            table.Cell().Padding(3).Text("الإجمالي").Bold().FontSize(9);
                            table.Cell().Padding(3).AlignRight()
                                .Text($"{invoice.TotalExpenses:N2}").Bold().FontSize(9);
                        });

                        contentCol.Item().PaddingTop(10);
                    }

                    // ----- الملخص -----
                    if (!IsHidden(Keys.Summary))
                    {
                        contentCol.Item().PaddingTop(8).Text("الملخص").FontSize(12).Bold();

                        contentCol.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                            });

                            void Row(string label, string value, bool bold = false)
                            {
                                var labelCell = table.Cell().Padding(3).Text(label).FontSize(9);
                                if (bold) labelCell.Bold();

                                var valueCell = table.Cell().Padding(3).AlignRight().Text(value).FontSize(9);
                                if (bold) valueCell.Bold();
                            }

                            Row("إجمالي المدفوعات", $"{invoice.TotalPayments:N2}");
                            Row("إجمالي الخصومات", $"{invoice.TotalDiscounts:N2}");
                            Row("صافي المدفوعات", $"{invoice.NetPayments:N2}");
                            Row("إجمالي المصاريف", $"{invoice.TotalExpenses:N2}");
                            Row("صافي التكلفة", $"{invoice.NetCost:N2}", bold: true);
                            Row("القيمة بالدولار", $"$ {invoice.TotalUsd:N2}");
                            Row("سعر الصرف", invoice.ExchangeRate.ToString("N6"));
                            Row("إجمالي البيع", $"{invoice.TotalSales:N2}", bold: true);
                            Row("إجمالي الربح", $"{invoice.TotalProfit:N2}", bold: true);
                            Row("هامش الربح %", $"{invoice.ProfitMargin:N4} %");
                        });
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

    // ============ Internal Types ============

    private sealed record ItemColumn(
        string Key,
        string Header,
        float Weight,
        Func<InvoiceItemEntity, string> Value,
        Func<InvoiceItemEntity, byte[]?>? ImageBytes = null);
}