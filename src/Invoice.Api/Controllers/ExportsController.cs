using Invoice.Api.Security;
using Invoice.Application.Dtos;
using Invoice.Infrastructure.Exporters;
using Invoice.Infrastructure.Persistence;
using Invoice.Infrastructure.Services;
using Invoice.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Invoice.Api.Controllers;

/// <summary>
/// نقاط نهاية API لتصدير الفواتير والتقارير إلى PDF و Excel.
/// كل نقطة تتطلب صلاحية exports.read.
/// </summary>
[ApiController]
[Route("api/exports")]
[Authorize]
public class ExportsController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly ReportDataService _reportService;

    public ExportsController(AppDbContext db, ReportDataService reportService)
    {
        _db = db;
        _reportService = reportService;
    }

    // ============================================================
    // تصدير الفواتير
    // ============================================================

    /// <summary>
    /// تنزيل فاتورة كملف PDF.
    /// hidden: مفاتيح الأجزاء المخفية (مفصولة بفواصل). انظر InvoicePdfExporter.Keys.
    /// </summary>
    [HttpGet("invoice/{id:long}/pdf")]
    [Authorize(Policy = Permissions.ExportsRead)]
    public async Task<IActionResult> ExportInvoicePdf(
        long id,
        [FromQuery] string? hidden = null)
    {
        var invoice = await LoadInvoiceAsync(id);
        if (invoice == null)
            return NotFound(new { message = "الفاتورة غير موجودة." });

        var company = await LoadCompanyInfoAsync();
        var (filePath, fileName) = PrepareTempFile(invoice.InvoiceNo, "pdf");

        try
        {
            InvoicePdfExporter.Export(invoice, company, filePath, ParseHidden(hidden));
            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            System.IO.File.Delete(filePath);
            return File(bytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "فشل توليد PDF.", detail = ex.Message });
        }
    }

    /// <summary>
    /// تنزيل فاتورة كملف Excel.
    /// hidden: مفاتيح الأجزاء المخفية (مفصولة بفواصل). انظر InvoicePdfExporter.Keys.
    /// </summary>
    [HttpGet("invoice/{id:long}/excel")]
    [Authorize(Policy = Permissions.ExportsRead)]
    public async Task<IActionResult> ExportInvoiceExcel(
        long id,
        [FromQuery] string? hidden = null)
    {
        var invoice = await LoadInvoiceAsync(id);
        if (invoice == null)
            return NotFound(new { message = "الفاتورة غير موجودة." });

        var company = await LoadCompanyInfoAsync();
        var (filePath, fileName) = PrepareTempFile(invoice.InvoiceNo, "xlsx");

        try
        {
            InvoiceExcelExporter.Export(invoice, company, filePath, ParseHidden(hidden));
            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            System.IO.File.Delete(filePath);
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "فشل توليد Excel.", detail = ex.Message });
        }
    }

    // ============================================================
    // تصدير التقارير
    // ============================================================

    /// <summary>
    /// تصدير أي تقرير إلى PDF.
    /// الأنواع المدعومة: dashboard, supplier, profit, period, price-changes,
    /// most-profitable, best-selling.
    /// </summary>
    [HttpGet("report/{type}/pdf")]
    [Authorize(Policy = Permissions.ExportsRead)]
    public async Task<IActionResult> ExportReportPdf(
        string type,
        [FromQuery] string period = "year",
        [FromQuery] int? supplierId = null,
        [FromQuery] int? productId = null,
        [FromQuery] string? category = null,
        [FromQuery] int topN = 10,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null)
    {
        var report = await _reportService.BuildAsync(
            type, period, supplierId, productId, category, topN, dateFrom, dateTo);

        if (report == null)
            return BadRequest(new
            {
                message = "نوع التقرير غير معروف أو معامل مطلوب ناقص.",
                supported = new[]
                {
                    "dashboard", "supplier", "profit", "period",
                    "price-changes", "most-profitable", "best-selling"
                }
            });

        var company = await LoadCompanyInfoAsync();
        var (filePath, fileName) = PrepareTempFile($"report-{type}", "pdf");

        try
        {
            ReportPdfExporter.Export(report, company, filePath);
            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            System.IO.File.Delete(filePath);
            return File(bytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "فشل توليد PDF للتقرير.", detail = ex.Message });
        }
    }

    /// <summary>تصدير أي تقرير إلى Excel.</summary>
    [HttpGet("report/{type}/excel")]
    [Authorize(Policy = Permissions.ExportsRead)]
    public async Task<IActionResult> ExportReportExcel(
        string type,
        [FromQuery] string period = "year",
        [FromQuery] int? supplierId = null,
        [FromQuery] int? productId = null,
        [FromQuery] string? category = null,
        [FromQuery] int topN = 10,
        [FromQuery] DateOnly? dateFrom = null,
        [FromQuery] DateOnly? dateTo = null)
    {
        var report = await _reportService.BuildAsync(
            type, period, supplierId, productId, category, topN, dateFrom, dateTo);

        if (report == null)
            return BadRequest(new
            {
                message = "نوع التقرير غير معروف أو معامل مطلوب ناقص.",
                supported = new[]
                {
                    "dashboard", "supplier", "profit", "period",
                    "price-changes", "most-profitable", "best-selling"
                }
            });

        var company = await LoadCompanyInfoAsync();
        var (filePath, fileName) = PrepareTempFile($"report-{type}", "xlsx");

        try
        {
            ReportExcelExporter.Export(report, company, filePath);
            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            System.IO.File.Delete(filePath);
            return File(bytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "فشل توليد Excel للتقرير.", detail = ex.Message });
        }
    }

    // ========= دوال مساعدة =========

    /// <summary>يُحوّل نص "a,b,c" إلى HashSet.</summary>
    private static HashSet<string>? ParseHidden(string? hidden)
    {
        if (string.IsNullOrWhiteSpace(hidden)) return null;
        var set = hidden
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => !string.IsNullOrEmpty(s))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        return set.Count > 0 ? set : null;
    }

    private async Task<Domain.Entities.Invoice?> LoadInvoiceAsync(long id)
    {
        return await _db.Invoices
            .Include(i => i.Supplier)
            .Include(i => i.Items).ThenInclude(it => it.Product)
            .Include(i => i.Payments)
            .Include(i => i.Discounts)
            .Include(i => i.Expenses)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);
    }

    private async Task<CompanyInfoDto> LoadCompanyInfoAsync()
    {
        var settings = await _db.Settings
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Key, s => s.Value ?? "");

        var company = CompanyInfoDto.FromSettings(settings);

        if (!string.IsNullOrWhiteSpace(company.LogoPath))
        {
            var bytes = await LogoStorage.ReadBytesAsync(company.LogoPath);
            if (bytes != null && bytes.Length > 0)
            {
                company.LogoBytes = bytes;
                company.LogoContentType = LogoStorage.GetContentType(company.LogoPath);
            }
        }

        return company;
    }

    private static (string filePath, string fileName) PrepareTempFile(string baseName, string extension)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "InvoiceExports");
        Directory.CreateDirectory(tempDir);
        var safeName = string.Join("_", baseName.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"{safeName}_{DateTime.Now:yyyyMMddHHmmss}.{extension}";
        var filePath = Path.Combine(tempDir, fileName);
        return (filePath, fileName);
    }
}