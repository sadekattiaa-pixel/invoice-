namespace Invoice.Domain.Entities;

/// <summary>
/// ربط مستخدم بدور (علاقة Many-to-Many).
/// </summary>
public class UserRole
{
    /// <summary>معرّف المستخدم.</summary>
    public int UserId { get; set; }

    /// <summary>مرجع المستخدم.</summary>
    public User? User { get; set; }

    /// <summary>معرّف الدور.</summary>
    public int RoleId { get; set; }

    /// <summary>مرجع الدور.</summary>
    public Role? Role { get; set; }

    /// <summary>تاريخ الإسناد.</summary>
    public DateTime AssignedAt { get; set; }
}