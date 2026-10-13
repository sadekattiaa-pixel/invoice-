using Invoice.Api.Security;
using Invoice.Domain.Entities;
using Invoice.Infrastructure.Persistence;
using Invoice.Infrastructure.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Invoice.Api.Controllers;

/// <summary>
/// نقاط نهاية API للأصناف — CRUD كامل مع قواعد العمل.
/// كل نقطة تتطلب صلاحية من نوع products.*
///
/// قواعد موحّدة لكل التصنيفات:
///   - الكود إلزامي دائمًا.
///   - الاسم = الكود تلقائيًا.
///   - Batteries: لها حقول إضافية (Ah, L, W, H, Oh, Layout, Pallet, صورة)، والـ Pattern = null.
///   - Car/Truck Tires: لها حقل Pattern إلزامي، وباقي الحقول الإضافية = null.
///
/// الصور: تُخزَّن على الخادم في مجلد ProductImageStorage.Folder،
/// ويُخزَّن المسار الكامل في Product.LayoutImageFilename.
/// </summary>
[ApiController]
[Route("api/products")]
[Authorize]
public class ProductsController : ControllerBase
{
    private static readonly string[] AllowedCategories =
        { "Batteries", "Car Tires", "Truck Tires" };

    private readonly AppDbContext _db;

