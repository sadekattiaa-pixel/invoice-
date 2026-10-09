namespace Invoice.Domain.Entities;

/// <summary>
/// إعدادات التطبيق — مخزَّنة كمفاتيح وقيم نصية.
/// تُستخدم لتخزين بيانات الشركة وبادئة أرقام الفواتير وغيرها.
/// </summary>
public class Settings
{
    /// <summary>مفتاح الإعداد (مثال: company_name).</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>قيمة الإعداد (مثال: شركة الانطلاقة).</summary>
    public string? Value { get; set; }

    /// <summary>تاريخ آخر تعديل.</summary>
    public DateTime UpdatedAt { get; set; }
}