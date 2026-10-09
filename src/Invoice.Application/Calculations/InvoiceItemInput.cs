namespace Invoice.Application.Calculations;

/// <summary>
/// مدخلات بند واحد في الفاتورة (قبل الحساب).
/// </summary>
public class InvoiceItemInput
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal PurchaseUsd { get; set; }
    public decimal SaleDzd { get; set; }
    public int? Pallet { get; set; }
}