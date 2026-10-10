namespace Invoice.Infrastructure.Storage;

/// <summary>
/// يخزّن شعار الشركة على القرص المحلي ضمن مجلد التطبيق.
/// المسار: %LOCALAPPDATA%\CostProfitApp\Uploads\company\logo.<ext>
/// </summary>
public static class LogoStorage
{
    private const string FileBaseName = "logo";

    /// <summary>مجلد تخزين الشعار.</summary>
    public static string BaseDirectory =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CostProfitApp",
            "Uploads",
            "company");

    /// <summary>
    /// يحفظ الشعار الجديد. يحذف أي شعار قديم بنفس الاسم أولًا.
    /// </summary>
    /// <param name="stream">محتوى الصورة.</param>
    /// <param name="extension">الامتداد مع النقطة (مثل ".png" أو ".jpg").</param>
    /// <returns>المسار الكامل للملف المحفوظ.</returns>
    public static async Task<string> SaveAsync(Stream stream, string extension, CancellationToken ct = default)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (string.IsNullOrWhiteSpace(extension))
            throw new ArgumentException("الامتداد مطلوب.", nameof(extension));

        // تأكيد أن الامتداد يبدأ بنقطة
        if (!extension.StartsWith('.')) extension = "." + extension;
        extension = extension.ToLowerInvariant();

        Directory.CreateDirectory(BaseDirectory);

        // احذف أي ملف logo سابق بأي امتداد
        foreach (var existing in Directory.GetFiles(BaseDirectory, $"{FileBaseName}.*"))
        {
            try { File.Delete(existing); } catch { /* تجاهل */ }
        }

        var fullPath = Path.Combine(BaseDirectory, $"{FileBaseName}{extension}");

        await using var fs = File.Create(fullPath);
        await stream.CopyToAsync(fs, ct);

        return fullPath;
    }

    /// <summary>يقرأ محتوى ملف الشعار من المسار المحدد. يُعيد null إن لم يوجد.</summary>
    public static async Task<byte[]?> ReadBytesAsync(string? path, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        if (!File.Exists(path)) return null;

        return await File.ReadAllBytesAsync(path, ct);
    }

    /// <summary>يحذف ملف الشعار من المسار المحدد (إن وُجد).</summary>
    public static void Delete(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return;
        if (!File.Exists(path)) return;

        try { File.Delete(path); } catch { /* تجاهل */ }
    }

    /// <summary>يُعيد نوع MIME حسب امتداد الملف.</summary>
    public static string GetContentType(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return "application/octet-stream";

        var ext = Path.GetExtension(path).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "application/octet-stream"
        };
    }

    /// <summary>يتحقق أن الامتداد مسموح.</summary>
    public static bool IsAllowedExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension)) return false;
        if (!extension.StartsWith('.')) extension = "." + extension;

        var ext = extension.ToLowerInvariant();
        return ext is ".png" or ".jpg" or ".jpeg" or ".gif" or ".bmp" or ".webp";
    }
}