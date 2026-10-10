using System.ComponentModel.DataAnnotations;

namespace Invoice.Api.Dtos.Auth;

/// <summary>
/// جسم طلب تسجيل الدخول.
/// </summary>
public class LoginRequest
{
    /// <summary>اسم المستخدم.</summary>
    [Required(ErrorMessage = "اسم المستخدم مطلوب.")]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    /// <summary>كلمة المرور (نص صريح).</summary>
    [Required(ErrorMessage = "كلمة المرور مطلوبة.")]
    [MaxLength(200)]
    public string Password { get; set; } = string.Empty;
}