namespace Invoice.Domain.Entities;

/// <summary>
/// المورد — يمثل شركة أو شخصًا نشتري منه.
/// </summary>
public class Supplier
{
    /// <summary>المعرّف الفريد (يتولّد تلقائيًا في قاعدة البيانات).</summary>
    public int Id { get; set; }

    /// <summary>كود المورد (مثال: S-001). فريد لكل مورد.</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>اسم المورد.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>العنوان (اختياري).</summary>
    public string? Address { get; set; }

    /// <summary>البريد الإلكتروني (اختياري).</summary>
    public string? Email { get; set; }

    /// <summary>رقم الهاتف (اختياري).</summary>
    public string? Phone { get; set; }

    /// <summary>تاريخ الإنشاء (يُضبط تلقائيًا).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>تاريخ آخر تعديل (اختياري).</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>رقم النسخة — يُستخدم لمنع تعارض التعديلات بين المستخدمين.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}