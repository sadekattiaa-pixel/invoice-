using System.ComponentModel.DataAnnotations;

namespace Invoice.Api.Dtos.Auth;

/// <summary>
/// جسم طلب تغيير كلمة المرور (للمستخدم الحالي).
/// </summary>
public class ChangePasswordRequest
{
    /// <summary>كلمة المرور الحالية للتحقق.</summary>
    [Required(ErrorMessage = "كلمة المرور الحالية مطلوبة.")]
    public string CurrentPassword { get; set; } = string.Empty;

    /// <summary>كلمة المرور الجديدة.</summary>
    [Required(ErrorMessage = "كلمة المرور الجديدة مطلوبة.")]
    [MinLength(6, ErrorMessage = "كلمة المرور يجب أن تكون 6 أحرف على الأقل.")]
    [MaxLength(200)]
    public string NewPassword { get; set; } = string.Empty;
}