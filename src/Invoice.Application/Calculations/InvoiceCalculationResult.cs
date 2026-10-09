namespace Invoice.Application.Calculations;

/// <summary>
/// نتيجة حساب بند واحد في الفاتورة.
/// </summary>
public class InvoiceItemResult
{
    public int ProductId { get; set; }
    public int Pallet { get; set; }
    public decimal Quantity { get; set; }
    public decimal PurchaseUsd { get; set; }
    public decimal PurchaseDzd { get; set; }
    public decimal SaleDzd { get; set; }
    public decimal TotalUsd { get; set; }
    public decimal TotalDzd { get; set; }
    public decimal TotalSale { get; set; }
    public decimal UnitProfit { get; set; }
    public decimal TotalProfit { get; set; }
}

/// <summary>
/// نتيجة حساب الفاتورة كاملة — كل الإجماليات والبنود.
/// </summary>
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
    public List<InvoiceItemResult> Items { get; set; } = new();
}