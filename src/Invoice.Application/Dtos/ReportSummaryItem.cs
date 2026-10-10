namespace Invoice.Application.Dtos;

/// <summary>
/// عنصر في قسم "الملخص" أسفل جدول التقرير (مثل: الإجمالي = ...).
/// </summary>
public class ReportSummaryItem
{
    public string Label { get; set; } = "";
    public string Value { get; set; } = "";
    public bool Bold { get; set; } = false;
}