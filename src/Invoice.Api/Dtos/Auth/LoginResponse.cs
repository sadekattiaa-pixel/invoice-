namespace Invoice.Api.Dtos.Auth;

/// <summary>
/// استجابة تسجيل الدخول — تُحمل فيها التوكنات وبيانات المستخدم.
/// </summary>
public class LoginResponse
{
    /// <summary>الـ Access Token (يُرسل مع كل طلب).</summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>الـ Refresh Token (لتجديد الـ Access دون إعادة إدخال كلمة المرور).</summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>تاريخ انتهاء الـ Access Token (UTC).</summary>
    public DateTime AccessTokenExpiresAt { get; set; }

    /// <summary>بيانات المستخدم مع أدواره وصلاحياته.</summary>
    public UserInfoDto User { get; set; } = new();
}