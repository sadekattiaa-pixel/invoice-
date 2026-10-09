namespace Invoice.Domain.Entities;

/// <summary>
/// بند صنف داخل الفاتورة — سطر واحد في جدول الأصناف.
/// </summary>
public class InvoiceItem
{
    /// <summary>المعرّف الفريد.</summary>
    public long Id { get; set; }

    /// <summary>معرّف الفاتورة الحاوية.</summary>
    public long InvoiceId { get; set; }

    /// <summary>مرجع الفاتورة.</summary>
    public Invoice? Invoice { get; set; }

    /// <summary>معرّف الصنف.</summary>
    public int ProductId { get; set; }

    /// <summary>مرجع الصنف.</summary>
    public Product? Product { get; set; }

    /// <summary>ترتيب السطر داخل الفاتورة.</summary>
    public int LineNo { get; set; }

    /// <summary>الكمية.</summary>
    public decimal Quantity { get; set; }

    /// <summary>عدد الطبالي (اختياري).</summary>
    public int? Pallet { get; set; }

    /// <summary>سعر الشراء بالدولار للوحدة.</summary>
    public decimal PurchaseUsd { get; set; }

    /// <summary>سعر الشراء بالدينار للوحدة (محسوب).</summary>
    public decimal PurchaseDzd { get; set; }

    /// <summary>سعر البيع بالدينار للوحدة.</summary>
    public decimal SaleDzd { get; set; }

    /// <summary>إجمالي القيمة بالدولار = الكمية × سعر الشراء $.</summary>
    public decimal TotalUsd { get; set; }

    /// <summary>إجمالي القيمة بالدينار = الكمية × سعر الشراء د.ل.</summary>
    public decimal TotalDzd { get; set; }

    /// <summary>إجمالي البيع = الكمية × سعر البيع.</summary>
    public decimal TotalSale { get; set; }

    /// <summary>ربح الوحدة = سعر البيع − سعر الشراء د.ل.</summary>
    public decimal UnitProfit { get; set; }

    /// <summary>إجمالي الربح = الكمية × ربح الوحدة.</summary>
    public decimal TotalProfit { get; set; }
}