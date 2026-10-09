using Invoice.Infrastructure.Exporters;
using Invoice.Infrastructure.Persistence;
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
        var invoice = await _db.Invoices
            .Include(i => i.Supplier)
            .Include(i => i.Items).ThenInclude(it => it.Product)
            .Include(i => i.Payments)
            .Include(i => i.Discounts)
            .Include(i => i.Expenses)
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
            return NotFound(new { message = "الفاتورة غير موجودة." });

        // نولّد الملف في مجلد مؤقت
        var tempDir = Path.Combine(Path.GetTempPath(), "InvoiceExports");
        Directory.CreateDirectory(tempDir);
        var fileName = $"{invoice.InvoiceNo}_{DateTime.Now:yyyyMMddHHmmss}.pdf";
        var filePath = Path.Combine(tempDir, fileName);

        try
        {
            InvoicePdfExporter.Export(invoice, filePath);

            // نقرأ الملف ونعيده كـ stream (حتى يُحذف بعد الاستخدام)
            var bytes = await System.IO.File.ReadAllBytesAsync(filePath);

            // نحذف الملف المؤقت بعد قراءته
            System.IO.File.Delete(filePath);

            return File(bytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "فشل توليد PDF.", detail = ex.Message });
        }
    }
}