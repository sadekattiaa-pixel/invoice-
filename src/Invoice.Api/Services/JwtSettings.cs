namespace Invoice.Api.Services;

/// <summary>
/// إعدادات JWT — تُربط بقسم "Jwt" في appsettings.json.
/// </summary>
public class JwtSettings
{
    /// <summary>المفتاح السري لتوقيع التوكن (≥ 32 حرفًا).</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>جهة الإصدار (اسم الـ API).</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>الجهة المستهدفة (تطبيق Desktop).</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>عمر الـ Access Token بالدقائق.</summary>
    public int AccessTokenMinutes { get; set; } = 60;

    /// <summary>عمر الـ Refresh Token بالأيام.</summary>
    public int RefreshTokenDays { get; set; } = 7;
}