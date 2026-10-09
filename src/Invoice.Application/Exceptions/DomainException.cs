namespace Invoice.Application.Exceptions;

/// <summary>
/// استثناء أساسي لقواعد المجال والتطبيق.
/// يُرفع عند انتهاك قاعدة عمل أو خطأ في الحساب.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}