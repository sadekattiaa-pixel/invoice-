namespace Invoice.Api.Services;

/// <summary>
/// تنفيذ تجزئة كلمات المرور باستخدام BCrypt.Net-Next.
/// </summary>
public class BcryptPasswordHasher : IPasswordHasher
{
    /// <summary>مستوى عمل BCrypt (12 = توازن جيد بين الأمان والسرعة).</summary>
    private const int WorkFactor = 12;

    public string Hash(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
            throw new ArgumentException("Password cannot be empty.", nameof(password));

        return BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);
    }

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        try
        {
            return BCrypt.Net.BCrypt.Verify(password, hash);
        }
        catch
        {
            // بصمة غير صالحة (مثلًا: نص عادي مخزَّن بالخطأ) → رفض.
            return false;
        }
    }
}