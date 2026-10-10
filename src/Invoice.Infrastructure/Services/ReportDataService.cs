using Invoice.Application.Dtos;
using Invoice.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Invoice.Infrastructure.Services;

/// <summary>
/// يبني ReportTableDto لكل تقرير — يكرّر منطق ReportsController
/// لكن في صورة جاهزة للتصدير (PDF/Excel).
/// </summary>
public class ReportDataService
{
    private readonly AppDbContext _db;

    public ReportDataService(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>يبني تقريرًا حسب النوع المطلوب. يُعيد null إن كان النوع غير معروف أو نقص معامل مطلوب.</summary>
    public async Task<ReportTableDto?> BuildAsync(
        string type,
        string period = "year",
        int? supplierId = null,
        int? productId = null,
        string? category = null,
        int topN = 10,
        DateOnly? dateFrom = null,
        DateOnly? dateTo = null)
    {
        return type.ToLowerInvariant() switch
        {
            "dashboard" => await BuildDashboardAsync(period),
            "supplier" => supplierId.HasValue ? await BuildSupplierAsync(supplierId.Value, period) : null,
            "profit" => await BuildProfitAsync(period, supplierId),
            "period" => (dateFrom.HasValue && dateTo.HasValue)
                ? await BuildPeriodAsync(dateFrom.Value, dateTo.Value, supplierId) : null,
            "price-changes" => productId.HasValue ? await BuildPriceChangesAsync(productId.Value, period) : null,
            "most-profitable" => await BuildMostProfitableAsync(period, category, topN),
            "best-selling" => await BuildBestSellingAsync(period, category, topN),
            _ => null
        };
    }

    // ============================================================
    // 1) Dashboard
    // ============================================================
    private async Task<ReportTableDto> BuildDashboardAsync(string period)
    {
        var (from, to) = GetPeriodRange(period);

        var baseQuery = _db.Invoices
            .Where(i => i.Status == "Archived" && i.InvoiceDate >= from && i.InvoiceDate <= to);

        var totalInvoices = await baseQuery.CountAsync();
        var totalPayments = await baseQuery.SumAsync(i => (decimal?)i.TotalPayments) ?? 0m;
        var totalUsd = await baseQuery.SumAsync(i => (decimal?)i.TotalUsd) ?? 0m;
        var totalProfit = await baseQuery.SumAsync(i => (decimal?)i.TotalProfit) ?? 0m;
        var totalSales = await baseQuery.SumAsync(i => (decimal?)i.TotalSales) ?? 0m;
        var drafts = await _db.Invoices.CountAsync(i => i.Status == "Draft");
        var suppliers = await _db.Suppliers.CountAsync();
        var products = await _db.Products.CountAsync();

        var report = new ReportTableDto
        {
            Title = "التقرير العام",
            Subtitle = $"الفترة: {from:yyyy-MM-dd} إلى {to:yyyy-MM-dd}"
        };

        report.Columns.Add(new ReportColumn { Header = "البند", AlignRight = true, Weight = 3 });
        report.Columns.Add(new ReportColumn { Header = "القيمة", AlignRight = true, Weight = 2 });

        report.AddRow("عدد الفواتير المؤرشفة", totalInvoices.ToString("N0"));
        report.AddRow("عدد المسودات", drafts.ToString("N0"));
        report.AddRow("عدد الموردين", suppliers.ToString("N0"));
        report.AddRow("عدد الأصناف", products.ToString("N0"));

        report.AddSummary("إجمالي المدفوعات", $"{totalPayments:N2} د.ل");
        report.AddSummary("إجمالي المبيعات", $"{totalSales:N2} د.ل");
        report.AddSummary("القيمة بالدولار", $"$ {totalUsd:N2}");
        report.AddSummary("إجمالي الربح", $"{totalProfit:N2} د.ل", bold: true);

        return report;
    }

    // ============================================================
    // 2) تقرير المورد
    // ============================================================
    private async Task<ReportTableDto?> BuildSupplierAsync(int supplierId, string period)
    {
        var supplier = await _db.Suppliers.FindAsync(supplierId);
        if (supplier == null) return null;

        var (from, to) = GetPeriodRange(period);

        var invoicesQuery = _db.Invoices
            .Where(i => i.SupplierId == supplierId
                        && i.Status == "Archived"
                        && i.InvoiceDate >= from
                        && i.InvoiceDate <= to);

        var invoicesCount = await invoicesQuery.CountAsync();
        var totalPayments = await invoicesQuery.SumAsync(i => (decimal?)i.TotalPayments) ?? 0m;
        var totalUsd = await invoicesQuery.SumAsync(i => (decimal?)i.TotalUsd) ?? 0m;
        var totalProfit = await invoicesQuery.SumAsync(i => (decimal?)i.TotalProfit) ?? 0m;

        var invoices = await invoicesQuery
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .Select(i => new
            {
                i.InvoiceNo,
                i.SupplierInvoiceNo,
                i.InvoiceDate,
                i.TotalUsd,
                i.NetCost,
                i.TotalProfit,
                TotalQty = i.Items.Sum(it => (decimal?)it.Quantity) ?? 0m
            })
            .ToListAsync();

        var report = new ReportTableDto
        {
            Title = $"تقرير المورد: {supplier.Name}",
            Subtitle = $"الفترة: {from:yyyy-MM-dd} إلى {to:yyyy-MM-dd}"
        };

        report.Columns.Add(new ReportColumn { Header = "رقم الفاتورة", AlignRight = true, Weight = 2 });
        report.Columns.Add(new ReportColumn { Header = "التاريخ", AlignRight = true, Weight = 2 });
        report.Columns.Add(new ReportColumn { Header = "رقم فاتورة المورد", AlignRight = true, Weight = 2 });
        report.Columns.Add(new ReportColumn { Header = "الكمية", AlignRight = true });
        report.Columns.Add(new ReportColumn { Header = "القيمة $", AlignRight = true });
        report.Columns.Add(new ReportColumn { Header = "صافي التكلفة", AlignRight = true });
        report.Columns.Add(new ReportColumn { Header = "الربح", AlignRight = true });

        foreach (var inv in invoices)
        {
            report.AddRow(
                inv.InvoiceNo,
                inv.InvoiceDate.ToString("yyyy-MM-dd"),
                inv.SupplierInvoiceNo ?? "—",
                inv.TotalQty.ToString("N2"),
                inv.TotalUsd.ToString("N2"),
                inv.NetCost.ToString("N2"),
                inv.TotalProfit.ToString("N2"));
        }

        report.AddSummary("عدد الفواتير", invoicesCount.ToString("N0"));
        report.AddSummary("إجمالي المدفوعات", $"{totalPayments:N2} د.ل");
        report.AddSummary("القيمة بالدولار", $"$ {totalUsd:N2}");
        report.AddSummary("إجمالي الربح", $"{totalProfit:N2} د.ل", bold: true);

        return report;
    }

    // ============================================================
    // 3) تقرير الأرباح
    // ============================================================
    private async Task<ReportTableDto> BuildProfitAsync(string period, int? supplierId)
    {
        var (from, to) = GetPeriodRange(period);

        var query = _db.Invoices
            .Where(i => i.Status == "Archived"
                        && i.InvoiceDate >= from
                        && i.InvoiceDate <= to);

        if (supplierId.HasValue)
            query = query.Where(i => i.SupplierId == supplierId.Value);

        var invoicesCount = await query.CountAsync();
        var totalSales = await query.SumAsync(i => (decimal?)i.TotalSales) ?? 0m;
        var totalProfit = await query.SumAsync(i => (decimal?)i.TotalProfit) ?? 0m;
        var avgMargin = await query.AverageAsync(i => (decimal?)i.ProfitMargin) ?? 0m;

        var invoices = await query
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .Select(i => new
            {
                i.InvoiceNo,
                i.InvoiceDate,
                SupplierName = i.Supplier != null ? i.Supplier.Name : null,
                i.TotalSales,
                i.TotalProfit,
                i.ProfitMargin
            })
            .ToListAsync();

        var report = new ReportTableDto
        {
            Title = "تقرير الأرباح",
            Subtitle = $"الفترة: {from:yyyy-MM-dd} إلى {to:yyyy-MM-dd}"
        };

        report.Columns.Add(new ReportColumn { Header = "رقم الفاتورة", AlignRight = true, Weight = 2 });
        report.Columns.Add(new ReportColumn { Header = "التاريخ", AlignRight = true, Weight = 2 });
        report.Columns.Add(new ReportColumn { Header = "المورد", AlignRight = true, Weight = 3 });
        report.Columns.Add(new ReportColumn { Header = "المبيعات", AlignRight = true });
        report.Columns.Add(new ReportColumn { Header = "الربح", AlignRight = true });
        report.Columns.Add(new ReportColumn { Header = "الهامش %", AlignRight = true });

        foreach (var inv in invoices)
        {
            report.AddRow(
                inv.InvoiceNo,
                inv.InvoiceDate.ToString("yyyy-MM-dd"),
                inv.SupplierName ?? "—",
                inv.TotalSales.ToString("N2"),
                inv.TotalProfit.ToString("N2"),
                inv.ProfitMargin.ToString("N4"));
        }

        report.AddSummary("عدد الفواتير", invoicesCount.ToString("N0"));
        report.AddSummary("إجمالي المبيعات", $"{totalSales:N2} د.ل");
        report.AddSummary("إجمالي الربح", $"{totalProfit:N2} د.ل", bold: true);
        report.AddSummary("متوسط الهامش", $"{avgMargin:N4} %");

        return report;
    }

    // ============================================================
    // 4) تقرير الفترة
    // ============================================================
    private async Task<ReportTableDto> BuildPeriodAsync(DateOnly dateFrom, DateOnly dateTo, int? supplierId)
    {
        var query = _db.Invoices
            .Where(i => i.Status == "Archived"
                        && i.InvoiceDate >= dateFrom
                        && i.InvoiceDate <= dateTo);

        if (supplierId.HasValue)
            query = query.Where(i => i.SupplierId == supplierId.Value);

        var invoicesCount = await query.CountAsync();
        var totalPayments = await query.SumAsync(i => (decimal?)i.TotalPayments) ?? 0m;
        var totalUsd = await query.SumAsync(i => (decimal?)i.TotalUsd) ?? 0m;
        var totalProfit = await query.SumAsync(i => (decimal?)i.TotalProfit) ?? 0m;

        var invoices = await query
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .Select(i => new
            {
                i.InvoiceNo,
                i.InvoiceDate,
                SupplierName = i.Supplier != null ? i.Supplier.Name : null,
                i.TotalUsd,
                i.TotalProfit
            })
            .ToListAsync();

        var report = new ReportTableDto
        {
            Title = "تقرير الفترة",
            Subtitle = $"من {dateFrom:yyyy-MM-dd} إلى {dateTo:yyyy-MM-dd}"
        };

        report.Columns.Add(new ReportColumn { Header = "رقم الفاتورة", AlignRight = true, Weight = 2 });
        report.Columns.Add(new ReportColumn { Header = "التاريخ", AlignRight = true, Weight = 2 });
        report.Columns.Add(new ReportColumn { Header = "المورد", AlignRight = true, Weight = 3 });
        report.Columns.Add(new ReportColumn { Header = "القيمة $", AlignRight = true });
        report.Columns.Add(new ReportColumn { Header = "الربح", AlignRight = true });

        foreach (var inv in invoices)
        {
            report.AddRow(
                inv.InvoiceNo,
                inv.InvoiceDate.ToString("yyyy-MM-dd"),
                inv.SupplierName ?? "—",
                inv.TotalUsd.ToString("N2"),
                inv.TotalProfit.ToString("N2"));
        }

        report.AddSummary("عدد الفواتير", invoicesCount.ToString("N0"));
        report.AddSummary("إجمالي المدفوعات", $"{totalPayments:N2} د.ل");
        report.AddSummary("القيمة بالدولار", $"$ {totalUsd:N2}");
        report.AddSummary("إجمالي الربح", $"{totalProfit:N2} د.ل", bold: true);

        return report;
    }

    // ============================================================
    // 5) تقرير تغيّر الأسعار
    // ============================================================
    private async Task<ReportTableDto?> BuildPriceChangesAsync(int productId, string period)
    {
        var product = await _db.Products.FindAsync(productId);
        if (product == null) return null;

        var (from, to) = GetPeriodRange(period);

        var rows = await _db.InvoiceItems
            .Include(ii => ii.Invoice).ThenInclude(i => i!.Supplier)
            .Where(ii => ii.ProductId == productId
                        && ii.Invoice != null
                        && ii.Invoice.Status == "Archived"
                        && ii.Invoice.InvoiceDate >= from
                        && ii.Invoice.InvoiceDate <= to)
            .OrderBy(ii => ii.Invoice!.InvoiceDate)
            .ThenBy(ii => ii.Id)
            .Select(ii => new
            {
                date = ii.Invoice!.InvoiceDate,
                invoiceNo = ii.Invoice.InvoiceNo,
                supplierName = ii.Invoice.Supplier != null ? ii.Invoice.Supplier.Name : null,
                purchaseUsd = ii.PurchaseUsd,
                saleDzd = ii.SaleDzd
            })
            .ToListAsync();

        var report = new ReportTableDto
        {
            Title = $"تقرير تغيّر الأسعار: {product.Name}",
            Subtitle = $"الفترة: {from:yyyy-MM-dd} إلى {to:yyyy-MM-dd}"
        };

        report.Columns.Add(new ReportColumn { Header = "التاريخ", AlignRight = true, Weight = 2 });
        report.Columns.Add(new ReportColumn { Header = "رقم الفاتورة", AlignRight = true, Weight = 2 });
        report.Columns.Add(new ReportColumn { Header = "المورد", AlignRight = true, Weight = 3 });
        report.Columns.Add(new ReportColumn { Header = "الشراء $", AlignRight = true });
        report.Columns.Add(new ReportColumn { Header = "البيع د.ل", AlignRight = true });
        report.Columns.Add(new ReportColumn { Header = "تغيّر الشراء %", AlignRight = true });
        report.Columns.Add(new ReportColumn { Header = "تغيّر البيع %", AlignRight = true });

        decimal? prevPurchase = null;
        decimal? prevSale = null;

        foreach (var r in rows)
        {
            decimal? changePurchase = null;
            decimal? changeSale = null;

            if (prevPurchase.HasValue && prevPurchase.Value > 0m)
                changePurchase = ((r.purchaseUsd - prevPurchase.Value) / prevPurchase.Value) * 100m;

            if (prevSale.HasValue && prevSale.Value > 0m)
                changeSale = ((r.saleDzd - prevSale.Value) / prevSale.Value) * 100m;

            report.AddRow(
                r.date.ToString("yyyy-MM-dd"),
                r.invoiceNo,
                r.supplierName ?? "—",
                r.purchaseUsd.ToString("N2"),
                r.saleDzd.ToString("N2"),
                changePurchase.HasValue ? changePurchase.Value.ToString("N2") : "—",
                changeSale.HasValue ? changeSale.Value.ToString("N2") : "—");

            prevPurchase = r.purchaseUsd;
            prevSale = r.saleDzd;
        }

        var firstRow = rows.FirstOrDefault();
        var lastRow = rows.LastOrDefault();

        report.AddSummary("أول سعر شراء", $"$ {firstRow?.purchaseUsd ?? 0m:N2}");
        report.AddSummary("آخر سعر شراء", $"$ {lastRow?.purchaseUsd ?? 0m:N2}");
        report.AddSummary("أول سعر بيع", $"{firstRow?.saleDzd ?? 0m:N2} د.ل");
        report.AddSummary("آخر سعر بيع", $"{lastRow?.saleDzd ?? 0m:N2} د.ل", bold: true);

        return report;
    }

    // ============================================================
    // 6) الأكثر ربحية
    // ============================================================
    private async Task<ReportTableDto> BuildMostProfitableAsync(string period, string? category, int topN)
    {
        var (from, to) = GetPeriodRange(period);

        var itemsQuery = _db.InvoiceItems
            .Include(ii => ii.Invoice)
            .Include(ii => ii.Product)
            .Where(ii => ii.Invoice != null
                        && ii.Invoice.Status == "Archived"
                        && ii.Invoice.InvoiceDate >= from
                        && ii.Invoice.InvoiceDate <= to);

        if (!string.IsNullOrWhiteSpace(category))
            itemsQuery = itemsQuery.Where(ii => ii.Product != null && ii.Product.Category == category);

        var grouped = await itemsQuery
            .GroupBy(ii => new
            {
                ii.ProductId,
                ProductCode = ii.Product != null ? ii.Product.Code : "",
                ProductName = ii.Product != null ? ii.Product.Name : "",
                ProductCategory = ii.Product != null ? ii.Product.Category : "",
                ProductPattern = ii.Product != null ? ii.Product.Pattern : null
            })
            .Select(g => new
            {
                id = g.Key.ProductId,
                code = g.Key.ProductCode,
                name = g.Key.ProductName,
                category = g.Key.ProductCategory,
                pattern = g.Key.ProductPattern,
                totalQty = g.Sum(x => x.Quantity),
                totalSales = g.Sum(x => x.TotalSale),
                totalProfit = g.Sum(x => x.TotalProfit)
            })
            .OrderByDescending(x => x.totalProfit)
            .ToListAsync();

        var finalList = topN > 0 ? grouped.Take(topN).ToList() : grouped;

        var report = new ReportTableDto
        {
            Title = "الأصناف الأكثر ربحية",
            Subtitle = $"الفترة: {from:yyyy-MM-dd} إلى {to:yyyy-MM-dd}"
        };

        report.Columns.Add(new ReportColumn { Header = "الصنف", AlignRight = true, Weight = 3 });
        report.Columns.Add(new ReportColumn { Header = "الكود", AlignRight = true, Weight = 2 });
        report.Columns.Add(new ReportColumn { Header = "التصنيف", AlignRight = true, Weight = 2 });
        report.Columns.Add(new ReportColumn { Header = "الكمية", AlignRight = true });
        report.Columns.Add(new ReportColumn { Header = "المبيعات", AlignRight = true });
        report.Columns.Add(new ReportColumn { Header = "الربح", AlignRight = true });
        report.Columns.Add(new ReportColumn { Header = "الهامش %", AlignRight = true });

        foreach (var p in finalList)
        {
            var margin = p.totalSales > 0m ? (p.totalProfit / p.totalSales) * 100m : 0m;
            report.AddRow(
                p.name,
                p.code,
                p.category,
                p.totalQty.ToString("N2"),
                p.totalSales.ToString("N2"),
                p.totalProfit.ToString("N2"),
                margin.ToString("N4"));
        }

        var totalProfitAll = grouped.Sum(p => p.totalProfit);
        var topProfit = grouped.FirstOrDefault()?.totalProfit ?? 0m;

        report.AddSummary("عدد الأصناف", grouped.Count.ToString("N0"));
        report.AddSummary("إجمالي الربح", $"{totalProfitAll:N2} د.ل");
        report.AddSummary("أعلى ربح صنف", $"{topProfit:N2} د.ل", bold: true);

        return report;
    }

    // ============================================================
    // 7) الأكثر مبيعًا
    // ============================================================
    private async Task<ReportTableDto> BuildBestSellingAsync(string period, string? category, int topN)
    {
        var (from, to) = GetPeriodRange(period);

        var itemsQuery = _db.InvoiceItems
            .Include(ii => ii.Invoice)
            .Include(ii => ii.Product)
            .Where(ii => ii.Invoice != null
                        && ii.Invoice.Status == "Archived"
                        && ii.Invoice.InvoiceDate >= from
                        && ii.Invoice.InvoiceDate <= to);

        if (!string.IsNullOrWhiteSpace(category))
            itemsQuery = itemsQuery.Where(ii => ii.Product != null && ii.Product.Category == category);

        var grouped = await itemsQuery
            .GroupBy(ii => new
            {
                ii.ProductId,
                ProductCode = ii.Product != null ? ii.Product.Code : "",
                ProductName = ii.Product != null ? ii.Product.Name : "",
                ProductCategory = ii.Product != null ? ii.Product.Category : ""
            })
            .Select(g => new
            {
                id = g.Key.ProductId,
                code = g.Key.ProductCode,
                name = g.Key.ProductName,
                category = g.Key.ProductCategory,
                totalQty = g.Sum(x => x.Quantity),
                totalSales = g.Sum(x => x.TotalSale),
                invoicesCount = g.Select(x => x.InvoiceId).Distinct().Count()
            })
            .OrderByDescending(x => x.totalQty)
            .ToListAsync();

        var finalList = topN > 0 ? grouped.Take(topN).ToList() : grouped;

        var report = new ReportTableDto
        {
            Title = "الأصناف الأكثر مبيعًا",
            Subtitle = $"الفترة: {from:yyyy-MM-dd} إلى {to:yyyy-MM-dd}"
        };

        report.Columns.Add(new ReportColumn { Header = "الصنف", AlignRight = true, Weight = 3 });
        report.Columns.Add(new ReportColumn { Header = "الكود", AlignRight = true, Weight = 2 });
        report.Columns.Add(new ReportColumn { Header = "التصنيف", AlignRight = true, Weight = 2 });
        report.Columns.Add(new ReportColumn { Header = "الكمية", AlignRight = true });
        report.Columns.Add(new ReportColumn { Header = "المبيعات", AlignRight = true });

        foreach (var p in finalList)
        {
            report.AddRow(
                p.name,
                p.code,
                p.category,
                p.totalQty.ToString("N2"),
                p.totalSales.ToString("N2"));
        }

        var totalQtyAll = grouped.Sum(p => p.totalQty);
        var totalSalesAll = grouped.Sum(p => p.totalSales);

        report.AddSummary("عدد الأصناف", grouped.Count.ToString("N0"));
        report.AddSummary("إجمالي الكمية", totalQtyAll.ToString("N2"));
        report.AddSummary("إجمالي المبيعات", $"{totalSalesAll:N2} د.ل", bold: true);

        return report;
    }

    // ============================================================
    // دوال مساعدة
    // ============================================================
    private static (DateOnly from, DateOnly to) GetPeriodRange(string period)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly from;

        switch (period?.ToLowerInvariant())
        {
            case "today": from = today; break;
            case "week":
                int diff = (int)today.DayOfWeek - (int)DayOfWeek.Monday;
                if (diff < 0) diff += 7;
                from = today.AddDays(-diff);
                break;
            case "month": from = new DateOnly(today.Year, today.Month, 1); break;
            case "quarter":
                int qStart = ((today.Month - 1) / 3) * 3 + 1;
                from = new DateOnly(today.Year, qStart, 1);
                break;
            case "year": from = new DateOnly(today.Year, 1, 1); break;
            default: from = new DateOnly(1900, 1, 1); break;
        }

        return (from, today);
    }
}