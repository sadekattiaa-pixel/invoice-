using Invoice.Application.Exceptions;

namespace Invoice.Application.Calculations;

/// <summary>
/// محرك حساب الفواتير.
/// 
/// قواعد الحساب مطابقة للنسخة الأصلية (Python):
/// - التقريب: ROUND_HALF_UP (MidpointRounding.AwayFromZero).
/// - الدقة: Money = 0.01, Rate = 0.000001, Percent = 0.0001.
/// 
/// الصيغ:
/// - net_payments  = total_payments − total_discounts
/// - net_cost      = net_payments + total_expenses
/// - exchange_rate = net_cost / total_usd
/// - purchase_dzd  = purchase_usd × exchange_rate
/// - unit_profit   = sale_dzd − purchase_dzd
/// - total_profit  = quantity × unit_profit
/// - profit_margin = (total_profit / total_sales) × 100
/// </summary>
public static class InvoiceCalculator
{
    /// <summary>دقة المبالغ المالية: منزلتان عشريتان.</summary>
    public const int MoneyDecimals = 2;

    /// <summary>دقة سعر الصرف: 6 منازل عشرية.</summary>
    public const int RateDecimals = 6;

    /// <summary>دقة النسبة المئوية: 4 منازل عشرية.</summary>
    public const int PercentDecimals = 4;

    /// <summary>
    /// يحسب كل قيم الفاتورة.
    /// </summary>
    /// <exception cref="DomainException">عند انتهاك قاعدة عمل أو بيانات غير صالحة.</exception>
    public static InvoiceCalculationResult Calculate(
        IReadOnlyList<InvoiceItemInput> items,
        IReadOnlyList<decimal> payments,
        IReadOnlyList<decimal> discounts,
        IReadOnlyList<decimal> expenses)
    {
        // ========= التحقق الأساسي =========

        if (items == null || items.Count == 0)
        {
            throw new DomainException("يجب إضافة صنف واحد على الأقل.");
        }

        if (payments == null) payments = Array.Empty<decimal>();
        if (discounts == null) discounts = Array.Empty<decimal>();
        if (expenses == null) expenses = Array.Empty<decimal>();

        // المدفوعات والخصومات والمصاريف لا يمكن أن تكون سالبة
        if (payments.Any(v => v < 0m))
            throw new DomainException("المدفوعات لا يمكن أن تكون سالبة.");
        if (discounts.Any(v => v < 0m))
            throw new DomainException("الخصومات لا يمكن أن تكون سالبة.");
        if (expenses.Any(v => v < 0m))
            throw new DomainException("المصاريف لا يمكن أن تكون سالبة.");

        // ========= الإجماليات الأولية =========

        decimal totalPayments = payments.Sum();
        decimal totalDiscounts = discounts.Sum();
        decimal totalExpenses = expenses.Sum();

        if (totalDiscounts > totalPayments)
        {
            throw new DomainException("إجمالي الخصومات لا يمكن أن يتجاوز إجمالي المدفوعات.");
        }

        decimal netPayments = totalPayments - totalDiscounts;
        decimal netCost = netPayments + totalExpenses;

        // ========= التحقق من البنود =========

        foreach (var it in items)
        {
            if (it.Quantity <= 0m)
                throw new DomainException("الكمية يجب أن تكون أكبر من صفر.");
            if (it.PurchaseUsd <= 0m)
                throw new DomainException("سعر الشراء يجب أن يكون أكبر من صفر.");
            if (it.SaleDzd <= 0m)
                throw new DomainException("سعر البيع يجب أن يكون أكبر من صفر.");
        }

        // ========= حساب total_usd =========

        decimal totalUsd = items.Sum(it => it.Quantity * it.PurchaseUsd);
        if (totalUsd <= 0m)
        {
            throw new DomainException("القيمة الإجمالية بالدولار يجب أن تكون أكبر من صفر.");
        }

        // ========= حساب سعر الصرف =========

        decimal exchangeRate = netCost / totalUsd;

        // ========= حساب البنود =========

        var resultItems = new List<InvoiceItemResult>();
        decimal totalSales = 0m;
        decimal totalProfit = 0m;

        foreach (var it in items)
        {
            // الحساب بالدقة الكاملة أولًا
            decimal purchaseDzdRaw = it.PurchaseUsd * exchangeRate;
            decimal itemTotalUsdRaw = it.Quantity * it.PurchaseUsd;
            decimal itemTotalDzdRaw = it.Quantity * purchaseDzdRaw;
            decimal itemTotalSaleRaw = it.Quantity * it.SaleDzd;
            decimal unitProfitRaw = it.SaleDzd - purchaseDzdRaw;
            decimal itemTotalProfitRaw = it.Quantity * unitProfitRaw;

            // تقريب للمخرجات النهائية
            resultItems.Add(new InvoiceItemResult
            {
                ProductId = it.ProductId,
                Pallet = it.Pallet ?? 0,
                Quantity = it.Quantity,
                PurchaseUsd = it.PurchaseUsd,
                PurchaseDzd = RoundMoney(purchaseDzdRaw),
                SaleDzd = it.SaleDzd,
                TotalUsd = RoundMoney(itemTotalUsdRaw),
                TotalDzd = RoundMoney(itemTotalDzdRaw),
                TotalSale = RoundMoney(itemTotalSaleRaw),
                UnitProfit = RoundMoney(unitProfitRaw),
                TotalProfit = RoundMoney(itemTotalProfitRaw)
            });

            // المجموع يُبنى من القيم غير المقربة لدقة أعلى
            totalSales += itemTotalSaleRaw;
            totalProfit += itemTotalProfitRaw;
        }

        // ========= هامش الربح =========

        decimal profitMargin = totalSales > 0m
            ? (totalProfit / totalSales) * 100m
            : 0m;

        // ========= النتيجة النهائية =========

        return new InvoiceCalculationResult
        {
            TotalPayments = RoundMoney(totalPayments),
            TotalDiscounts = RoundMoney(totalDiscounts),
            NetPayments = RoundMoney(netPayments),
            TotalExpenses = RoundMoney(totalExpenses),
            NetCost = RoundMoney(netCost),
            TotalUsd = RoundMoney(totalUsd),
            ExchangeRate = RoundRate(exchangeRate),
            TotalSales = RoundMoney(totalSales),
            TotalProfit = RoundMoney(totalProfit),
            ProfitMargin = RoundPercent(profitMargin),
            Items = resultItems
        };
    }

    // ========= دوال التقريب =========

    /// <summary>تقريب المبالغ المالية (منزلتان) بنمط ROUND_HALF_UP.</summary>
    public static decimal RoundMoney(decimal value)
        => Math.Round(value, MoneyDecimals, MidpointRounding.AwayFromZero);

    /// <summary>تقريب سعر الصرف (6 منازل) بنمط ROUND_HALF_UP.</summary>
    public static decimal RoundRate(decimal value)
        => Math.Round(value, RateDecimals, MidpointRounding.AwayFromZero);

    /// <summary>تقريب النسبة المئوية (4 منازل) بنمط ROUND_HALF_UP.</summary>
    public static decimal RoundPercent(decimal value)
        => Math.Round(value, PercentDecimals, MidpointRounding.AwayFromZero);
}