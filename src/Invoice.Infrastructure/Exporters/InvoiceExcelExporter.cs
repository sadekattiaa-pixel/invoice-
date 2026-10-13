using ClosedXML.Excel;
using Invoice.Application.Dtos;
using InvoiceEntity = Invoice.Domain.Entities.Invoice;
using InvoiceItemEntity = Invoice.Domain.Entities.InvoiceItem;

namespace Invoice.Infrastructure.Exporters;

/// <summary>
/// يُولّد ملف Excel للفاتورة — بورقتين:
/// ورقة "الفاتورة": الشعار + الشركة + المورد + الأصناف + الملاحظات
/// ورقة "الملخص": المدفوعات + الخصومات + المصاريف + الملخص
///
/// يدعم إخفاء أجزاء كاملة عبر المعامل hidden (مفاتيح InvoicePdfExporter.Keys).
/// </summary>
public static class InvoiceExcelExporter
{
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

        using var workbook = new XLWorkbook();

        // ================= الورقة 1: الفاتورة =================
        var ws1 = workbook.Worksheets.Add("الفاتورة");
        ws1.RightToLeft = true;
        BuildInvoiceSheet(ws1, invoice, company, IsHidden);

        // ================= الورقة 2: الملخص =================
        var ws2 = workbook.Worksheets.Add("الملخص");
        ws2.RightToLeft = true;
        BuildSummarySheet(ws2, invoice, IsHidden);

