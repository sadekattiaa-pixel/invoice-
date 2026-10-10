namespace Invoice.Api.Services;

/// <summary>
/// خدمة تجزئة كلمات المرور — واجهة لتمكين التبديل والاختبار.
/// </summary>
public interface IPasswordHasher
{
    /// <summary>توليد بصمة BCrypt من كلمة المرور الصريحة.</summary>
    string Hash(string password);

    /// <summary>التحقق من مطابقة كلمة مرور صريحة لبصمة مخزَّنة.</summary>
    bool Verify(string password, string hash);
}