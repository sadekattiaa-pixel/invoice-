using Invoice.Api.Security;
using Invoice.Domain.Entities;
using Invoice.Infrastructure.Persistence;
using Invoice.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Invoice.Api.Controllers;

/// <summary>
/// نقاط نهاية API للإعدادات — مخزن مفتاح/قيمة + رفع شعار الشركة.
/// القراءة تتطلب settings.read، والتعديل يتطلب settings.update.
/// </summary>
[ApiController]
[Route("api/settings")]
[Authorize]
public class SettingsController : ControllerBase
{
    private const string LogoPathKey = "company_logo_path";

    private readonly AppDbContext _db;

    public SettingsController(AppDbContext db)
    {
        _db = db;
    }

    // ============================================================
    // إعدادات مفتاح/قيمة
    // ============================================================

    /// <summary>جلب كل الإعدادات كمفتاح/قيمة.</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.SettingsRead)]
    public async Task<ActionResult> GetAll()
    {
        var settings = await _db.Settings
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Key, s => s.Value ?? "");

        return Ok(settings);
    }

    /// <summary>حفظ/تحديث مجموعة من الإعدادات دفعة واحدة.</summary>
    [HttpPut]
    [Authorize(Policy = Permissions.SettingsUpdate)]
    public async Task<ActionResult> UpdateAll([FromBody] Dictionary<string, string> values)
    {
        if (values == null || values.Count == 0)
            return BadRequest(new { message = "لا توجد قيم للحفظ." });

        var now = DateTime.UtcNow;

        foreach (var (key, value) in values)
        {
            if (string.IsNullOrWhiteSpace(key)) continue;

            var existing = await _db.Settings.FindAsync(key);
            if (existing == null)
            {
                _db.Settings.Add(new Settings
                {
                    Key = key,
                    Value = value,
                    UpdatedAt = now
                });
            }
            else
            {
                existing.Value = value;
                existing.UpdatedAt = now;
            }
        }

        await _db.SaveChangesAsync();
        return NoContent();
    }

    // ============================================================
    // شعار الشركة
    // ============================================================

    /// <summary>رفع شعار الشركة (multipart/form-data، حقل اسمه "file").</summary>
    [HttpPost("logo")]
    [Authorize(Policy = Permissions.SettingsUpdate)]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5 MB
    public async Task<IActionResult> UploadLogo(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "لم يتم إرسال ملف." });

        var extension = Path.GetExtension(file.FileName);
        if (!LogoStorage.IsAllowedExtension(extension))
            return BadRequest(new { message = "امتداد غير مدعوم. المسموح: png, jpg, jpeg, gif, bmp, webp." });

        // احذف الشعار القديم من القرص
        var oldPath = await _db.Settings
            .Where(s => s.Key == LogoPathKey)
            .Select(s => s.Value)
            .FirstOrDefaultAsync();

        LogoStorage.Delete(oldPath);

        // احفظ الجديد
        string savedPath;
        await using (var stream = file.OpenReadStream())
        {
            savedPath = await LogoStorage.SaveAsync(stream, extension);
        }

        // حدّث قاعدة البيانات
        var now = DateTime.UtcNow;
        var existing = await _db.Settings.FindAsync(LogoPathKey);
        if (existing == null)
        {
            _db.Settings.Add(new Settings
            {
                Key = LogoPathKey,
                Value = savedPath,
                UpdatedAt = now
            });
        }
        else
        {
            existing.Value = savedPath;
            existing.UpdatedAt = now;
        }
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "تم رفع الشعار بنجاح.",
            path = savedPath
        });
    }

    /// <summary>استرجاع شعار الشركة الحالي (صورة).</summary>
    [HttpGet("logo")]
    [Authorize(Policy = Permissions.SettingsRead)]
    public async Task<IActionResult> GetLogo()
    {
        var path = await _db.Settings
            .AsNoTracking()
            .Where(s => s.Key == LogoPathKey)
            .Select(s => s.Value)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(path))
            return NotFound(new { message = "لا يوجد شعار محفوظ." });

        if (!System.IO.File.Exists(path))
            return NotFound(new { message = "ملف الشعار غير موجود على القرص." });

        var bytes = await System.IO.File.ReadAllBytesAsync(path);
        var contentType = LogoStorage.GetContentType(path);
        return File(bytes, contentType);
    }

    /// <summary>حذف شعار الشركة الحالي (من القرص ومن قاعدة البيانات).</summary>
    [HttpDelete("logo")]
    [Authorize(Policy = Permissions.SettingsUpdate)]
    public async Task<IActionResult> DeleteLogo()
    {
        var existing = await _db.Settings.FindAsync(LogoPathKey);
        if (existing == null)
            return NotFound(new { message = "لا يوجد شعار محفوظ." });

        LogoStorage.Delete(existing.Value);
        _db.Settings.Remove(existing);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}