namespace Invoice.Application.Dtos;

/// <summary>
/// صف واحد في جدول تقرير — قائمة نصوص بنفس ترتيب الأعمدة.
/// </summary>
public class ReportRow
{
    public List<string> Values { get; set; } = new();
}