namespace Invoice.Domain.Entities;

/// <summary>
/// المستخدم — حساب للدخول إلى النظام.
/// </summary>
public class User
{
    /// <summary>المعرّف الفريد.</summary>
    public int Id { get; set; }

    /// <summary>اسم المستخدم (فريد، يُستخدم للدخول).</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>بصمة كلمة المرور (BCrypt).</summary>
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>الاسم الكامل.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>البريد الإلكتروني (اختياري).</summary>
    public string? Email { get; set; }

    /// <summary>رقم الهاتف (اختياري).</summary>
    public string? Phone { get; set; }

    /// <summary>هل الحساب مفعّل؟</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>تاريخ آخر تسجيل دخول (اختياري).</summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>تاريخ الإنشاء.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>تاريخ آخر تعديل.</summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>رقم النسخة لمنع تعارض التعديلات.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    // ========= العلاقات =========

    /// <summary>أدوار المستخدم.</summary>
    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
}