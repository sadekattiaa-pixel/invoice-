using Invoice.Application.Dtos;
using Invoice.Infrastructure.Exporters;
using Invoice.Infrastructure.Persistence;
using Invoice.Infrastructure.Storage;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Invoice.Api.Controllers;

/// <summary>
/// نقاط نهاية API لتصدير الفواتير إلى PDF و Excel.
/// </summary>
[ApiController]
[Route("api/exports")]
public class ExportsController : ControllerBase
{
    private readonly AppDbContext _db;

    public ExportsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>تنزيل فاتورة كملف PDF.</summary>
    [HttpGet("invoice/{id:long}/pdf")]
    public async Task<IActionResult> ExportInvoicePdf(long id)
    {
        var invoice = await LoadInvoiceAsync(id);
        if (invoice == null)
            return NotFound(new { message = "الفاتورة غير موجودة." });

        var company = await LoadCompanyInfoAsync();
        var (filePath, fileName) = PrepareTempFile(invoice.InvoiceNo, "pdf");

        try
        {
            InvoicePdfExporter.Export(invoice, company, filePath);
            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);
            System.IO.File.Delete(filePath);
            return File(bytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "فشل توليد PDF.", detail = ex.Message });
        }
    }

    /// <summary>تنزيل فاتورة كملف Excel.</summary>
    [HttpGet("invoice/{id:long}/excel")]
    public async Task<IActionResult> ExportInvoiceExcel(long id)
    {
        var invoice = await LoadInvoiceAsync(id);
        if (invoice == null)
            return NotFound(new { message = "الفاتورة غير موجودة." });

        var company = await LoadCompanyInfoAsync();
        var (filePath, fileName) = PrepareTempFile(invoice.InvoiceNo, "xlsx");

        try
        {
            InvoiceExcelExporter.Export(invoice, company, filePath);
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

    // ========= دوال مساعدة =========

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

        // حمّل الشعار من القرص إن وُجد مسار
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

    private static (string filePath, string fileName) PrepareTempFile(string invoiceNo, string extension)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), "InvoiceExports");
        Directory.CreateDirectory(tempDir);
        var safeName = string.Join("_", invoiceNo.Split(Path.GetInvalidFileNameChars()));
        var fileName = $"{safeName}_{DateTime.Now:yyyyMMddHHmmss}.{extension}";
        var filePath = Path.Combine(tempDir, fileName);
        return (filePath, fileName);
    }
}