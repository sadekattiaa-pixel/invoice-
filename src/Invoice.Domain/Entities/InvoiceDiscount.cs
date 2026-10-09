namespace Invoice.Domain.Entities;

/// <summary>
/// خصم على الفاتورة (قد يكون هناك عدة خصومات في الفاتورة الواحدة).
/// </summary>
public class InvoiceDiscount
{
    /// <summary>المعرّف الفريد.</summary>
    public long Id { get; set; }

    /// <summary>معرّف الفاتورة.</summary>
    public long InvoiceId { get; set; }

    /// <summary>مرجع الفاتورة.</summary>
    public Invoice? Invoice { get; set; }

    /// <summary>وصف الخصم (مثال: خصم تعاقد).</summary>
    public string? Label { get; set; }

    /// <summary>قيمة الخصم بالدينار.</summary>
    public decimal Amount { get; set; }

    /// <summary>ترتيب الخصم داخل الفاتورة.</summary>
    public int LineNo { get; set; }
}