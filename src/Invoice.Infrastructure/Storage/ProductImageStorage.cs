using System.Security.Cryptography;

namespace Invoice.Infrastructure.Storage;

/// <summary>
/// تخزين صور الأصناف على الخادم.
/// المسار: %LOCALAPPDATA%\CostProfitApp\Uploads\products\
/// أسماء الملفات: product_<guid>.<ext>
///
/// يُستخدم من ProductsController لرفع/قراءة/حذف صور الأصناف،
/// بحيث يمكن بعد ذلك تضمين الصور في تصدير PDF/Excel.
/// </summary>
public static class ProductImageStorage
{
    private static readonly string[] AllowedExtensions =
        { ".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp" };

    /// <summary>مجلد تخزين صور الأصناف على الخادم.</summary>
    public static string Folder
    {
        get
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CostProfitApp", "Uploads", "products");
            Directory.CreateDirectory(folder);
            return folder;
        }
    }

    /// <summary>هل الامتداد مدعوم؟</summary>
    public static bool IsAllowedExtension(string? extension)
    {
        if (string.IsNullOrWhiteSpace(extension)) return false;
        var ext = extension.Trim().ToLowerInvariant();
        if (!ext.StartsWith('.')) ext = "." + ext;
        return AllowedExtensions.Contains(ext);
    }

    /// <summary>
    /// يحفظ الصورة ويُرجع **المسار الكامل** على الخادم (يُخزَّن في قاعدة البيانات).
    /// </summary>
    public static async Task<string> SaveAsync(Stream stream, string extension, CancellationToken ct = default)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (!IsAllowedExtension(extension))
            throw new ArgumentException($"امتداد غير مدعوم: {extension}", nameof(extension));

        var ext = extension.Trim().ToLowerInvariant();
        if (!ext.StartsWith('.')) ext = "." + ext;

        var fileName = $"product_{Guid.NewGuid():N}{ext}";
        var fullPath = Path.Combine(Folder, fileName);

        await using var fileStream = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await stream.CopyToAsync(fileStream, ct);
        await fileStream.FlushAsync(ct);

        return fullPath;
    }

    /// <summary>
    /// يقرأ البايتات من مسار محفوظ (سواء مسار كامل أو اسم ملف قديم).
    /// يُرجع null إن لم يوجد الملف.
    /// </summary>
    public static async Task<byte[]?> ReadBytesAsync(string? pathOrFileName)
    {
        var full = ResolvePath(pathOrFileName);
        if (full == null) return null;

        try
        {
            return await File.ReadAllBytesAsync(full);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>يحذف الملف إن وُجد (يتجاهل الأخطاء).</summary>
    public static void Delete(string? pathOrFileName)
    {
        var full = ResolvePath(pathOrFileName);
        if (full == null) return;

        try
        {
            File.Delete(full);
        }
        catch
        {
            // نتجاهل — الحذف قد يفشل لأسباب خارجة عن سيطرتنا.
        }
    }

    /// <summary>يُرجع Content-Type من الامتداد.</summary>
    public static string GetContentType(string? pathOrFileName)
    {
        var ext = Path.GetExtension(pathOrFileName ?? "").ToLowerInvariant();
        return ext switch
        {
            ".png"  => "image/png",
            ".jpg"  => "image/jpeg",
            ".jpeg" => "image/jpeg",
            ".gif"  => "image/gif",
            ".bmp"  => "image/bmp",
            ".webp" => "image/webp",
            _       => "application/octet-stream"
        };
    }

    /// <summary>
    /// يُحوّل مدخلًا (مسار كامل أو اسم ملف قديم) إلى مسار كامل على القرص، أو null إن لم يوجد.
    /// - إذا كان المسار مطلقًا وموجودًا → يُرجَع كما هو.
    /// - إذا كان اسم ملف فقط → يُبحث داخل مجلد التخزين (توافق مع الوضع القديم).
    /// </summary>
    private static string? ResolvePath(string? pathOrFileName)
    {
        if (string.IsNullOrWhiteSpace(pathOrFileName)) return null;

        // مسار مطلق وموجود
        if (Path.IsPathRooted(pathOrFileName))
        {
            return File.Exists(pathOrFileName) ? pathOrFileName : null;
        }

        // اسم ملف فقط → ابحث في مجلد التخزين
        var candidate = Path.Combine(Folder, pathOrFileName);
        return File.Exists(candidate) ? candidate : null;
    }
}