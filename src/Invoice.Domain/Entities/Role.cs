namespace Invoice.Domain.Entities;

/// <summary>
/// الدور — مجموعة صلاحيات تُمنح للمستخدمين.
/// </summary>
public class Role
{
    /// <summary>المعرّف الفريد.</summary>
    public int Id { get; set; }

    /// <summary>اسم الدور (فريد، مثال: Admin).</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>وصف الدور (اختياري).</summary>
    public string? Description { get; set; }

    /// <summary>هل دور نظامي (لا يُحذف)؟</summary>
    public bool IsSystem { get; set; }

    /// <summary>تاريخ الإنشاء.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>تاريخ آخر تعديل.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>رقم النسخة لمنع تعارض التعديلات.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // ========= العلاقات =========

    /// <summary>مستخدمو هذا الدور.</summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    /// <summary>صلاحيات هذا الدور.</summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}