namespace Invoice.Desktop.Models;

/// <summary>
/// نماذج البيانات الخاصة بالفاتورة — تُطابق ما يتوقعه/يُرجعه الـ API.
/// </summary>

// ============================================================
// Request DTOs (ما نُرسله للـ API)
// ============================================================

/// <summary>طلب حفظ/حساب فاتورة (كامل).</summary>
public class InvoiceSaveRequest
{
    public int SupplierId { get; set; }
    public string? SupplierInvoiceNo { get; set; }
    public DateOnly InvoiceDate { get; set; } = DateOnly.FromDateTime(DateTime.Now);
    public List<InvoiceItemRequest> Items { get; set; } = new();
    public List<InvoiceAmountRequest> Payments { get; set; } = new();
    public List<InvoiceAmountRequest> Discounts { get; set; } = new();
    public List<InvoiceAmountRequest> Expenses { get; set; } = new();
    public string? Notes { get; set; }
    public string? Notes2 { get; set; }
}

/// <summary>بند فاتورة — صنف + كمية + شراء$ + بيع د.ل.</summary>
public class InvoiceItemRequest
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal PurchaseUsd { get; set; }
    public decimal SaleDzd { get; set; }
    public int? Pallet { get; set; }
}

/// <summary>عنصر مبلغ (دفعة/خصم/مصروف) — label + amount.</summary>
public class InvoiceAmountRequest
{
    public string? Label { get; set; }
    public decimal Amount { get; set; }
}

// ============================================================
// Response DTOs (ما يرجعه الـ API)
// ============================================================

/// <summary>ناتج حساب المعاينة (POST /api/invoices/calculate).</summary>
public class InvoiceCalculationResult
{
    public decimal TotalPayments { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal NetPayments { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetCost { get; set; }
    public decimal TotalUsd { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal ProfitMargin { get; set; }
    public List<CalculatedItem> Items { get; set; } = new();
}

/// <summary>بند محسوب (ناتج الحساب).</summary>
public class CalculatedItem
{
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public int? Pallet { get; set; }
    public decimal PurchaseUsd { get; set; }
    public decimal PurchaseDzd { get; set; }
    public decimal SaleDzd { get; set; }
    public decimal TotalUsd { get; set; }
    public decimal TotalDzd { get; set; }
    public decimal TotalSale { get; set; }
    public decimal UnitProfit { get; set; }
    public decimal TotalProfit { get; set; }
}

/// <summary>استجابة حفظ/أرشفة فاتورة.</summary>
public class InvoiceSaveResponse
{
    public long Id { get; set; }
    public string InvoiceNo { get; set; } = "";
    public string Status { get; set; } = "";
    public decimal? TotalProfit { get; set; }
    public string? Message { get; set; }
}

/// <summary>فاتورة كاملة (GET /api/invoices/{id}).</summary>
public class InvoiceDetail
{
    public long Id { get; set; }
    public string InvoiceNo { get; set; } = "";
    public string? SupplierInvoiceNo { get; set; }
    public int SupplierId { get; set; }
    public SupplierDto? Supplier { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public string Status { get; set; } = "";
    public decimal TotalPayments { get; set; }
    public decimal TotalDiscounts { get; set; }
    public decimal NetPayments { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetCost { get; set; }
    public decimal TotalUsd { get; set; }
    public decimal ExchangeRate { get; set; }
    public decimal TotalSales { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal ProfitMargin { get; set; }
    public string? Notes { get; set; }
    public string? Notes2 { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public List<InvoiceItemDto> Items { get; set; } = new();
    public List<InvoiceAmountDto> Payments { get; set; } = new();
    public List<InvoiceAmountDto> Discounts { get; set; } = new();
    public List<InvoiceAmountDto> Expenses { get; set; } = new();
}

/// <summary>مورد مختصر داخل فاتورة.</summary>
public class SupplierDto
{
    public int Id { get; set; }
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
}

/// <summary>بند فاتورة كامل (من الـ API).</summary>
public class InvoiceItemDto
{
    public long Id { get; set; }
    public int ProductId { get; set; }
    public string? ProductCode { get; set; }
    public string? ProductName { get; set; }
    public string? ProductCategory { get; set; }
    public string? ProductPattern { get; set; }
    public string? ProductImage { get; set; }
    public int LineNo { get; set; }
    public decimal Quantity { get; set; }
    public int? Pallet { get; set; }
    public decimal PurchaseUsd { get; set; }
    public decimal PurchaseDzd { get; set; }
    public decimal SaleDzd { get; set; }
    public decimal TotalUsd { get; set; }
    public decimal TotalDzd { get; set; }
    public decimal TotalSale { get; set; }
    public decimal UnitProfit { get; set; }
    public decimal TotalProfit { get; set; }
}

/// <summary>عنصر مبلغ (دفعة/خصم/مصروف) من الـ API.</summary>
public class InvoiceAmountDto
{
    public long Id { get; set; }
    public string? Label { get; set; }
    public decimal Amount { get; set; }
    public int LineNo { get; set; }
}

/// <summary>ملخص فاتورة في القائمة (GET /api/invoices).</summary>
public class InvoiceListRow
{
    public long Id { get; set; }
    public string InvoiceNo { get; set; } = "";
    public string? SupplierInvoiceNo { get; set; }
    public DateOnly InvoiceDate { get; set; }
    public string Status { get; set; } = "";
    public decimal TotalPayments { get; set; }
    public decimal TotalUsd { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal NetCost { get; set; }
    public decimal ProfitMargin { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public int SupplierId { get; set; }
    public string? SupplierName { get; set; }
    public decimal TotalQty { get; set; }
}

/// <summary>استجابة الحصول على الرقم التالي.</summary>
public class NextNumberResponse
{
    public string InvoiceNo { get; set; } = "";
}