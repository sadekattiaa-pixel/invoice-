namespace Invoice.Domain.Entities;

/// <summary>
/// دفعة مالية داخل الفاتورة (قد تكون هناك عدة دفعات في الفاتورة الواحدة).
/// </summary>
public class InvoicePayment
{
    /// <summary>المعرّف الفريد.</summary>
    public long Id { get; set; }

    /// <summary>معرّف الفاتورة.</summary>
    public long InvoiceId { get; set; }

    /// <summary>مرجع الفاتورة.</summary>
    public Invoice? Invoice { get; set; }

    /// <summary>وصف الدفعة (مثال: دفعة أولى).</summary>
    public string? Label { get; set; }

    /// <summary>المبلغ بالدينار.</summary>
    public decimal Amount { get; set; }

    /// <summary>ترتيب الدفعة داخل الفاتورة.</summary>
    public int LineNo { get; set; }
}