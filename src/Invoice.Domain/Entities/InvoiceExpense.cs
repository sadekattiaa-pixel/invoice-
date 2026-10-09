namespace Invoice.Domain.Entities;

/// <summary>
/// مصروف إضافي على الفاتورة (مثل: جمرك، نقل، شحن).
/// </summary>
public class InvoiceExpense
{
    /// <summary>المعرّف الفريد.</summary>
    public long Id { get; set; }

    /// <summary>معرّف الفاتورة.</summary>
    public long InvoiceId { get; set; }

    /// <summary>مرجع الفاتورة.</summary>
    public Invoice? Invoice { get; set; }

    /// <summary>وصف المصروف (مثال: جمرك).</summary>
    public string Label { get; set; } = string.Empty;

    /// <summary>قيمة المصروف بالدينار.</summary>
    public decimal Amount { get; set; }

    /// <summary>ترتيب المصروف داخل الفاتورة.</summary>
    public int LineNo { get; set; }
}