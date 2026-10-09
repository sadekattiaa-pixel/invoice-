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
    // Dashboard KPIs
    // ============================================================

    /// <summary>
    /// إحصائيات سريعة للوحة التحكم.
    /// period: today / week / month / quarter / year / all
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<ActionResult> GetDashboard([FromQuery] string period = "year")
    {
        var (from, to) = GetPeriodRange(period);

        // الفواتير المؤرشفة فقط تدخل في الإحصائيات
        var baseQuery = _db.Invoices
            .Where(i => i.Status == "Archived" && i.InvoiceDate >= from && i.InvoiceDate <= to);

        var totalInvoices = await baseQuery.CountAsync();
        var totalPayments = await baseQuery.SumAsync(i => (decimal?)i.TotalPayments) ?? 0m;
        var totalUsd = await baseQuery.SumAsync(i => (decimal?)i.TotalUsd) ?? 0m;
        var totalProfit = await baseQuery.SumAsync(i => (decimal?)i.TotalProfit) ?? 0m;
        var totalSales = await baseQuery.SumAsync(i => (decimal?)i.TotalSales) ?? 0m;

        // عدد الفواتير المسودة (بلا اعتبار للفترة، لأنها قيد العمل)
        var openInvoices = await _db.Invoices.CountAsync(i => i.Status == "Draft");

        // عدد الموردين والأصناف الإجمالي
        var totalSuppliers = await _db.Suppliers.CountAsync();
        var totalProducts = await _db.Products.CountAsync();

        return Ok(new
        {
            period,
            from,
            to,
            invoices = new
            {
                total = totalInvoices,
                drafts = openInvoices
            },
            amounts = new
            {
                totalPayments,
                totalUsd,
                totalSales,
                totalProfit
            },
            catalog = new
            {
                suppliers = totalSuppliers,
                products = totalProducts
            }
        });
    }

    // ============================================================
    // دوال مساعدة
    // ============================================================

    /// <summary>يُرجع نطاق التاريخ بحسب الفترة المختارة.</summary>
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
            default: // "all"
                from = new DateOnly(1900, 1, 1);
                break;
        }

        return (from, today);
    }
}