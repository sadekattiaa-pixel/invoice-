using ClosedXML.Excel;
using Invoice.Application.Dtos;

namespace Invoice.Infrastructure.Exporters;

/// <summary>
/// يُولّد Excel لأي تقرير — يعتمد على ReportTableDto كبنية عامة.
/// </summary>
public static class ReportExcelExporter
{
    public static void Export(ReportTableDto report, CompanyInfoDto company, string filePath)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("التقرير");
        ws.RightToLeft = true;

        int colCount = Math.Max(1, report.Columns.Count);
        int row = 1;

        // ============ ترويسة الشركة ============

        if (company.LogoBytes != null && company.LogoBytes.Length > 0)
        {
            ws.Row(row).Height = 45;
            ws.Range(row, 1, row, colCount).Merge();

            using var ms = new MemoryStream(company.LogoBytes);
            var picture = ws.AddPicture(ms);
            picture.Name = "CompanyLogo";
            picture.MoveTo(ws.Cell(row, 1));
            picture.WithSize(100, 55);

            row++;
        }

        if (!string.IsNullOrWhiteSpace(company.Name))
        {
            var r = ws.Range(row, 1, row, colCount);
            r.Merge();
            r.Value = company.Name;
            r.Style.Font.Bold = true;
            r.Style.Font.FontSize = 14;
            r.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            row++;
        }

        if (!string.IsNullOrWhiteSpace(company.Phone)
            || !string.IsNullOrWhiteSpace(company.Email))
        {
            var contactParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(company.Phone))
                contactParts.Add(company.Phone);
            if (!string.IsNullOrWhiteSpace(company.Email))
                contactParts.Add(company.Email);

            var r = ws.Range(row, 1, row, colCount);
            r.Merge();
            r.Value = string.Join("   |   ", contactParts);
            r.Style.Font.FontSize = 9;
            r.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            row++;
        }

        row++; // فراغ

        // ============ عنوان التقرير ============

        var titleR = ws.Range(row, 1, row, colCount);
        titleR.Merge();
        titleR.Value = report.Title;
        titleR.Style.Font.Bold = true;
        titleR.Style.Font.FontSize = 16;
        titleR.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        row++;

        if (!string.IsNullOrWhiteSpace(report.Subtitle))
        {
            var subR = ws.Range(row, 1, row, colCount);
            subR.Merge();
            subR.Value = report.Subtitle;
            subR.Style.Font.FontSize = 10;
            subR.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            row++;
        }

        row++; // فراغ

        // ============ جدول التقرير ============

        if (report.Columns.Count > 0)
        {
            int tableStartRow = row;

            // رأس الجدول
            for (int i = 0; i < report.Columns.Count; i++)
            {
                var colDef = report.Columns[i];
                var cell = ws.Cell(row, i + 1);
                cell.Value = colDef.Header;
                cell.Style.Font.Bold = true;
                cell.Style.Fill.BackgroundColor = XLColor.LightGray;
                cell.Style.Alignment.Horizontal = colDef.AlignRight
                    ? XLAlignmentHorizontalValues.Right
                    : XLAlignmentHorizontalValues.Center;
                cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                cell.Style.Border.OutsideBorderColor = XLColor.Black;
            }
            row++;

            // صفوف البيانات
            foreach (var r in report.Rows)
            {
                for (int i = 0; i < report.Columns.Count; i++)
                {
                    var colDef = report.Columns[i];
                    var value = i < r.Values.Count ? r.Values[i] : "";

                    var cell = ws.Cell(row, i + 1);
                    cell.Value = value;
                    cell.Style.Alignment.Horizontal = colDef.AlignRight
                        ? XLAlignmentHorizontalValues.Right
                        : XLAlignmentHorizontalValues.Center;
                    cell.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.Black;

                    if (colDef.Bold) cell.Style.Font.Bold = true;
                }
                row++;
            }

            int tableEndRow = row - 1;
            var tableRange = ws.Range(tableStartRow, 1, tableEndRow, report.Columns.Count);
            tableRange.Style.Border.OutsideBorder = XLBorderStyleValues.Medium;
            tableRange.Style.Border.OutsideBorderColor = XLColor.Black;
            tableRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            tableRange.Style.Border.InsideBorderColor = XLColor.Gray;
        }

        row++; // فراغ

        // ============ الملخص ============

        if (report.Summary.Count > 0)
        {
            var sTitle = ws.Range(row, 1, row, colCount);
            sTitle.Merge();
            sTitle.Value = "الملخص";
            sTitle.Style.Font.Bold = true;
            sTitle.Style.Font.FontSize = 13;
            sTitle.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
            row++;

            foreach (var s in report.Summary)
            {
                int splitAt = Math.Max(1, colCount / 2);

                var rLabel = ws.Range(row, 1, row, splitAt);
                rLabel.Merge();
                rLabel.Value = s.Label;
                rLabel.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
                if (s.Bold) rLabel.Style.Font.Bold = true;

                var rValue = ws.Range(row, splitAt + 1, row, colCount);
                rValue.Merge();
                rValue.Value = s.Value;
                rValue.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;
                if (s.Bold) rValue.Style.Font.Bold = true;

                row++;
            }
        }

        // ============ توسيع الأعمدة ============
        ws.Columns().AdjustToContents();
        for (int c = 1; c <= colCount; c++)
        {
            if (ws.Column(c).Width < 12) ws.Column(c).Width = 12;
        }

        workbook.SaveAs(filePath);
    }
}