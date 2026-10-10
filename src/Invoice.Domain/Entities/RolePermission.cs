namespace Invoice.Domain.Entities;

/// <summary>
/// ربط دور بصلاحية (علاقة Many-to-Many).
/// </summary>
public class RolePermission
{
    /// <summary>معرّف الدور.</summary>
    public int RoleId { get; set; }

    /// <summary>مرجع الدور.</summary>
    public Role? Role { get; set; }

    /// <summary>معرّف الصلاحية.</summary>
    public int PermissionId { get; set; }

    /// <summary>مرجع الصلاحية.</summary>
    public Permission? Permission { get; set; }

    /// <summary>تاريخ الإسناد.</summary>
    public DateTime AssignedAt { get; set; }
}