using Invoice.Application.Dtos;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace Invoice.Infrastructure.Exporters;

/// <summary>
/// يُولّد PDF لأي تقرير — يعتمد على ReportTableDto كبنية عامة.
/// </summary>
public static class ReportPdfExporter
{
    static ReportPdfExporter()
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
    }

    public static void Export(ReportTableDto report, CompanyInfoDto company, string filePath)
    {
        Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("Arial"));

                // ============ الترويسة: الشعار + اسم الشركة ============
                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        // الشعار (يسار)
                        if (company.LogoBytes != null && company.LogoBytes.Length > 0)
                        {
                            row.ConstantItem(80)
                                .AlignTop().AlignLeft()
                                .Height(60)
                                .Image(company.LogoBytes).FitArea();
                        }
                        else
                        {
                            row.ConstantItem(80);
                        }

                        // اسم الشركة (يمين)
                        row.RelativeItem().AlignTop().AlignRight().Column(c =>
                        {
                            if (!string.IsNullOrWhiteSpace(company.Name))
                                c.Item().Text(company.Name).FontSize(13).Bold();
                        });
                    });

                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor(Colors.Grey.Medium);
                });

                // ============ المحتوى ============
                page.Content().PaddingVertical(8).Column(col =>
                {
                    // عنوان التقرير
                    col.Item().AlignCenter().Text(report.Title).FontSize(16).Bold();

                    if (!string.IsNullOrWhiteSpace(report.Subtitle))
                    {
                        col.Item().PaddingTop(2).AlignCenter()
                            .Text(report.Subtitle).FontSize(10);
                    }

                    col.Item().PaddingTop(10);

                    // جدول
                    if (report.Columns.Count > 0)
                    {
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                foreach (var c in report.Columns)
                                    columns.RelativeColumn(Math.Max(1, c.Weight));
                            });

                            // رأس الجدول
                            table.Header(header =>
                            {
                                foreach (var c in report.Columns)
                                {
                                    var cell = header.Cell()
                                        .Background(Colors.Grey.Lighten2)
                                        .Border(0.5f).BorderColor(Colors.Black)
                                        .Padding(4);

                                    if (c.AlignRight)
                                        cell = cell.AlignRight();
                                    else
                                        cell = cell.AlignCenter();

                                    var txt = cell.Text(c.Header).FontSize(10).Bold();
                                }
                            });

                            // صفوف البيانات
                            foreach (var r in report.Rows)
                            {
                                for (int i = 0; i < report.Columns.Count; i++)
                                {
                                    var colDef = report.Columns[i];
                                    var value = i < r.Values.Count ? r.Values[i] : "";

                                    var cell = table.Cell()
                                        .Border(0.5f).BorderColor(Colors.Black)
                                        .Padding(4);

                                    if (colDef.AlignRight)
                                        cell = cell.AlignRight();
                                    else
                                        cell = cell.AlignCenter();

                                    var txt = cell.Text(value).FontSize(10);
                                    if (colDef.Bold) txt.Bold();
                                }
                            }
                        });
                    }

                    // الملخص
                    if (report.Summary.Count > 0)
                    {
                        col.Item().PaddingTop(14).Text("الملخص").FontSize(13).Bold();

                        col.Item().PaddingTop(6).Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(3);
                            });

                            foreach (var s in report.Summary)
                            {
                                var labelCell = table.Cell().Padding(3)
                                    .Text(s.Label).FontSize(10);
                                if (s.Bold) labelCell.Bold();

                                var valueCell = table.Cell().Padding(3)
                                    .AlignRight().Text(s.Value).FontSize(10);
                                if (s.Bold) valueCell.Bold();
                            }
                        });
                    }
                });

                // ============ الفوتر ============
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