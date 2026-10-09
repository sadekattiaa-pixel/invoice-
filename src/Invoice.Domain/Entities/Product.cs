namespace Invoice.Domain.Entities;

/// <summary>
/// الصنف — بطارية أو إطار.
/// </summary>
public class Product
{
    /// <summary>المعرّف الفريد.</summary>
    public int Id { get; set; }

    /// <summary>كود الصنف (فريد).</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>اسم الصنف.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>التصنيف: Batteries / Car Tires / Truck Tires.</summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>النقشة (للإطارات فقط، فارغ للبطاريات).</summary>
    public string? Pattern { get; set; }

    /// <summary>سعة البطارية (AH).</summary>
    public decimal? Ah { get; set; }

    /// <summary>الطول (L).</summary>
    public decimal? L { get; set; }

    /// <summary>العرض (W).</summary>
    public decimal? W { get; set; }

    /// <summary>الارتفاع (H).</summary>
    public decimal? H { get; set; }

    /// <summary>الارتفاع الكلي (OH).</summary>
    public decimal? Oh { get; set; }

    /// <summary>رمز التخطيط.</summary>
    public int? Layout { get; set; }

    /// <summary>اسم ملف الصورة (اختياري).</summary>
    public string? LayoutImageFilename { get; set; }

    /// <summary>عدد الطبالي.</summary>
    public int? Pallet { get; set; }

    /// <summary>معرّف المورد المالك.</summary>
    public int SupplierId { get; set; }

    /// <summary>مرجع كامل للمورد (يُحمَّل عند الحاجة).</summary>
    public Supplier? Supplier { get; set; }

    /// <summary>تاريخ الإنشاء.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>تاريخ آخر تعديل.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>رقم النسخة لمنع تعارض التعديلات.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}