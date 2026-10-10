namespace Invoice.Application.Dtos;

/// <summary>
/// بيانات الشركة المستخرجة من جدول Settings — تُستخدم في ترويسة PDF و Excel.
/// </summary>
public class CompanyInfoDto
{
    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }

    /// <summary>مسار ملف الشعار على القرص (إن وُجد).</summary>
    public string? LogoPath { get; set; }

    /// <summary>محتوى ملف الشعار (يُحمَّل في ExportsController ويُعرض في المُصدِّرين).</summary>
    public byte[]? LogoBytes { get; set; }

    /// <summary>نوع MIME للشعار (مثلاً: image/png).</summary>
    public string? LogoContentType { get; set; }

    /// <summary>
    /// يبني الكائن من قاموس Settings (مفتاح/قيمة).
    /// المفاتيح المتوقعة: company_name, company_address, company_phone,
    /// company_email, company_website, company_logo_path
    /// </summary>
    public static CompanyInfoDto FromSettings(IReadOnlyDictionary<string, string> settings)
    {
        string Get(string key) =>
            settings.TryGetValue(key, out var v) ? (v ?? "") : "";

        return new CompanyInfoDto
        {
            Name = Get("company_name"),
            Address = Get("company_address"),
            Phone = Get("company_phone"),
            Email = Get("company_email"),
            Website = Get("company_website"),
            LogoPath = Get("company_logo_path")
        };
    }
}