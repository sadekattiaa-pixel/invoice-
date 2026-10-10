namespace Invoice.Api.Dtos.Auth;

/// <summary>
/// بيانات مختصرة عن المستخدم — تُعاد في /login و /me.
/// </summary>
public class UserInfoDto
{
    /// <summary>المعرّف الفريد.</summary>
    public int Id { get; set; }

    /// <summary>اسم المستخدم.</summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>الاسم الكامل.</summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>البريد الإلكتروني (اختياري).</summary>
    public string? Email { get; set; }

    /// <summary>أسماء الأدوار.</summary>
    public List<string> Roles { get; set; } = new();

    /// <summary>أكواد الصلاحيات (resource.action).</summary>
    public List<string> Permissions { get; set; } = new();
}