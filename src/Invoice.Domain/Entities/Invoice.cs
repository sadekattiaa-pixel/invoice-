namespace Invoice.Domain.Entities;

/// <summary>
/// الفاتورة — الوحدة الأساسية للعمل.
/// </summary>
public class Invoice
{
    /// <summary>المعرّف الفريد.</summary>
    public long Id { get; set; }

    /// <summary>رقم الفاتورة الداخلي (فريد، مثل INV-2026-0001).</summary>
    public string InvoiceNo { get; set; } = string.Empty;

    /// <summary>رقم الفاتورة عند المورد (اختياري).</summary>
    public string? SupplierInvoiceNo { get; set; }

    /// <summary>معرّف المورد.</summary>
    public int SupplierId { get; set; }

    /// <summary>مرجع كامل للمورد.</summary>
    public Supplier? Supplier { get; set; }

    /// <summary>تاريخ الفاتورة.</summary>
    public DateOnly InvoiceDate { get; set; }

    /// <summary>الحالة: Draft أو Archived.</summary>
    public string Status { get; set; } = "Draft";

    // ========= الإجماليات =========

    /// <summary>إجمالي المدفوعات.</summary>
    public decimal TotalPayments { get; set; }

    /// <summary>إجمالي الخصومات.</summary>
    public decimal TotalDiscounts { get; set; }

    /// <summary>صافي المدفوعات = المدفوعات − الخصومات.</summary>
    public decimal NetPayments { get; set; }

    /// <summary>إجمالي المصاريف الإضافية.</summary>
    public decimal TotalExpenses { get; set; }

    /// <summary>صافي التكلفة = صافي المدفوعات + المصاريف.</summary>
    public decimal NetCost { get; set; }

    /// <summary>القيمة الإجمالية بالدولار.</summary>
    public decimal TotalUsd { get; set; }

    /// <summary>سعر الصرف المحسوب.</summary>
    public decimal ExchangeRate { get; set; }

    /// <summary>إجمالي البيع.</summary>
    public decimal TotalSales { get; set; }

    /// <summary>إجمالي الربح.</summary>
    public decimal TotalProfit { get; set; }

    /// <summary>هامش الربح %.</summary>
    public decimal ProfitMargin { get; set; }

    // ========= بيانات إضافية =========

    /// <summary>حقول عرض اختيارية (JSON).</summary>
    public string? DisplayFields { get; set; }

    /// <summary>ملاحظة أولى.</summary>
    public string? Notes { get; set; }

    /// <summary>ملاحظة ثانية.</summary>
    public string? Notes2 { get; set; }

    /// <summary>تاريخ الأرشفة (فارغ إذا كانت مسودة).</summary>
    public DateTime? ArchivedAt { get; set; }

    /// <summary>تاريخ الإنشاء.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>تاريخ آخر تعديل.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>معرّف المستخدم الذي أنشأ الفاتورة.</summary>
    public int? CreatedById { get; set; }

    /// <summary>رقم النسخة لمنع تعارض التعديلات.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // ========= العلاقات =========

    /// <summary>بنود الأصناف في الفاتورة.</summary>
    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();

    /// <summary>المدفوعات.</summary>
    public ICollection<InvoicePayment> Payments { get; set; } = new List<InvoicePayment>();

    /// <summary>الخصومات.</summary>
    public ICollection<InvoiceDiscount> Discounts { get; set; } = new List<InvoiceDiscount>();

    /// <summary>المصاريف الإضافية.</summary>
    public ICollection<InvoiceExpense> Expenses { get; set; } = new List<InvoiceExpense>();
}