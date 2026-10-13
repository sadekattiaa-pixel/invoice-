using Invoice.Api.Security;
using Invoice.Domain.Entities;
using Invoice.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Invoice.Api.Controllers;

/// <summary>
/// نقاط نهاية API للأصناف — CRUD كامل مع قواعد العمل.
/// كل نقطة تتطلب صلاحية من نوع products.*
///
/// قواعد موحّدة لكل التصنيفات (بند 14):
///   - الكود إلزامي دائمًا.
///   - الاسم = الكود تلقائيًا.
///   - Batteries: لها حقول إضافية (Ah, L, W, H, Oh, Layout, Pallet, صورة)، والـ Pattern = null.
///   - Car/Truck Tires: لها حقل Pattern إلزامي، وباقي الحقول الإضافية = null.
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

        // الكود إلزامي في كل التصنيفات (بند 14)
        if (string.IsNullOrWhiteSpace(product.Code))
        {
            return BadRequest(new { message = "كود الصنف مطلوب." });
        }

        // الاسم = الكود تلقائيًا في كل التصنيفات
        product.Code = product.Code.Trim();
        product.Name = product.Code;

        if (product.Category == "Batteries")
        {
            // البطارية: لا نقشة
            product.Pattern = null;
        }
        else
        {
            // الإطارات: النقشة إلزامية
            if (string.IsNullOrWhiteSpace(product.Pattern))
            {
                return BadRequest(new { message = "نقشة الإطار مطلوبة." });
            }

            // تنظيف الحقول الإضافية غير المتعلقة بالإطارات
            product.Ah = null;
            product.L = null;
            product.W = null;
            product.H = null;
            product.Oh = null;
            product.Layout = null;
            product.LayoutImageFilename = null;
            product.Pallet = null;
        }

        // التحقق من عدم تكرار الكود
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

        // الكود إلزامي في كل التصنيفات (بند 14)
        if (string.IsNullOrWhiteSpace(product.Code))
        {
            return BadRequest(new { message = "كود الصنف مطلوب." });
        }

        // الاسم = الكود تلقائيًا
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

        // التحقق من عدم تكرار الكود (باستثناء الصنف نفسه)
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
        existing.LayoutImageFilename = product.LayoutImageFilename;
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

        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}