        workbook.SaveAs(filePath);
    }

    // ============================================================
    // الورقة 1
    // ============================================================
    private static void BuildInvoiceSheet(
        IXLWorksheet ws,
        InvoiceEntity invoice,
        CompanyInfoDto company,
        Func<string, bool> IsHidden)
    {
        // ============ حساب أعمدة جدول الأصناف (21 عمودًا) ============
        var allCols = new List<ItemColumn>
        {
            new(InvoicePdfExporter.Keys.ItemsLineNo,           "#",                0.5,  item => item.LineNo,                        null,          true),
            new(InvoicePdfExporter.Keys.ItemsCode,             "الكود",            1.8,  item => item.Product?.Code ?? "—",          null,          false),
            new(InvoicePdfExporter.Keys.ItemsAh,               "AH",               0.7,  item => (object?)item.Product?.Ah,          "#,##0.##",    false),
            new(InvoicePdfExporter.Keys.ItemsL,                "L",                0.6,  item => (object?)item.Product?.L,           "#,##0.##",    false),
            new(InvoicePdfExporter.Keys.ItemsW,                "W",                0.6,  item => (object?)item.Product?.W,           "#,##0.##",    false),
            new(InvoicePdfExporter.Keys.ItemsH,                "H",                0.6,  item => (object?)item.Product?.H,           "#,##0.##",    false),
            new(InvoicePdfExporter.Keys.ItemsOh,               "OH",               0.7,  item => (object?)item.Product?.Oh,          "#,##0.##",    false),
            new(InvoicePdfExporter.Keys.ItemsLayout,           "LAYOUT",           0.9,  item => (object?)item.Product?.Layout,      null,          false),
            new(InvoicePdfExporter.Keys.ItemsImage,            "الصورة",           0.8,  _ => "",                                    null,          false),
            new(InvoicePdfExporter.Keys.ItemsPallet,           "PALLET",           0.8,  item => (object?)item.Product?.Pallet,      null,          false),
            new(InvoicePdfExporter.Keys.ItemsCategory,         "التصنيف",          1.2,  item => item.Product?.Category ?? "",       null,          false),
            new(InvoicePdfExporter.Keys.ItemsPattern,          "النقشة",           1.3,  item => item.Product?.Pattern ?? "",        null,          false),
            new(InvoicePdfExporter.Keys.ItemsQuantity,         "الكمية",           1.0,  item => item.Quantity,                      "#,##0.##",    false),
            new(InvoicePdfExporter.Keys.ItemsPurchaseUsd,      "الشراء $",         1.3,  item => item.PurchaseUsd,                   "#,##0.00",    false),
            new(InvoicePdfExporter.Keys.ItemsTotalPurchaseUsd, "اجمالي الشراء $",  1.6,  item => item.Quantity * item.PurchaseUsd,   "#,##0.00",    false),
            new(InvoicePdfExporter.Keys.ItemsPurchaseDzd,      "التكلفة",          1.4,  item => item.PurchaseDzd,                   "#,##0.00",    false),
            new(InvoicePdfExporter.Keys.ItemsTotalDzd,         "اجمالي التكلفة",   1.6,  item => item.TotalDzd,                      "#,##0.00",    false),
            new(InvoicePdfExporter.Keys.ItemsSaleDzd,          "البيع",            1.4,  item => item.SaleDzd,                       "#,##0.00",    false),
            new(InvoicePdfExporter.Keys.ItemsTotalSale,        "اجمالي البيع",     1.6,  item => item.TotalSale,                     "#,##0.00",    false),
            new(InvoicePdfExporter.Keys.ItemsUnitProfit,       "الربح",            1.4,  item => item.UnitProfit,                    "#,##0.00",    false),
            new(InvoicePdfExporter.Keys.ItemsTotalProfit,      "اجمالي الربح",     1.6,  item => item.TotalProfit,                   "#,##0.00",    false),
        };
        var visibleCols = allCols.Where(c => !IsHidden(c.Key)).ToList();

        // LastCol = عدد الأعمدة الظاهرة أو 7 (الحد الأدنى للعناوين)
        int lastCol = Math.Max(visibleCols.Count, 7);

        int row = 1;
        var supplier = invoice.Supplier;

        for (int c = 1; c <= lastCol; c++)
            ws.Column(c).Width = 14;

        // ====================================================
        // ========== 1) الشركة (يمين) + الشعار (يسار) ==========
        // ====================================================
        if (!IsHidden(InvoicePdfExporter.Keys.CompanyInfo))
        {
            void CompanyLine(string? value, bool bold = false, int fontSize = 11)
            {
                var r = ws.Range(row, 1, row, Math.Min(3, lastCol));
                r.Merge();
                var cell = ws.Cell(row, 1);
                cell.Value = value ?? "";
                cell.Style.Font.Bold = bold;
                cell.Style.Font.FontSize = fontSize;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
                if (ws.Row(row).Height < 20) ws.Row(row).Height = 20;
                row++;
            }

            CompanyLine(company.Name, bold: true, fontSize: 16);
            CompanyLine(company.Address);
            CompanyLine(company.Phone);
            CompanyLine(company.Email);
            CompanyLine(company.Website);
        }

        // الشعار
        if (!IsHidden(InvoicePdfExporter.Keys.Logo)
            && company.LogoBytes != null && company.LogoBytes.Length > 0)
        {
            using var ms = new MemoryStream(company.LogoBytes);
            var picture = ws.AddPicture(ms);
            picture.Name = "CompanyLogo";
            picture.MoveTo(ws.Cell(1, Math.Min(5, lastCol)));
            picture.WithSize(160, 100);
        }

        int logoRows = 6;
        while (row < logoRows) row++;
        row++;

        // ====================================================
        // ========== 2) الفاتورة (يمين) + المورد (يسار) ==========
        // ====================================================
        var invoiceLines = new List<string?>();
        if (!IsHidden(InvoicePdfExporter.Keys.InvoiceInfo))
        {
            invoiceLines.Add(invoice.InvoiceDate.ToString("yyyy-MM-dd"));
            invoiceLines.Add(invoice.InvoiceNo);
            if (!string.IsNullOrWhiteSpace(invoice.SupplierInvoiceNo))
                invoiceLines.Add(invoice.SupplierInvoiceNo);
        }

        var supplierLines = new List<string?>();
        if (!IsHidden(InvoicePdfExporter.Keys.SupplierInfo) && supplier != null)
        {
            if (!string.IsNullOrWhiteSpace(supplier.Name)) supplierLines.Add(supplier.Name);
            if (!string.IsNullOrWhiteSpace(supplier.Address)) supplierLines.Add(supplier.Address);
            if (!string.IsNullOrWhiteSpace(supplier.Phone)) supplierLines.Add(supplier.Phone);
            if (!string.IsNullOrWhiteSpace(supplier.Email)) supplierLines.Add(supplier.Email);
        }

        int maxLines = Math.Max(invoiceLines.Count, supplierLines.Count);
        for (int i = 0; i < maxLines; i++)
        {
            var invVal = i < invoiceLines.Count ? invoiceLines[i] : "";
            var rRight = ws.Range(row, 1, row, Math.Min(3, lastCol));
            rRight.Merge();
            rRight.Value = invVal;
            rRight.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            rRight.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

            var supVal = i < supplierLines.Count ? supplierLines[i] : "";
            if (lastCol >= 5)
            {
                var rLeft = ws.Range(row, 5, row, lastCol);
                rLeft.Merge();
                rLeft.Value = supVal;
                rLeft.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                rLeft.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;
            }

            row++;
        }

        row++;

        // ====================================================
        // ========== 3) جدول الأصناف ==========
        // ====================================================
        if (visibleCols.Count > 0 && invoice.Items.Any())
        {
            var titleR = ws.Range(row, 1, row, lastCol);
            titleR.Merge();
            titleR.Value = "جدول الأصناف";
            titleR.Style.Font.Bold = true;
            titleR.Style.Font.FontSize = 13;
            titleR.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            row++;

            int tableStartRow = row;
            for (int i = 0; i < visibleCols.Count; i++)
            {
                var cell = ws.Cell(row, i + 1);
                cell.Value = visibleCols[i].Header;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.Black;
            }
            row++;

            foreach (var item in invoice.Items.OrderBy(i => i.LineNo))
            {
                for (int i = 0; i < visibleCols.Count; i++)
                {
                    var col = visibleCols[i];
                    var cell = ws.Cell(row, i + 1);
                    var val = col.Get(item);

                    if (val is null)
                        cell.Value = "";
                    else if (val is decimal d)
                        cell.Value = d;
                    else if (val is int intVal)
                        cell.Value = intVal;
                    else
                        cell.Value = val.ToString() ?? "";

                    if (!string.IsNullOrEmpty(col.Format))
                        cell.Style.NumberFormat.Format = col.Format;

                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.Black;
                }
                row++;
            }

            int tableEndRow = row - 1;
            if (tableEndRow >= tableStartRow)
            {
                var tableRange = ws.Range(tableStartRow, 1, tableEndRow, visibleCols.Count);
                tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
                tableRange.Style.Border.OutsideBorderColor = XLColor.Black;
                tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
                tableRange.Style.Border.InsideBorderColor = XLColor.Gray;
            }

            row++;
        }

        // ====================================================
        // ========== 4) ملاحظات ==========
        // ====================================================
        if (!IsHidden(InvoicePdfExporter.Keys.Notes) && !string.IsNullOrWhiteSpace(invoice.Notes))
        {
            var t = ws.Range(row, 1, row, lastCol);
            t.Merge();
            t.Value = "ملاحظات";
            t.Style.Font.Bold = true;
            t.Style.Font.FontSize = 13;
            t.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            row++;

            var n = ws.Range(row, 1, row, lastCol);
            n.Merge();
            n.Value = invoice.Notes;
            n.Style.Alignment.WrapText = true;
            n.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        }
    }

    // ============================================================
    // الورقة 2
    // ============================================================
    private static void BuildSummarySheet(
        IXLWorksheet ws,
        InvoiceEntity invoice,
        Func<string, bool> IsHidden)
    {
        const int LastCol = 7;
        int row = 1;

        for (int c = 1; c <= LastCol; c++)
            ws.Column(c).Width = 14;

        void SectionTitle(string title)
        {
            var r = ws.Range(row, 1, row, LastCol);
            r.Merge();
            r.Value = title;
            r.Style.Font.Bold = true;
            r.Style.Font.FontSize = 13;
            r.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            row++;
        }

        void SectionItem(string label, decimal amount)
        {
            var rLabel = ws.Range(row, 1, row, 4);
            rLabel.Merge();
            rLabel.Value = label;
            rLabel.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            var rValue = ws.Range(row, 5, row, LastCol);
            rValue.Merge();
            rValue.Value = $"{amount:N2}";
            rValue.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            row++;
        }

        void SectionTotal(string label, decimal amount)
        {
            var rLabel = ws.Range(row, 1, row, 4);
            rLabel.Merge();
            rLabel.Value = label;
            rLabel.Style.Font.Bold = true;
            rLabel.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            var rValue = ws.Range(row, 5, row, LastCol);
            rValue.Merge();
            rValue.Value = $"{amount:N2}";
            rValue.Style.Font.Bold = true;
            rValue.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            row++;
        }

        void SummaryRow(string label, string value, bool bold = false)
        {
            var rLabel = ws.Range(row, 1, row, 4);
            rLabel.Merge();
            rLabel.Value = label;
            rLabel.Style.Font.Bold = bold;
            rLabel.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;

            var rValue = ws.Range(row, 5, row, LastCol);
            rValue.Merge();
            rValue.Value = value;
            rValue.Style.Font.Bold = bold;
            rValue.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

            row++;
        }

        // ========== 1) المدفوعات ==========
        if (!IsHidden(InvoicePdfExporter.Keys.Payments))
        {
            var payments = invoice.Payments.OrderBy(p => p.LineNo).ToList();
            if (payments.Count > 0)
            {
                SectionTitle("المدفوعات");
                foreach (var p in payments)
                    SectionItem(p.Label ?? "—", p.Amount);
                SectionTotal("الإجمالي", invoice.TotalPayments);
                row++;
            }
        }

        // ========== 2) الخصومات ==========
        if (!IsHidden(InvoicePdfExporter.Keys.Discounts))
        {
            var discounts = invoice.Discounts.OrderBy(d => d.LineNo).ToList();
            if (discounts.Count > 0)
            {
                SectionTitle("الخصومات");
                foreach (var d in discounts)
                    SectionItem(d.Label ?? "—", d.Amount);
                SectionTotal("الإجمالي", invoice.TotalDiscounts);
                row++;
            }
        }

        // ========== 3) المصاريف ==========
        if (!IsHidden(InvoicePdfExporter.Keys.Expenses))
        {
            var expenses = invoice.Expenses.OrderBy(e => e.LineNo).ToList();
            if (expenses.Count > 0)
            {
                SectionTitle("المصاريف");
                foreach (var e in expenses)
                    SectionItem(e.Label, e.Amount);
                SectionTotal("الإجمالي", invoice.TotalExpenses);
                row++;
            }
        }

        // ========== 4) الملخص ==========
        if (!IsHidden(InvoicePdfExporter.Keys.Summary))
        {
            SectionTitle("الملخص");
            SummaryRow("إجمالي المدفوعات", $"{invoice.TotalPayments:N2}");
            SummaryRow("إجمالي الخصومات", $"{invoice.TotalDiscounts:N2}");
            SummaryRow("صافي المدفوعات", $"{invoice.NetPayments:N2}");
            SummaryRow("إجمالي المصاريف", $"{invoice.TotalExpenses:N2}");
            SummaryRow("صافي التكلفة", $"{invoice.NetCost:N2}", bold: true);
            SummaryRow("القيمة بالدولار", $"$ {invoice.TotalUsd:N2}");
            SummaryRow("سعر الصرف", invoice.ExchangeRate.ToString("N6"));
            SummaryRow("إجمالي البيع", $"{invoice.TotalSales:N2}", bold: true);
            SummaryRow("إجمالي الربح", $"{invoice.TotalProfit:N2}", bold: true);
            SummaryRow("هامش الربح %", $"{invoice.ProfitMargin:N4} %");
        }
    }

    // ============ Internal Types ============

    private sealed record ItemColumn(
        string Key,
        string Header,
        double Width,
        Func<InvoiceItemEntity, object?> Get,
        string? Format,
        bool IsConstant);
}