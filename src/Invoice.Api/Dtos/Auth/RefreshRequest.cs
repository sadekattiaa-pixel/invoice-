using System.ComponentModel.DataAnnotations;

namespace Invoice.Api.Dtos.Auth;

/// <summary>
/// جسم طلب تجديد الـ Access Token.
/// </summary>
public class RefreshRequest
{
    /// <summary>الـ Refresh Token المُحصَّل عليه سابقًا.</summary>
    [Required(ErrorMessage = "Refresh Token مطلوب.")]
    public string RefreshToken { get; set; } = string.Empty;
}