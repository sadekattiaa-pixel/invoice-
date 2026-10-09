using Invoice.Domain.Entities;
using Invoice.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Invoice.Api.Controllers;

/// <summary>
/// نقاط نهاية API للإعدادات — مخزن مفتاح/قيمة.
/// </summary>
[ApiController]
[Route("api/settings")]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _db;

    public SettingsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>جلب كل الإعدادات كمفتاح/قيمة.</summary>
    [HttpGet]
    public async Task<ActionResult> GetAll()
    {
        var settings = await _db.Settings
            .AsNoTracking()
            .ToDictionaryAsync(s => s.Key, s => s.Value ?? "");

        return Ok(settings);
    }

    /// <summary>حفظ/تحديث مجموعة من الإعدادات دفعة واحدة.</summary>
    [HttpPut]
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
}