using Invoice.Application.Calculations;
using Invoice.Application.Dtos;
using Invoice.Application.Exceptions;
using Invoice.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using InvoiceEntity = Invoice.Domain.Entities.Invoice;
using InvoiceItemEntity = Invoice.Domain.Entities.InvoiceItem;
using InvoicePaymentEntity = Invoice.Domain.Entities.InvoicePayment;
using InvoiceDiscountEntity = Invoice.Domain.Entities.InvoiceDiscount;
using InvoiceExpenseEntity = Invoice.Domain.Entities.InvoiceExpense;

namespace Invoice.Api.Controllers;

/// <summary>
/// نقاط نهاية API للفواتير — إنشاء، تعديل، أرشفة، حذف، تكرار.
/// </summary>
[ApiController]
[Route("api/invoices")]
public class InvoicesController : ControllerBase
{
    private readonly AppDbContext _db;

    public InvoicesController(AppDbContext db)
    {
        _db = db;
    }

    // ============================================================
    // قراءة
    // ============================================================

    /// <summary>جلب الفواتير مع الفلترة والبحث.</summary>
    [HttpGet]
    public async Task<ActionResult> GetAll(
        [FromQuery] string? status,
        [FromQuery] string? search,
        [FromQuery] int? supplierId,
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo)
    {
        var query = _db.Invoices.Include(i => i.Supplier).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(i => i.Status == status);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(i =>
                i.InvoiceNo.Contains(term) ||
                (i.SupplierInvoiceNo != null && i.SupplierInvoiceNo.Contains(term)));
        }

        if (supplierId.HasValue)
            query = query.Where(i => i.SupplierId == supplierId.Value);

        if (dateFrom.HasValue)
            query = query.Where(i => i.InvoiceDate >= dateFrom.Value);

        if (dateTo.HasValue)
            query = query.Where(i => i.InvoiceDate <= dateTo.Value);

        var result = await query
            .OrderByDescending(i => i.InvoiceDate)
            .ThenByDescending(i => i.Id)
            .Select(i => new
            {
                i.Id,
                i.InvoiceNo,
                i.SupplierInvoiceNo,
                i.InvoiceDate,
                i.Status,
                i.TotalPayments,
                i.TotalUsd,
                i.TotalProfit,
                i.NetCost,
                i.ProfitMargin,
                i.ArchivedAt,
                i.CreatedAt,
                i.SupplierId,
                SupplierName = i.Supplier != null ? i.Supplier.Name : null,
                TotalQty = i.Items.Sum(it => (decimal?)it.Quantity) ?? 0m
            })
            .ToListAsync();

