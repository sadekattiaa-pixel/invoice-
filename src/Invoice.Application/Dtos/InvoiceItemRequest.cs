namespace Invoice.Application.Dtos;

/// <summary>
/// بند واحد في طلب حفظ الفاتورة.
/// </summary>
public class InvoiceItemRequest
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal PurchaseUsd { get; set; }
    public decimal SaleDzd { get; set; }
    public int? Pallet { get; set; }
}