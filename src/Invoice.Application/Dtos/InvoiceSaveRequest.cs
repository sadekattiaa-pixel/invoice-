namespace Invoice.Application.Dtos;

/// <summary>
/// الطلب الكامل لحفظ فاتورة (إنشاء أو تعديل مسودة).
/// </summary>
public class InvoiceSaveRequest
{
    public int SupplierId { get; set; }
    public string? SupplierInvoiceNo { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public List<InvoiceItemRequest> Items { get; set; } = new();
    public List<InvoiceAmountRequest> Payments { get; set; } = new();
    public List<InvoiceAmountRequest> Discounts { get; set; } = new();
    public List<InvoiceAmountRequest> Expenses { get; set; } = new();
    public string? Notes { get; set; }
    public string? Notes2 { get; set; }
}