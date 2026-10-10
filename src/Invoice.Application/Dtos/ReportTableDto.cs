namespace Invoice.Application.Dtos;

/// <summary>
/// حاوية عامة لبيانات أي تقرير — تُستخدم لتوليد PDF و Excel بنفس البنية.
/// </summary>
public class ReportTableDto
{
    /// <summary>عنوان التقرير (مثلاً: "تقرير الأرباح").</summary>
    public string Title { get; set; } = "";

    /// <summary>سطر وصفي تحت العنوان (مثلاً: "الفترة: 2026-01-01 إلى 2026-12-31").</summary>
    public string? Subtitle { get; set; }

    /// <summary>أعمدة الجدول.</summary>
    public List<ReportColumn> Columns { get; set; } = new();

    /// <summary>صفوف البيانات.</summary>
    public List<ReportRow> Rows { get; set; } = new();

    /// <summary>عناصر الملخص (تظهر أسفل الجدول).</summary>
    public List<ReportSummaryItem> Summary { get; set; } = new();

    /// <summary>مساعد: يضيف صفًا سريعًا بقائمة قيم نصية.</summary>
    public void AddRow(params string[] values)
    {
        Rows.Add(new ReportRow { Values = values.ToList() });
    }

    /// <summary>مساعد: يضيف عنصرًا في الملخص.</summary>
    public void AddSummary(string label, string value, bool bold = false)
    {
        Summary.Add(new ReportSummaryItem { Label = label, Value = value, Bold = bold });
    }
}