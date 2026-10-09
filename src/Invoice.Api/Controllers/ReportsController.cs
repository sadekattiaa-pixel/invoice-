using Invoice.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Invoice.Api.Controllers;

/// <summary>
/// نقاط نهاية API للتقارير والإحصائيات.
/// </summary>
[ApiController]
[Route("api/reports")]
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

    /// <summary>
    /// تقرير بين تاريخين: إحصائيات + قائمة فواتير + أرباح يومية.
    /// </summary>
    [HttpGet("period")]
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