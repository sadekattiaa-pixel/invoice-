using Invoice.Infrastructure.Persistence;
using Invoice.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// تسجيل DbContext مع سلسلة الاتصال من appsettings.json
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// تسجيل خدمات التصدير
builder.Services.AddScoped<ReportDataService>();

// تفعيل Controllers
builder.Services.AddControllers();

// Swagger لواجهة تجريبية
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// تفعيل Swagger في بيئة التطوير فقط
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// إعادة التوجيه إلى HTTPS
app.UseHttpsRedirection();

// تفعيل التحقق من الصلاحيات (سنستخدمه لاحقًا)
app.UseAuthorization();

// ربط الـ Controllers
app.MapControllers();

app.Run();