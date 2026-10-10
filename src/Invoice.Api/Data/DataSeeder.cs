using Invoice.Api.Services;
using Invoice.Domain.Entities;
using Invoice.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Invoice.Api.Data;

/// <summary>
/// تهيئة بيانات أولية — صلاحيات، أدوار، مستخدم admin افتراضي.
/// آمنة عند التكرار: تتجاهل ما هو موجود مسبقًا.
/// </summary>
public static class DataSeeder
{
    /// <summary>اسم مستخدم admin الافتراضي.</summary>
    public const string DefaultAdminUsername = "admin";

    /// <summary>كلمة مرور admin الافتراضية — غيّرها بعد أول تسجيل دخول.</summary>
    public const string DefaultAdminPassword = "admin123";

    /// <summary>تعريفات الصلاحيات (الكود، الاسم المعروض، التصنيف).</summary>
    private static readonly (string Code, string Name, string Category)[] PermissionDefinitions =
    {
        ("suppliers.read",   "عرض الموردين",     "Suppliers"),
        ("suppliers.create", "إنشاء مورد",       "Suppliers"),
        ("suppliers.update", "تعديل مورد",       "Suppliers"),
        ("suppliers.delete", "حذف مورد",         "Suppliers"),

        ("products.read",    "عرض الأصناف",      "Products"),
        ("products.create",  "إنشاء صنف",        "Products"),
        ("products.update",  "تعديل صنف",        "Products"),
        ("products.delete",  "حذف صنف",          "Products"),

        ("invoices.read",    "عرض الفواتير",     "Invoices"),
        ("invoices.create",  "إنشاء فاتورة",     "Invoices"),
        ("invoices.update",  "تعديل فاتورة",     "Invoices"),
        ("invoices.delete",  "حذف فاتورة",       "Invoices"),
        ("invoices.archive", "أرشفة فاتورة",     "Invoices"),

        ("reports.read",     "عرض التقارير",     "Reports"),
        ("exports.read",     "تصدير PDF/Excel",  "Exports"),

        ("settings.read",    "عرض الإعدادات",    "Settings"),
        ("settings.update",  "تعديل الإعدادات",  "Settings"),

        ("users.read",       "عرض المستخدمين",   "Users"),
        ("users.create",     "إنشاء مستخدم",     "Users"),
        ("users.update",     "تعديل مستخدم",     "Users"),
        ("users.delete",     "حذف مستخدم",       "Users"),

        ("roles.read",       "عرض الأدوار",      "Roles"),
        ("roles.update",     "تعديل الأدوار",    "Roles"),
    };

    /// <summary>صلاحيات مستثناة من دور Accountant.</summary>
    private static readonly HashSet<string> AccountantExcluded = new()
    {
        "users.read", "users.create", "users.update", "users.delete",
        "roles.read", "roles.update",
        "settings.update"
    };

    /// <summary>الصلاحيات المسموحة لدور Viewer (قراءة فقط).</summary>
    private static readonly HashSet<string> ViewerAllowed = new()
    {
        "suppliers.read", "products.read", "invoices.read",
        "reports.read", "exports.read", "settings.read"
    };

    /// <summary>تنفيذ التهيئة الكاملة.</summary>
    public static async Task SeedAsync(AppDbContext db, IPasswordHasher hasher)
    {
        var permissionByCode = await SeedPermissionsAsync(db);
        var roleByName = await SeedRolesAsync(db);
        await LinkRolePermissionsAsync(db, permissionByCode, roleByName);
        await SeedAdminUserAsync(db, hasher, roleByName["Admin"]);
    }

    // ============ Permissions ============