        return Ok(result);
    }

    /// <summary>جلب فاتورة واحدة بكل بياناتها.</summary>
    [HttpGet("{id:long}")]
    public async Task<ActionResult> GetById(long id)
    {
        var invoice = await _db.Invoices
            .Include(i => i.Supplier)
            .Include(i => i.Items).ThenInclude(it => it.Product)
            .Include(i => i.Payments)
            .Include(i => i.Discounts)
            .Include(i => i.Expenses)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (invoice == null)
            return NotFound(new { message = "الفاتورة غير موجودة." });

        return Ok(invoice);
    }

    /// <summary>الحصول على رقم الفاتورة التالي المتوقع.</summary>
    [HttpGet("next-number")]
    public async Task<ActionResult> GetNextNumber()
    {
        var number = await GenerateInvoiceNumberAsync();
        return Ok(new { invoiceNo = number });
    }

    // ============================================================
    // حساب معاينة (بدون حفظ)
    // ============================================================

    [HttpPost("calculate")]
    public async Task<ActionResult> Calculate([FromBody] InvoiceSaveRequest request)
    {
        try
        {
            if (!await _db.Suppliers.AnyAsync(s => s.Id == request.SupplierId))
                return BadRequest(new { message = "المورد غير موجود." });

            var calc = RunCalculation(request);
            return Ok(calc);
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================================
    // إنشاء وتعديل
    // ============================================================

    /// <summary>إنشاء فاتورة جديدة (مسودة).</summary>
    [HttpPost]
    public Task<ActionResult> Create([FromBody] InvoiceSaveRequest request)
        => SaveOrUpdateAsync(null, request);

    /// <summary>تعديل مسودة موجودة.</summary>
    [HttpPut("{id:long}")]
    public Task<ActionResult> Update(long id, [FromBody] InvoiceSaveRequest request)
        => SaveOrUpdateAsync(id, request);

    // ============================================================
    // أرشفة
    // ============================================================

    /// <summary>أرشفة فاتورة — بعدها لا تقبل التعديل.</summary>
    [HttpPost("{id:long}/archive")]
    public async Task<ActionResult> Archive(long id, [FromBody] InvoiceSaveRequest request)
    {
        try
        {
            var existing = await _db.Invoices.FindAsync(id);
            if (existing == null)
                return NotFound(new { message = "الفاتورة غير موجودة." });

            if (existing.Status == "Archived")
                return BadRequest(new { message = "الفاتورة مؤرشفة بالفعل." });

            // نحفظ أي تعديل على المسودة أولًا
            var saveResult = await SaveOrUpdateAsync(id, request);
            if (saveResult is not OkObjectResult)
                return saveResult;

            // نُعيد التحميل ونؤرشف
            var invoice = await _db.Invoices.FindAsync(id);
            if (invoice == null)
                return NotFound(new { message = "الفاتورة غير موجودة." });

            invoice.Status = "Archived";
            invoice.ArchivedAt = DateTime.UtcNow;
            invoice.UpdatedAt = DateTime.UtcNow;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                id = invoice.Id,
                invoiceNo = invoice.InvoiceNo,
                status = invoice.Status,
                message = "تم أرشفة الفاتورة بنجاح."
            });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    // ============================================================
    // تكرار
    // ============================================================

    /// <summary>إنشاء نسخة مسودة من فاتورة موجودة.</summary>
    [HttpPost("{id:long}/duplicate")]
    public async Task<ActionResult> Duplicate(long id)
    {
        var original = await _db.Invoices
            .Include(i => i.Items)
            .Include(i => i.Payments)
            .Include(i => i.Discounts)
            .Include(i => i.Expenses)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (original == null)
            return NotFound(new { message = "الفاتورة غير موجودة." });

        var request = new InvoiceSaveRequest
        {
            SupplierId = original.SupplierId,
            SupplierInvoiceNo = original.SupplierInvoiceNo,
            InvoiceDate = original.InvoiceDate,
            Items = original.Items.OrderBy(it => it.LineNo).Select(it => new InvoiceItemRequest
            {
                ProductId = it.ProductId,
                Quantity = it.Quantity,
                PurchaseUsd = it.PurchaseUsd,
                SaleDzd = it.SaleDzd,
                Pallet = it.Pallet
            }).ToList(),
            Payments = original.Payments.OrderBy(p => p.LineNo)
                .Select(p => new InvoiceAmountRequest { Label = p.Label, Amount = p.Amount }).ToList(),
            Discounts = original.Discounts.OrderBy(d => d.LineNo)
                .Select(d => new InvoiceAmountRequest { Label = d.Label, Amount = d.Amount }).ToList(),
            Expenses = original.Expenses.OrderBy(e => e.LineNo)
                .Select(e => new InvoiceAmountRequest { Label = e.Label, Amount = e.Amount }).ToList(),
            Notes = original.Notes,
            Notes2 = original.Notes2
        };

        return await SaveOrUpdateAsync(null, request);
    }

    // ============================================================
    // حذف
    // ============================================================

    [HttpDelete("{id:long}")]
    public async Task<IActionResult> Delete(long id)
    {
        var invoice = await _db.Invoices.FindAsync(id);
        if (invoice == null)
            return NotFound(new { message = "الفاتورة غير موجودة." });

        // الحذف يُسلسل تلقائيًا إلى البنود والمدفوعات والخصومات والمصاريف.
        _db.Invoices.Remove(invoice);
        await _db.SaveChangesAsync();

        return NoContent();
    }

    // ============================================================
    // المنطق المشترك
    // ============================================================

    private async Task<ActionResult> SaveOrUpdateAsync(long? id, InvoiceSaveRequest request)
    {
        try
        {
            // ========= التحقق =========

            if (request.SupplierId <= 0)
                return BadRequest(new { message = "المورد مطلوب." });

            if (!await _db.Suppliers.AnyAsync(s => s.Id == request.SupplierId))
                return BadRequest(new { message = "المورد غير موجود." });

            if (request.Items == null || request.Items.Count == 0)
                return BadRequest(new { message = "يجب إضافة صنف واحد على الأقل." });

            var productIds = request.Items.Select(i => i.ProductId).Distinct().ToList();
            var existingProductIds = await _db.Products
                .Where(p => productIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();

            var missing = productIds.Except(existingProductIds).ToList();
            if (missing.Count > 0)
                return BadRequest(new { message = $"الأصناف التالية غير موجودة: {string.Join(", ", missing)}" });

            // ========= الحساب =========

            var calc = RunCalculation(request);

            // ========= بدء معاملة =========

            await using var transaction = await _db.Database.BeginTransactionAsync();

            InvoiceEntity invoice;

            if (id.HasValue)
            {
                // ----- تعديل مسودة -----
                var existing = await _db.Invoices
                    .Include(i => i.Items)
                    .Include(i => i.Payments)
                    .Include(i => i.Discounts)
                    .Include(i => i.Expenses)
                    .FirstOrDefaultAsync(i => i.Id == id.Value);

                if (existing == null)
                    return NotFound(new { message = "الفاتورة غير موجودة." });

                if (existing.Status != "Draft")
                    return BadRequest(new { message = "لا يمكن تعديل فاتورة مؤرشفة." });

                invoice = existing;

                ApplyCalculationToHeader(invoice, request, calc);
                invoice.UpdatedAt = DateTime.UtcNow;

                // احذف البنود القديمة قبل إعادة الإدراج
                _db.InvoiceItems.RemoveRange(invoice.Items);
                _db.InvoicePayments.RemoveRange(invoice.Payments);
                _db.InvoiceDiscounts.RemoveRange(invoice.Discounts);
                _db.InvoiceExpenses.RemoveRange(invoice.Expenses);

                await _db.SaveChangesAsync();
            }
            else
            {
                // ----- إنشاء جديد -----
                var invoiceNo = await GenerateInvoiceNumberAsync();

                invoice = new InvoiceEntity
                {
                    InvoiceNo = invoiceNo,
                    Status = "Draft",
                    CreatedAt = DateTime.UtcNow
                };
                ApplyCalculationToHeader(invoice, request, calc);

                _db.Invoices.Add(invoice);
                await _db.SaveChangesAsync();
            }

            // ========= إدراج البنود والمدفوعات وغيرها =========

            AddChildren(invoice.Id, calc, request);

            await _db.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new
            {
                id = invoice.Id,
                invoiceNo = invoice.InvoiceNo,
                status = invoice.Status,
                totalProfit = invoice.TotalProfit,
                message = "تم حفظ الفاتورة بنجاح."
            });
        }
        catch (DomainException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (DbUpdateConcurrencyException)
        {
            return Conflict(new { message = "تم تعديل هذه الفاتورة بواسطة مستخدم آخر. أعد التحميل." });
        }
    }

    /// <summary>ينفّذ الحساب من الطلب عبر InvoiceCalculator.</summary>
    private static InvoiceCalculationResult RunCalculation(InvoiceSaveRequest request)
    {
        var items = request.Items.Select(i => new InvoiceItemInput
        {
            ProductId = i.ProductId,
            Quantity = i.Quantity,
            PurchaseUsd = i.PurchaseUsd,
            SaleDzd = i.SaleDzd,
            Pallet = i.Pallet
        }).ToList();

        return InvoiceCalculator.Calculate(
            items,
            request.Payments.Select(p => p.Amount).ToList(),
            request.Discounts.Select(d => d.Amount).ToList(),
            request.Expenses.Select(e => e.Amount).ToList());
    }

    /// <summary>ينسخ قيم الطلب والحساب إلى رأس الفاتورة.</summary>
    private static void ApplyCalculationToHeader(
        InvoiceEntity invoice,
        InvoiceSaveRequest request,
        InvoiceCalculationResult calc)
    {
        invoice.SupplierId = request.SupplierId;
        invoice.SupplierInvoiceNo = request.SupplierInvoiceNo;
        invoice.InvoiceDate = request.InvoiceDate;
        invoice.TotalPayments = calc.TotalPayments;
        invoice.TotalDiscounts = calc.TotalDiscounts;
        invoice.NetPayments = calc.NetPayments;
        invoice.TotalExpenses = calc.TotalExpenses;
        invoice.NetCost = calc.NetCost;
        invoice.TotalUsd = calc.TotalUsd;
        invoice.ExchangeRate = calc.ExchangeRate;
        invoice.TotalSales = calc.TotalSales;
        invoice.TotalProfit = calc.TotalProfit;
        invoice.ProfitMargin = calc.ProfitMargin;
        invoice.Notes = request.Notes;
        invoice.Notes2 = request.Notes2;
    }

    /// <summary>يضيف البنود والمدفوعات والخصومات والمصاريف.</summary>
    private void AddChildren(long invoiceId, InvoiceCalculationResult calc, InvoiceSaveRequest request)
    {
        var lineNo = 1;
        foreach (var c in calc.Items)
        {
            _db.InvoiceItems.Add(new InvoiceItemEntity
            {
                InvoiceId = invoiceId,
                ProductId = c.ProductId,
                LineNo = lineNo++,
                Quantity = c.Quantity,
                Pallet = c.Pallet == 0 ? null : c.Pallet,
                PurchaseUsd = c.PurchaseUsd,
                PurchaseDzd = c.PurchaseDzd,
                SaleDzd = c.SaleDzd,
                TotalUsd = c.TotalUsd,
                TotalDzd = c.TotalDzd,
                TotalSale = c.TotalSale,
                UnitProfit = c.UnitProfit,
                TotalProfit = c.TotalProfit
            });
        }

        lineNo = 1;
        foreach (var p in request.Payments)
        {
            _db.InvoicePayments.Add(new InvoicePaymentEntity
            {
                InvoiceId = invoiceId,
                Label = p.Label,
                Amount = p.Amount,
                LineNo = lineNo++
            });
        }

        lineNo = 1;
        foreach (var d in request.Discounts)
        {
            _db.InvoiceDiscounts.Add(new InvoiceDiscountEntity
            {
                InvoiceId = invoiceId,
                Label = d.Label,
                Amount = d.Amount,
                LineNo = lineNo++
            });
        }

        lineNo = 1;
        foreach (var e in request.Expenses)
        {
            _db.InvoiceExpenses.Add(new InvoiceExpenseEntity
            {
                InvoiceId = invoiceId,
                Label = e.Label ?? "",
                Amount = e.Amount,
                LineNo = lineNo++
            });
        }
    }

    /// <summary>يولّد رقم فاتورة جديد بالشكل INV-YYYY-NNNN.</summary>
    private async Task<string> GenerateInvoiceNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"INV-{year}-";

        var existing = await _db.Invoices
            .Where(i => i.InvoiceNo.StartsWith(prefix))
            .Select(i => i.InvoiceNo)
            .ToListAsync();

        int maxNum = 0;
        foreach (var no in existing)
        {
            var suffix = no.Substring(prefix.Length);
            if (int.TryParse(suffix, out var n) && n > maxNum)
                maxNum = n;
        }

        return $"{prefix}{(maxNum + 1):D4}";
    }
}