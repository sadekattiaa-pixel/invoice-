using Invoice.Domain.Entities;
using Invoice.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Invoice.Api.Controllers;

/// <summary>
/// نقاط نهاية API للأصناف — CRUD كامل مع قواعد العمل.
/// </summary>
[ApiController]
[Route("api/products")]
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
    public async Task<ActionResult<Product>> Create([FromBody] Product product)
    {
        // 1) التحقق من التصنيف
        if (!AllowedCategories.Contains(product.Category))
        {
            return BadRequest(new { message = "تصنيف غير معروف. المسموح: Batteries, Car Tires, Truck Tires." });
        }

        // 2) التحقق من وجود المورد
        var supplierExists = await _db.Suppliers.AnyAsync(s => s.Id == product.SupplierId);
        if (!supplierExists)
        {
            return BadRequest(new { message = "المورد غير موجود." });
        }

        // 3) قواعد الأعمال حسب التصنيف
        if (product.Category == "Batteries")
        {
            if (string.IsNullOrWhiteSpace(product.Code))
            {
                return BadRequest(new { message = "كود البطارية مطلوب." });
            }

            // البطارية: الكود هو نفسه الاسم (قاعدة المشروع الأصلي)
            product.Name = product.Code.Trim();
            product.Pattern = null;

            // منع تكرار الاسم (بلا اعتبار لحالة الأحرف أو المسافات)
            var existingName = await _db.Products
                .Where(p => p.Category == "Batteries")
                .Select(p => p.Name)
                .ToListAsync();

            var normalizedNew = NormalizeName(product.Name);
            if (existingName.Any(n => NormalizeName(n) == normalizedNew))
            {
                return BadRequest(new { message = "اسم البطارية مستخدم بالفعل." });
            }
        }
        else
        {
            // الإطارات: تحتاج اسم + نقشة
            if (string.IsNullOrWhiteSpace(product.Name))
            {
                return BadRequest(new { message = "اسم الإطار مطلوب." });
            }
            if (string.IsNullOrWhiteSpace(product.Pattern))
            {
                return BadRequest(new { message = "نقشة الإطار مطلوبة." });
            }

            // توليد كود فريد (hash) لتفادي التكرار
            product.Code = GenerateTireCode(product);
        }

        // 4) منع تكرار الكود
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

        if (product.Category == "Batteries")
        {
            if (string.IsNullOrWhiteSpace(product.Code))
            {
                return BadRequest(new { message = "كود البطارية مطلوب." });
            }
            product.Name = product.Code.Trim();
            product.Pattern = null;

            // التحقق من عدم التكرار مع أصناف أخرى
            var others = await _db.Products
                .Where(p => p.Category == "Batteries" && p.Id != id)
                .Select(p => p.Name)
                .ToListAsync();

            var normalizedNew = NormalizeName(product.Name);
            if (others.Any(n => NormalizeName(n) == normalizedNew))
            {
                return BadRequest(new { message = "اسم البطارية مستخدم من صنف آخر." });
            }
        }
        else
        {
            if (string.IsNullOrWhiteSpace(product.Name) || string.IsNullOrWhiteSpace(product.Pattern))
            {
                return BadRequest(new { message = "اسم الإطار والنقشة مطلوبان." });
            }
            product.Code = GenerateTireCode(product);
        }

        // التحقق من عدم تكرار الكود مع أصناف أخرى
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

    // ========= دوال مساعدة =========

    /// <summary>تطبيع الاسم: NFKC + إزالة مسافات زائدة + حالة موحّدة.</summary>
    private static string NormalizeName(string name)
    {
        var normalized = name.Normalize(System.Text.NormalizationForm.FormKC);
        var parts = normalized.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        return string.Join(" ", parts).ToLowerInvariant();
    }

    /// <summary>توليد كود فريد للإطار على أساس (التصنيف + الاسم + النقشة + المورد).</summary>
    private static string GenerateTireCode(Product product)
    {
        var identity = $"{product.Category}|{NormalizeName(product.Name)}|{NormalizeName(product.Pattern ?? "")}|{product.SupplierId}";
        var bytes = System.Text.Encoding.UTF8.GetBytes(identity);
        var hash = System.Security.Cryptography.SHA256.HashData(bytes);
        return "TIRE-" + Convert.ToHexString(hash).ToLowerInvariant();
    }
}