    public ProductsController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>جلب الأصناف مع إمكانية البحث والفلترة.</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.ProductsRead)]
    public async Task<ActionResult<List<Product>>> GetAll(
        [FromQuery] string? search,
        [FromQuery] string? category,
        [FromQuery] int? supplierId)
    {
        var query = _db.Products.Include(p => p.Supplier).AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(p =>
                p.Code.Contains(term) ||
                p.Name.Contains(term) ||
                (p.Pattern != null && p.Pattern.Contains(term)));
        }

        if (!string.IsNullOrWhiteSpace(category))
        {
            query = query.Where(p => p.Category == category);
        }

        if (supplierId.HasValue)
        {
            query = query.Where(p => p.SupplierId == supplierId.Value);
        }

        var products = await query
            .OrderBy(p => p.Name)
            .ToListAsync();

        return Ok(products);
    }

    /// <summary>جلب صنف واحد بالمعرّف.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.ProductsRead)]
    public async Task<ActionResult<Product>> GetById(int id)
    {
        var product = await _db.Products
            .Include(p => p.Supplier)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (product == null)
        {
            return NotFound(new { message = "الصنف غير موجود." });
        }
        return Ok(product);
    }

    /// <summary>إنشاء صنف جديد مع التحقق من قواعد العمل.</summary>
    [HttpPost]
    [Authorize(Policy = Permissions.ProductsCreate)]
    public async Task<ActionResult<Product>> Create([FromBody] Product product)
    {
        if (!AllowedCategories.Contains(product.Category))
        {
            return BadRequest(new { message = "تصنيف غير معروف. المسموح: Batteries, Car Tires, Truck Tires." });
        }

        var supplierExists = await _db.Suppliers.AnyAsync(s => s.Id == product.SupplierId);
        if (!supplierExists)
        {
            return BadRequest(new { message = "المورد غير موجود." });
        }

        if (string.IsNullOrWhiteSpace(product.Code))
        {
            return BadRequest(new { message = "كود الصنف مطلوب." });
        }

        product.Code = product.Code.Trim();
        product.Name = product.Code;

        if (product.Category == "Batteries")
        {
            product.Pattern = null;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(product.Pattern))
            {
                return BadRequest(new { message = "نقشة الإطار مطلوبة." });
            }

            product.Ah = null;
            product.L = null;
            product.W = null;
            product.H = null;
            product.Oh = null;
            product.Layout = null;
            product.LayoutImageFilename = null;
            product.Pallet = null;
        }

        // عند الإنشاء: نُصفّر مسار الصورة دائمًا — الصورة تُرفع بعد الإنشاء عبر POST /image
        product.LayoutImageFilename = null;

        var codeExists = await _db.Products.AnyAsync(p => p.Code == product.Code);
        if (codeExists)
        {
            return BadRequest(new { message = "كود الصنف مستخدم بالفعل." });
        }

        product.Id = 0;
        product.CreatedAt = DateTime.UtcNow;

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, product);
    }

    /// <summary>تعديل صنف موجود.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.ProductsUpdate)]
    public async Task<IActionResult> Update(int id, [FromBody] Product product)
    {
        if (id != product.Id)
        {
            return BadRequest(new { message = "المعرّف غير متطابق." });
        }

        if (!AllowedCategories.Contains(product.Category))
        {
            return BadRequest(new { message = "تصنيف غير معروف." });
        }

        var existing = await _db.Products.FindAsync(id);
        if (existing == null)
        {
            return NotFound(new { message = "الصنف غير موجود." });
        }

        var supplierExists = await _db.Suppliers.AnyAsync(s => s.Id == product.SupplierId);
        if (!supplierExists)
        {
            return BadRequest(new { message = "المورد غير موجود." });
        }

        if (string.IsNullOrWhiteSpace(product.Code))
        {
            return BadRequest(new { message = "كود الصنف مطلوب." });
        }

        product.Code = product.Code.Trim();
        product.Name = product.Code;

        if (product.Category == "Batteries")
        {
            product.Pattern = null;
        }
        else
        {
            if (string.IsNullOrWhiteSpace(product.Pattern))
            {
                return BadRequest(new { message = "نقشة الإطار مطلوبة." });
            }

            product.Ah = null;
            product.L = null;
            product.W = null;
            product.H = null;
            product.Oh = null;
            product.Layout = null;
            product.LayoutImageFilename = null;
            product.Pallet = null;
        }

        var codeTaken = await _db.Products.AnyAsync(p => p.Code == product.Code && p.Id != id);
        if (codeTaken)
        {
            return BadRequest(new { message = "كود الصنف مستخدم من صنف آخر." });
        }

        existing.Code = product.Code;
        existing.Name = product.Name;
        existing.Category = product.Category;
        existing.Pattern = product.Pattern;
        existing.Ah = product.Ah;
        existing.L = product.L;
        existing.W = product.W;
        existing.H = product.H;
        existing.Oh = product.Oh;
        existing.Layout = product.Layout;

        // لا نُعدّل مسار الصورة من خلال PUT — الصورة تُدار فقط عبر POST/DELETE /image
        // (إن أرسل Desktop مسارًا غير فارغ وكان فارغًا حاليًا، نُبقيه كما هو)
        // → لا نلمس existing.LayoutImageFilename هنا.

        existing.Pallet = product.Pallet;
        existing.SupplierId = product.SupplierId;
        existing.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "تم تعديل هذا الصنف بواسطة مستخدم آخر. أعد التحميل." });
        }

        return NoContent();
    }

    /// <summary>حذف صنف (مع منع الحذف إن كان مستخدمًا في فاتورة).</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.ProductsDelete)]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound(new { message = "الصنف غير موجود." });
        }

        var usedInInvoices = await _db.InvoiceItems.AnyAsync(ii => ii.ProductId == id);
        if (usedInInvoices)
        {
            var count = await _db.InvoiceItems.CountAsync(ii => ii.ProductId == id);
            return BadRequest(new { message = $"لا يمكن حذف الصنف: مستخدم في {count} بند فاتورة." });
        }

        // حذف ملف الصورة من القرص (إن وُجد)
        ProductImageStorage.Delete(product.LayoutImageFilename);

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ============================================================
    // صورة الصنف
    // ============================================================

    /// <summary>
    /// رفع صورة الصنف (multipart/form-data، حقل اسمه "file").
    /// يَحفظ الصورة على الخادم ويُحدّث Product.LayoutImageFilename بالمسار الكامل.
    /// يستبدل الصورة القديمة إن وُجدت.
    /// </summary>
    [HttpPost("{id:int}/image")]
    [Authorize(Policy = Permissions.ProductsUpdate)]
    [RequestSizeLimit(5 * 1024 * 1024)] // 5 MB
    public async Task<IActionResult> UploadImage(int id, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { message = "لم يتم إرسال ملف." });

        var extension = Path.GetExtension(file.FileName);
        if (!ProductImageStorage.IsAllowedExtension(extension))
            return BadRequest(new { message = "امتداد غير مدعوم. المسموح: png, jpg, jpeg, gif, bmp, webp." });

        var product = await _db.Products.FindAsync(id);
        if (product == null)
            return NotFound(new { message = "الصنف غير موجود." });

        // احذف القديمة
        ProductImageStorage.Delete(product.LayoutImageFilename);

        // احفظ الجديدة
        string savedPath;
        await using (var stream = file.OpenReadStream())
        {
            savedPath = await ProductImageStorage.SaveAsync(stream, extension);
        }

        product.LayoutImageFilename = savedPath;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return Ok(new
        {
            message = "تم رفع الصورة بنجاح.",
            path = savedPath
        });
    }

    /// <summary>جلب صورة الصنف الحالية (صورة).</summary>
    [HttpGet("{id:int}/image")]
    [Authorize(Policy = Permissions.ProductsRead)]
    public async Task<IActionResult> GetImage(int id)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new { p.LayoutImageFilename })
            .FirstOrDefaultAsync();

        if (product == null)
            return NotFound(new { message = "الصنف غير موجود." });

        if (string.IsNullOrWhiteSpace(product.LayoutImageFilename))
            return NotFound(new { message = "لا توجد صورة لهذا الصنف." });

        var bytes = await ProductImageStorage.ReadBytesAsync(product.LayoutImageFilename);
        if (bytes == null || bytes.Length == 0)
            return NotFound(new { message = "ملف الصورة غير موجود على الخادم." });

        var contentType = ProductImageStorage.GetContentType(product.LayoutImageFilename);
        return File(bytes, contentType);
    }

    /// <summary>حذف صورة الصنف (من القرص ومن قاعدة البيانات).</summary>
    [HttpDelete("{id:int}/image")]
    [Authorize(Policy = Permissions.ProductsUpdate)]
    public async Task<IActionResult> DeleteImage(int id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null)
            return NotFound(new { message = "الصنف غير موجود." });

        if (string.IsNullOrWhiteSpace(product.LayoutImageFilename))
            return NoContent(); // لا شيء لحذفه

        ProductImageStorage.Delete(product.LayoutImageFilename);
        product.LayoutImageFilename = null;
        product.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        return NoContent();
    }
}