using ClosedXML.Excel;
using Invoice.Application.Dtos;
using InvoiceEntity = Invoice.Domain.Entities.Invoice;

namespace Invoice.Infrastructure.Exporters;

/// <summary>
/// يُولّد ملف Excel للفاتورة — بنفس ترتيب PDF، بورقتين:
/// ورقة "الفاتورة": الشعار + بيانات الشركة + المورد + الأصناف + ملاحظات
/// ورقة "الملخص": المدفوعات + الخصومات + المصاريف + الملخص
/// </summary>
public static class InvoiceExcelExporter
{
    public static void Export(InvoiceEntity invoice, CompanyInfoDto company, string filePath)
    {
        using var workbook = new XLWorkbook();

        // ================= الورقة 1: الفاتورة =================
        var ws1 = workbook.Worksheets.Add("الفاتورة");
        ws1.RightToLeft = true;
        BuildInvoiceSheet(ws1, invoice, company);

        // ================= الورقة 2: الملخص =================
        var ws2 = workbook.Worksheets.Add("الملخص");
        ws2.RightToLeft = true;
        BuildSummarySheet(ws2, invoice);

        workbook.SaveAs(filePath);
    }

    // ============================================================
    // الورقة 1: الترويسة + المورد + الأصناف + الملاحظات
    // ============================================================
    private static void BuildInvoiceSheet(IXLWorksheet ws, InvoiceEntity invoice, CompanyInfoDto company)
    {
        const int LastCol = 7;
        int row = 1;
        var supplier = invoice.Supplier;

        // عرض ثابت للأعمدة
        for (int c = 1; c <= LastCol; c++)
            ws.Column(c).Width = 14;

        // ====================================================
        // ========== 1) بيانات الشركة (يمين) + الشعار (يسار) ==========
        // ====================================================
        // في ورقة RTL: العمود A على اليمين، العمود G على اليسار.
        // بيانات الشركة في A:C (يمين)، الشعار في E:G (يسار).

        void CompanyLine(string? value, bool bold = false, int fontSize = 11)
        {
            var r = ws.Range(row, 1, row, 3);
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

        int logoStartRow = row;
        CompanyLine(company.Name, bold: true, fontSize: 16);
        CompanyLine(company.Address);
        CompanyLine(company.Phone);
        CompanyLine(company.Email);
        CompanyLine(company.Website);
        int companyDataEndRow = row - 1;

        // الشعار — على اليسار (العمود E, أي رقم 5)
        if (company.LogoBytes != null && company.LogoBytes.Length > 0)
        {
            using var ms = new MemoryStream(company.LogoBytes);
            var picture = ws.AddPicture(ms);
            picture.Name = "CompanyLogo";
            picture.MoveTo(ws.Cell(logoStartRow, 5));
            picture.WithSize(160, 100);
        }

        row++; // فراغ

        // ====================================================
        // ========== 2) بيانات الفاتورة (يمين) + المورد (يسار) ==========
        // ====================================================

        // قائمة سطور الفاتورة (يمين)
        var invoiceLines = new List<string?>
        {
            invoice.InvoiceDate.ToString("yyyy-MM-dd"),
            invoice.InvoiceNo
        };
        if (!string.IsNullOrWhiteSpace(invoice.SupplierInvoiceNo))
            invoiceLines.Add(invoice.SupplierInvoiceNo);

        // قائمة سطور المورد (يسار)
        var supplierLines = new List<string?>();
        if (supplier != null)
        {
            if (!string.IsNullOrWhiteSpace(supplier.Name)) supplierLines.Add(supplier.Name);
            if (!string.IsNullOrWhiteSpace(supplier.Address)) supplierLines.Add(supplier.Address);
            if (!string.IsNullOrWhiteSpace(supplier.Phone)) supplierLines.Add(supplier.Phone);
            if (!string.IsNullOrWhiteSpace(supplier.Email)) supplierLines.Add(supplier.Email);
        }

        int maxLines = Math.Max(invoiceLines.Count, supplierLines.Count);
        for (int i = 0; i < maxLines; i++)
        {
            // الفاتورة على اليمين — A:C
            var invVal = i < invoiceLines.Count ? invoiceLines[i] : "";
            var rRight = ws.Range(row, 1, row, 3);
            rRight.Merge();
            rRight.Value = invVal;
            rRight.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            rRight.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

            // المورد على اليسار — E:G
            var supVal = i < supplierLines.Count ? supplierLines[i] : "";
            var rLeft = ws.Range(row, 5, row, LastCol);
            rLeft.Merge();
            rLeft.Value = supVal;
            rLeft.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            rLeft.Style.Alignment.Vertical = XLAlignmentVerticalValues.Top;

            row++;
        }

        row++; // فراغ

        // ====================================================
        // ========== 3) جدول الأصناف ==========
        // ====================================================
        var titleR = ws.Range(row, 1, row, LastCol);
        titleR.Merge();
        titleR.Value = "جدول الأصناف";
        titleR.Style.Font.Bold = true;
        titleR.Style.Font.FontSize = 13;
        titleR.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        row++;

        int tableStartRow = row;
        string[] headers = { "#", "الصنف", "الكمية", "الشراء $", "البيع د.ل", "إجمالي البيع", "الربح" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(row, i + 1);
            cell.Value = headers[i];
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
            ws.Cell(row, 1).Value = item.LineNo;
            ws.Cell(row, 2).Value = item.Product?.Name ?? "—";
            ws.Cell(row, 3).Value = item.Quantity;
            ws.Cell(row, 4).Value = item.PurchaseUsd;
            ws.Cell(row, 5).Value = item.SaleDzd;
            ws.Cell(row, 6).Value = item.TotalSale;
            ws.Cell(row, 7).Value = item.TotalProfit;

            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.##";
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";

            for (int c = 1; c <= LastCol; c++)
            {
                var cell = ws.Cell(row, c);
                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.Black;
            }
            row++;
        }

        int tableEndRow = row - 1;
        var tableRange = ws.Range(tableStartRow, 1, tableEndRow, LastCol);
        tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
        tableRange.Style.Border.OutsideBorderColor = XLColor.Black;
        tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        tableRange.Style.Border.InsideBorderColor = XLColor.Gray;

        row++; // فراغ

        // ====================================================
        // ========== 4) ملاحظات ==========
        // ====================================================
        if (!string.IsNullOrWhiteSpace(invoice.Notes))
        {
            var t = ws.Range(row, 1, row, LastCol);
            t.Merge();
            t.Value = "ملاحظات";
            t.Style.Font.Bold = true;
            t.Style.Font.FontSize = 13;
            t.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            row++;

            var n = ws.Range(row, 1, row, LastCol);
            n.Merge();
            n.Value = invoice.Notes;
            n.Style.Alignment.WrapText = true;
            n.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
        }
    }

    // ============================================================
    // الورقة 2: المدفوعات + الخصومات + المصاريف + الملخص
    // ============================================================
    private static void BuildSummarySheet(IXLWorksheet ws, InvoiceEntity invoice)
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
            rValue.Value = $"{amount:N2} د.ل";
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
            rValue.Value = $"{amount:N2} د.ل";
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
        var payments = invoice.Payments.OrderBy(p => p.LineNo).ToList();
        if (payments.Count > 0)
        {
            SectionTitle("المدفوعات");
            foreach (var p in payments)
                SectionItem(p.Label ?? "—", p.Amount);
            SectionTotal("الإجمالي", invoice.TotalPayments);
            row++;
        }

        // ========== 2) الخصومات ==========
        var discounts = invoice.Discounts.OrderBy(d => d.LineNo).ToList();
        if (discounts.Count > 0)
        {
            SectionTitle("الخصومات");
            foreach (var d in discounts)
                SectionItem(d.Label ?? "—", d.Amount);
            SectionTotal("الإجمالي", invoice.TotalDiscounts);
            row++;
        }

        // ========== 3) المصاريف ==========
        var expenses = invoice.Expenses.OrderBy(e => e.LineNo).ToList();
        if (expenses.Count > 0)
        {
            SectionTitle("المصاريف");
            foreach (var e in expenses)
                SectionItem(e.Label, e.Amount);
            SectionTotal("الإجمالي", invoice.TotalExpenses);
            row++;
        }

        // ========== 4) الملخص ==========
        SectionTitle("الملخص");
        SummaryRow("إجمالي المدفوعات", $"{invoice.TotalPayments:N2} د.ل");
        SummaryRow("إجمالي الخصومات", $"{invoice.TotalDiscounts:N2} د.ل");
        SummaryRow("صافي المدفوعات", $"{invoice.NetPayments:N2} د.ل");
        SummaryRow("إجمالي المصاريف", $"{invoice.TotalExpenses:N2} د.ل");
        SummaryRow("صافي التكلفة", $"{invoice.NetCost:N2} د.ل", bold: true);
        SummaryRow("القيمة بالدولار", $"$ {invoice.TotalUsd:N2}");
        SummaryRow("سعر الصرف", invoice.ExchangeRate.ToString("N6"));
        SummaryRow("إجمالي البيع", $"{invoice.TotalSales:N2} د.ل", bold: true);
        SummaryRow("إجمالي الربح", $"{invoice.TotalProfit:N2} د.ل", bold: true);
        SummaryRow("هامش الربح %", $"{invoice.ProfitMargin:N4} %");
    }
}