    private static async Task<Dictionary<string, Permission>> SeedPermissionsAsync(AppDbContext db)
    {
        var existing = await db.Permissions.ToDictionaryAsync(p => p.Code);
        var added = false;

        foreach (var (code, name, category) in PermissionDefinitions)
        {
            if (existing.ContainsKey(code))
                continue;

            var perm = new Permission
            {
                Code = code,
                Name = name,
                Category = category,
                CreatedAt = DateTime.UtcNow
            };
            db.Permissions.Add(perm);
            existing[code] = perm;
            added = true;
        }

        if (added)
            await db.SaveChangesAsync();

        return existing;
    }

    // ============ Roles ============

    private static async Task<Dictionary<string, Role>> SeedRolesAsync(AppDbContext db)
    {
        var existing = await db.Roles.ToDictionaryAsync(r => r.Name);
        var added = false;

        var definitions = new (string Name, string Description)[]
        {
            ("Admin",      "مدير النظام — كل الصلاحيات"),
            ("Accountant", "محاسب — كل شيء ما عدا إدارة المستخدمين والأدوار والإعدادات"),
            ("Viewer",     "عارض — قراءة فقط")
        };

        foreach (var (name, description) in definitions)
        {
            if (existing.ContainsKey(name))
                continue;

            var role = new Role
            {
                Name = name,
                Description = description,
                IsSystem = true,
                CreatedAt = DateTime.UtcNow
            };
            db.Roles.Add(role);
            existing[name] = role;
            added = true;
        }

        if (added)
            await db.SaveChangesAsync();

        return existing;
    }

    // ============ Role-Permission Links ============

    private static async Task LinkRolePermissionsAsync(
        AppDbContext db,
        Dictionary<string, Permission> permissionByCode,
        Dictionary<string, Role> roleByName)
    {
        var allCodes = permissionByCode.Keys.ToList();
        var accountantCodes = allCodes.Where(c => !AccountantExcluded.Contains(c)).ToList();
        var viewerCodes = allCodes.Where(c => ViewerAllowed.Contains(c)).ToList();

        await SyncRolePermissionsAsync(db, roleByName["Admin"], allCodes, permissionByCode);
        await SyncRolePermissionsAsync(db, roleByName["Accountant"], accountantCodes, permissionByCode);
        await SyncRolePermissionsAsync(db, roleByName["Viewer"], viewerCodes, permissionByCode);

        await db.SaveChangesAsync();
    }

    private static async Task SyncRolePermissionsAsync(
        AppDbContext db,
        Role role,
        IEnumerable<string> desiredCodes,
        Dictionary<string, Permission> permissionByCode)
    {
        var currentLinks = await db.RolePermissions
            .Where(rp => rp.RoleId == role.Id)
            .ToListAsync();

        var desiredPermissionIds = desiredCodes
            .Where(c => permissionByCode.ContainsKey(c))
            .Select(c => permissionByCode[c].Id)
            .ToHashSet();

        var currentPermissionIds = currentLinks.Select(rp => rp.PermissionId).ToHashSet();

        // حذف الصلاحيات الزائدة
        foreach (var link in currentLinks.Where(rp => !desiredPermissionIds.Contains(rp.PermissionId)))
            db.RolePermissions.Remove(link);

        // إضافة الصلاحيات الناقصة
        foreach (var pid in desiredPermissionIds.Where(pid => !currentPermissionIds.Contains(pid)))
        {
            db.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = pid,
                AssignedAt = DateTime.UtcNow
            });
        }
    }

    // ============ Admin User ============

    private static async Task SeedAdminUserAsync(AppDbContext db, IPasswordHasher hasher, Role adminRole)
    {
        var exists = await db.Users.AnyAsync(u => u.Username == DefaultAdminUsername);
        if (exists)
            return;

        var admin = new User
        {
            Username = DefaultAdminUsername,
            PasswordHash = hasher.Hash(DefaultAdminPassword),
            FullName = "مدير النظام",
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(admin);
        await db.SaveChangesAsync();

        db.UserRoles.Add(new UserRole
        {
            UserId = admin.Id,
            RoleId = adminRole.Id,
            AssignedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }
}