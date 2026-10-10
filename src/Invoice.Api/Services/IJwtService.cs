using System.Security.Claims;
using Invoice.Domain.Entities;

namespace Invoice.Api.Services;

/// <summary>
/// خدمة إدارة توكنات JWT — توليد وتحقق.
/// </summary>
public interface IJwtService
{
    /// <summary>
    /// توليد Access Token قصير العمر يحمل هوية المستخدم + أدواره + صلاحياته.
    /// </summary>
    string GenerateAccessToken(User user, IEnumerable<string> roles, IEnumerable<string> permissions);

    /// <summary>
    /// توليد Refresh Token طويل العمر (يُستخدم لتجديد الـ Access Token).
    /// </summary>
    string GenerateRefreshToken(User user);

    /// <summary>
    /// التحقق من صلاحية توكن — يُعيد الـ Claims إن كان صالحًا، وإلا null.
    /// </summary>
    ClaimsPrincipal? ValidateToken(string token);
}