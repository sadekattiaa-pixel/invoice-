using Invoice.Api.Security;
using Invoice.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Invoice.Api.Controllers;

/// <summary>
/// نقاط نهاية API للتقارير والإحصائيات.
/// كل نقطة تتطلب صلاحية reports.read.
/// </summary>
[ApiController]
[Route("api/reports")]
[Authorize]
public class ReportsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ReportsController(AppDbContext db)
    {
        _db = db;
    }

    // ============================================================
    // 1) Dashboard KPIs
    // ============================================================

    [HttpGet("dashboard")]
    [Authorize(Policy = Permissions.ReportsRead)]
    public async Task<ActionResult> GetDashboard([FromQuery] string period = "year")
    {
        var (from, to) = GetPeriodRange(period);

        var baseQuery = _db.Invoices
            .Where(i => i.Status == "Archived" && i.InvoiceDate >= from && i.InvoiceDate <= to);

        var totalInvoices = await baseQuery.CountAsync();
        var totalPayments = await baseQuery.SumAsync(i => (decimal?)i.TotalPayments) ?? 0m;
        var totalUsd = await baseQuery.SumAsync(i => (decimal?)i.TotalUsd) ?? 0m;
        var totalProfit = await baseQuery.SumAsync(i => (decimal?)i.TotalProfit) ?? 0m;
        var totalSales = await baseQuery.SumAsync(i => (decimal?)i.TotalSales) ?? 0m;

        var openInvoices = await _db.Invoices.CountAsync(i => i.Status == "Draft");
        var totalSuppliers = await _db.Suppliers.CountAsync();
        var totalProducts = await _db.Products.CountAsync();

        return Ok(new
        {
            period,
            from,
            to,
            invoices = new { total = totalInvoices, drafts = openInvoices },
            amounts = new { totalPayments, totalUsd, totalSales, totalProfit },
            catalog = new { suppliers = totalSuppliers, products = totalProducts }
        });
    }

    // ============================================================
    // 2) تقرير المورد
    // ============================================================

    [HttpGet("supplier/{supplierId:int}")]
    [Authorize(Policy = Permissions.ReportsRead)]
    public async Task<ActionResult> GetSupplierReport(
        int supplierId,
        [FromQuery] string period = "year")
    {
        var supplier = await _db.Suppliers.FindAsync(supplierId);
        if (supplier == null)
            return NotFound(new { message = "المورد غير موجود." });

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
                i.Id,
                i.InvoiceNo,
                i.SupplierInvoiceNo,
                i.InvoiceDate,
                i.TotalUsd,
                i.NetCost,
                i.TotalProfit,
                TotalQty = i.Items.Sum(it => (decimal?)it.Quantity) ?? 0m
            })
            .ToListAsync();

        var monthly = await invoicesQuery
            .GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
            .Select(g => new
            {
                year = g.Key.Year,
                month = g.Key.Month,
                profit = g.Sum(i => i.TotalProfit)
            })
            .OrderBy(x => x.year).ThenBy(x => x.month)
            .ToListAsync();

        return Ok(new
        {
            supplier = new
            {
                supplier.Id,
                supplier.Code,
                supplier.Name,
                supplier.Phone,
                supplier.Email,
                supplier.Address
            },
            period,
            from,
            to,
            stats = new { invoicesCount, totalPayments, totalUsd, totalProfit },
            invoices,
            monthly
        });
    }

    // ============================================================
    // 3) تقرير الأرباح
    // ============================================================

    [HttpGet("profit")]
    [Authorize(Policy = Permissions.ReportsRead)]
    public async Task<ActionResult> GetProfitReport(
        [FromQuery] string period = "year",
        [FromQuery] int? supplierId = null)
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
                i.Id,
                i.InvoiceNo,
                i.InvoiceDate,
                SupplierName = i.Supplier != null ? i.Supplier.Name : null,
                i.TotalSales,
                i.TotalProfit,
                i.ProfitMargin
            })
            .ToListAsync();

        var monthly = await query
            .GroupBy(i => new { i.InvoiceDate.Year, i.InvoiceDate.Month })
            .Select(g => new
            {
                year = g.Key.Year,
                month = g.Key.Month,
                profit = g.Sum(i => i.TotalProfit)
            })
            .OrderBy(x => x.year).ThenBy(x => x.month)
            .ToListAsync();

        return Ok(new
        {
            period,
            from,
            to,
            supplierId,
            stats = new { invoicesCount, totalSales, totalProfit, avgMargin },
            invoices,
            monthly
        });
    }

    // ============================================================
    // 4) تقرير الفترة
    // ============================================================

    [HttpGet("period")]
    [Authorize(Policy = Permissions.ReportsRead)]
    public async Task<ActionResult> GetPeriodReport(
        [FromQuery] DateOnly dateFrom,
        [FromQuery] DateOnly dateTo,
        [FromQuery] int? supplierId = null)
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
                i.Id,
                i.InvoiceNo,
                i.InvoiceDate,
                SupplierName = i.Supplier != null ? i.Supplier.Name : null,
                i.TotalUsd,
                i.TotalProfit
            })
            .ToListAsync();

        var daily = await query
            .GroupBy(i => i.InvoiceDate)
            .Select(g => new
            {
                date = g.Key,
                profit = g.Sum(i => i.TotalProfit)
            })
            .OrderBy(x => x.date)
            .ToListAsync();

        return Ok(new
        {
            dateFrom,
            dateTo,
            supplierId,
            stats = new { invoicesCount, totalPayments, totalUsd, totalProfit },
            invoices,
            daily
        });
    }

    // ============================================================
    // 5) تقرير تغيّر الأسعار
    // ============================================================

    [HttpGet("price-changes/{productId:int}")]
    [Authorize(Policy = Permissions.ReportsRead)]
    public async Task<ActionResult> GetPriceChangesReport(
        int productId,
        [FromQuery] string period = "year")
    {
        var product = await _db.Products.FindAsync(productId);
        if (product == null)
            return NotFound(new { message = "الصنف غير موجود." });

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

        var changes = new List<object>();
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

            changes.Add(new
            {
                r.date,
                r.invoiceNo,
                r.supplierName,
                r.purchaseUsd,
                r.saleDzd,
                changePurchase,
                changeSale
            });

            prevPurchase = r.purchaseUsd;
            prevSale = r.saleDzd;
        }

        var firstRow = rows.FirstOrDefault();
        var lastRow = rows.LastOrDefault();

        return Ok(new
        {
            product = new
            {
                product.Id,
                product.Code,
                product.Name,
                product.Category,
                product.Pattern
            },
            period,
            from,
            to,
            stats = new
            {
                firstPurchase = firstRow?.purchaseUsd ?? 0m,
                lastPurchase = lastRow?.purchaseUsd ?? 0m,
                firstSale = firstRow?.saleDzd ?? 0m,
                lastSale = lastRow?.saleDzd ?? 0m
            },
            changes
        });
    }

    // ============================================================
    // 6) الأكثر ربحية
    // ============================================================

    /// <summary>
    /// أعلى الأصناف ربحية.
    /// topN = 0 يعني كل الأصناف.
    /// category = "" يعني كل التصنيفات.
    /// </summary>
    [HttpGet("most-profitable")]
    [Authorize(Policy = Permissions.ReportsRead)]
    public async Task<ActionResult> GetMostProfitableReport(
        [FromQuery] string period = "year",
        [FromQuery] string? category = null,
        [FromQuery] int topN = 10)
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

        var products = finalList.Select(p => new
        {
            p.id,
            p.code,
            p.name,
            p.category,
            p.pattern,
            p.totalQty,
            p.totalSales,
            p.totalProfit,
            margin = p.totalSales > 0m ? (p.totalProfit / p.totalSales) * 100m : 0m
        }).ToList();

        var totalProfitAll = grouped.Sum(p => p.totalProfit);
        var productsCount = grouped.Count;
        var avgMarginAll = products.Any() ? products.Average(p => p.margin) : 0m;
        var topProfit = grouped.FirstOrDefault()?.totalProfit ?? 0m;

        return Ok(new
        {
            period,
            from,
            to,
            category,
            topN,
            stats = new
            {
                totalProfit = totalProfitAll,
                productsCount,
                avgMargin = avgMarginAll,
                topProfit
            },
            products
        });
    }

    // ============================================================
    // 7) الأكثر مبيعًا
    // ============================================================

    /// <summary>
    /// الأصناف الأكثر مبيعًا (بالكمية).
    /// topN = 0 يعني كل الأصناف.
    /// category = "" يعني كل التصنيفات.
    /// </summary>
    [HttpGet("best-selling")]
    [Authorize(Policy = Permissions.ReportsRead)]
    public async Task<ActionResult> GetBestSellingReport(
        [FromQuery] string period = "year",
        [FromQuery] string? category = null,
        [FromQuery] int topN = 10)
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
                invoicesCount = g.Select(x => x.InvoiceId).Distinct().Count()
            })
            .OrderByDescending(x => x.totalQty)
            .ToListAsync();

        var finalList = topN > 0 ? grouped.Take(topN).ToList() : grouped;

        var totalQtyAll = grouped.Sum(p => p.totalQty);
        var totalSalesAll = grouped.Sum(p => p.totalSales);
        var topQty = grouped.FirstOrDefault()?.totalQty ?? 0m;

        return Ok(new
        {
            period,
            from,
            to,
            category,
            topN,
            stats = new
            {
                totalQty = totalQtyAll,
                totalSales = totalSalesAll,
                topQty,
                productsCount = grouped.Count
            },
            products = finalList
        });
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
            case "today":
                from = today;
                break;
            case "week":
                int diff = (int)today.DayOfWeek - (int)DayOfWeek.Monday;
                if (diff < 0) diff += 7;
                from = today.AddDays(-diff);
                break;
            case "month":
                from = new DateOnly(today.Year, today.Month, 1);
                break;
            case "quarter":
                int qStart = ((today.Month - 1) / 3) * 3 + 1;
                from = new DateOnly(today.Year, qStart, 1);
                break;
            case "year":
                from = new DateOnly(today.Year, 1, 1);
                break;
            default:
                from = new DateOnly(1900, 1, 1);
                break;
        }

        return (from, today);
    }
}