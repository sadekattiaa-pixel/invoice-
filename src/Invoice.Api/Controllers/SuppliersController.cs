using Invoice.Api.Security;
using Invoice.Domain.Entities;
using Invoice.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Invoice.Api.Controllers;

/// <summary>
/// نقاط نهاية API للموردين — CRUD كامل.
/// كل نقطة تتطلب صلاحية من نوع suppliers.*
/// </summary>
[ApiController]
[Route("api/suppliers")]
[Authorize]
public class SuppliersController : ControllerBase
{
    private readonly AppDbContext _db;

    public SuppliersController(AppDbContext db)
    {
        _db = db;
    }

    /// <summary>جلب كل الموردين مرتبين بالاسم.</summary>
    [HttpGet]
    [Authorize(Policy = Permissions.SuppliersRead)]
    public async Task<ActionResult<List<Supplier>>> GetAll()
    {
        var suppliers = await _db.Suppliers
            .OrderBy(s => s.Name)
            .ToListAsync();

        return Ok(suppliers);
    }

    /// <summary>جلب مورد واحد بالمعرّف.</summary>
    [HttpGet("{id:int}")]
    [Authorize(Policy = Permissions.SuppliersRead)]
    public async Task<ActionResult<Supplier>> GetById(int id)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier == null)
        {
            return NotFound(new { message = "المورد غير موجود." });
        }
        return Ok(supplier);
    }

    /// <summary>إنشاء مورد جديد.</summary>
    [HttpPost]
    [Authorize(Policy = Permissions.SuppliersCreate)]
    public async Task<ActionResult<Supplier>> Create([FromBody] Supplier supplier)
    {
        var exists = await _db.Suppliers.AnyAsync(s => s.Code == supplier.Code);
        if (exists)
        {
            return BadRequest(new { message = "كود المورد مستخدم بالفعل." });
        }

        supplier.Id = 0;
        supplier.CreatedAt = DateTime.UtcNow;

        _db.Suppliers.Add(supplier);
        await _db.SaveChangesAsync();

        return CreatedAtAction(nameof(GetById), new { id = supplier.Id }, supplier);
    }

    /// <summary>تعديل مورد موجود.</summary>
    [HttpPut("{id:int}")]
    [Authorize(Policy = Permissions.SuppliersUpdate)]
    public async Task<IActionResult> Update(int id, [FromBody] Supplier supplier)
    {
        if (id != supplier.Id)
        {
            return BadRequest(new { message = "المعرّف غير متطابق." });
        }

        var existing = await _db.Suppliers.FindAsync(id);
        if (existing == null)
        {
            return NotFound(new { message = "المورد غير موجود." });
        }

        var codeTaken = await _db.Suppliers.AnyAsync(s => s.Code == supplier.Code && s.Id != id);
        if (codeTaken)
        {
            return BadRequest(new { message = "كود المورد مستخدم بالفعل من مورد آخر." });
        }

        existing.Code = supplier.Code;
        existing.Name = supplier.Name;
        existing.Address = supplier.Address;
        existing.Email = supplier.Email;
        existing.Phone = supplier.Phone;
        existing.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "تم تعديل هذا المورد بواسطة مستخدم آخر. أعد التحميل." });
        }

        return NoContent();
    }

    /// <summary>حذف مورد.</summary>
    [HttpDelete("{id:int}")]
    [Authorize(Policy = Permissions.SuppliersDelete)]
    public async Task<IActionResult> Delete(int id)
    {
        var supplier = await _db.Suppliers.FindAsync(id);
        if (supplier == null)
        {
            return NotFound(new { message = "المورد غير موجود." });
        }

        var hasProducts = await _db.Products.AnyAsync(p => p.SupplierId == id);
        var hasInvoices = await _db.Invoices.AnyAsync(i => i.SupplierId == id);

        if (hasProducts || hasInvoices)
        {
            return BadRequest(new
            {
                message = $"لا يمكن حذف المورد: لديه {await _db.Products.CountAsync(p => p.SupplierId == id)} صنف و{await _db.Invoices.CountAsync(i => i.SupplierId == id)} فاتورة."
            });
        }

        _db.Suppliers.Remove(supplier);
        await _db.SaveChangesAsync();

        return NoContent();
    }
}