namespace Invoice.Application.Dtos;

/// <summary>
/// عنصر مبلغ (دفعة / خصم / مصروف) في طلب حفظ الفاتورة.
/// </summary>
public class InvoiceAmountRequest
{
    public string? Label { get; set; }
    public decimal Amount { get; set; }
}