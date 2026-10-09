using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Invoice.Infrastructure.Persistence;

/// <summary>
/// مصنع يسمح لأدوات dotnet-ef بإنشاء AppDbContext في وضع التصميم
/// (عند تشغيل أوامر migrations بدون تطبيق يعمل).
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // اتصال مؤقت يُستخدم فقط عند تشغيل أوامر dotnet ef.
        // الاتصال الفعلي سيُقرأ من appsettings.json في مشروع Api لاحقًا.
        const string connectionString =
            "Server=localhost\\SQLEXPRESS;Database=InvoiceDb;Trusted_Connection=True;TrustServerCertificate=True;";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}