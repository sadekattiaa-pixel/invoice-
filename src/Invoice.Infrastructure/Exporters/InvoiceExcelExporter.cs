using ClosedXML.Excel;
using InvoiceEntity = Invoice.Domain.Entities.Invoice;

namespace Invoice.Infrastructure.Exporters;

/// <summary>
/// يُولّد ملف Excel للفاتورة باستخدام ClosedXML.
/// </summary>
public static class InvoiceExcelExporter
{
    public static void Export(InvoiceEntity invoice, string filePath)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("الفاتورة");

        // جعل الورقة من اليمين لليسار
        ws.RightToLeft = true;

        int row = 1;

        // ========= الترويسة =========

        var titleCell = ws.Cell(row, 1);
        titleCell.Value = "فاتورة";
        titleCell.Style.Font.FontSize = 20;
        titleCell.Style.Font.Bold = true;
        ws.Range(row, 1, row, 7).Merge();
        titleCell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        row += 2;

        ws.Cell(row, 1).Value = "رقم الفاتورة:";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = invoice.InvoiceNo;
        row++;

        if (!string.IsNullOrWhiteSpace(invoice.SupplierInvoiceNo))
        {
            ws.Cell(row, 1).Value = "رقم فاتورة المورد:";
            ws.Cell(row, 1).Style.Font.Bold = true;
            ws.Cell(row, 2).Value = invoice.SupplierInvoiceNo;
            row++;
        }

        ws.Cell(row, 1).Value = "التاريخ:";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = invoice.InvoiceDate.ToString("yyyy-MM-dd");
        row++;

        ws.Cell(row, 1).Value = "المورد:";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 2).Value = invoice.Supplier?.Name ?? "—";
        row += 2;

        // ========= جدول الأصناف =========

        ws.Cell(row, 1).Value = "الأصناف";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 14;
        row++;

        int headerRow = row;
        string[] headers = { "#", "الصنف", "الكمية", "الشراء $", "البيع د.ل", "إجمالي البيع", "الربح" };
        for (int i = 0; i < headers.Length; i++)
        {
            var cell = ws.Cell(row, i + 1);
            cell.Value = headers[i];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.LightGray;
            cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            cell.Style.Border.BottomBorder = XLBorderStyleValues.Thin;
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

            // تنسيق الأرقام
            ws.Cell(row, 3).Style.NumberFormat.Format = "#,##0.##";
            ws.Cell(row, 4).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 5).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 6).Style.NumberFormat.Format = "#,##0.00";
            ws.Cell(row, 7).Style.NumberFormat.Format = "#,##0.00";

            for (int c = 1; c <= 7; c++)
                ws.Cell(row, c).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;

            row++;
        }
        row++;

        // ========= الملخص =========

        ws.Cell(row, 1).Value = "الملخص";
        ws.Cell(row, 1).Style.Font.Bold = true;
        ws.Cell(row, 1).Style.Font.FontSize = 14;
        row++;

        void SummaryRow(string label, string value, bool bold = false)
        {
            ws.Cell(row, 1).Value = label;
            ws.Cell(row, 1).Style.Font.Bold = bold;
            ws.Cell(row, 2).Value = value;
            ws.Cell(row, 2).Style.Font.Bold = bold;
            ws.Cell(row, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
            row++;
        }

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
        row++;

        // ========= الدفعات والخصومات والمصاريف =========

        var payments = invoice.Payments.OrderBy(p => p.LineNo).ToList();
        if (payments.Count > 0)
        {
            ws.Cell(row, 1).Value = "الدفعات";
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;
            foreach (var p in payments)
            {
                ws.Cell(row, 1).Value = "• " + (p.Label ?? "—");
                ws.Cell(row, 2).Value = $"{p.Amount:N2} د.ل";
                row++;
            }
            row++;
        }

        var discounts = invoice.Discounts.OrderBy(d => d.LineNo).ToList();
        if (discounts.Count > 0)
        {
            ws.Cell(row, 1).Value = "الخصومات";
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;
            foreach (var d in discounts)
            {
                ws.Cell(row, 1).Value = "• " + (d.Label ?? "—");
                ws.Cell(row, 2).Value = $"{d.Amount:N2} د.ل";
                row++;
            }
            row++;
        }

        var expenses = invoice.Expenses.OrderBy(e => e.LineNo).ToList();
        if (expenses.Count > 0)
        {
            ws.Cell(row, 1).Value = "المصاريف";
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;
            foreach (var e in expenses)
            {
                ws.Cell(row, 1).Value = "• " + e.Label;
                ws.Cell(row, 2).Value = $"{e.Amount:N2} د.ل";
                row++;
            }
            row++;
        }

        // ========= الملاحظات =========

        if (!string.IsNullOrWhiteSpace(invoice.Notes))
        {
            ws.Cell(row, 1).Value = "ملاحظات";
            ws.Cell(row, 1).Style.Font.Bold = true;
            row++;
            ws.Cell(row, 1).Value = invoice.Notes;
            ws.Range(row, 1, row, 7).Merge();
            row++;
        }

        // ========= توسيع الأعمدة تلقائيًا =========

        ws.Columns().AdjustToContents();
        // حد أدنى لعرض الأعمدة
        for (int c = 1; c <= 7; c++)
        {
            if (ws.Column(c).Width < 12) ws.Column(c).Width = 12;
        }

        workbook.SaveAs(filePath);
    }
}