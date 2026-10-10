namespace Invoice.Domain.Entities;

/// <summary>
/// الصلاحية — إذن منفرد بصيغة resource.action (مثال: suppliers.read).
/// </summary>
public class Permission
{
    /// <summary>المعرّف الفريد.</summary>
    public int Id { get; set; }

    /// <summary>كود الصلاحية (فريد، مثال: invoices.create).</summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>اسم الصلاحية (للعرض).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>وصف الصلاحية (اختياري).</summary>
    public string? Description { get; set; }

    /// <summary>تصنيف الصلاحية (مثال: Suppliers).</summary>
    public string? Category { get; set; }

    /// <summary>تاريخ الإنشاء.</summary>
    public DateTime CreatedAt { get; set; }

    // ========= العلاقات =========

    /// <summary>الأدوار التي تملك هذه الصلاحية.</summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}