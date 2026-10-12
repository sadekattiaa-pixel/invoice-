namespace Invoice.Api.Security;

/// <summary>
/// قائمة الصلاحيات المعرَّفة في النظام بصيغة resource.action.
/// تُستخدم كأسماء Policies في [Authorize(Policy = "...")].
/// ⚠️ يجب أن تبقى متطابقة تمامًا مع DataSeeder.cs.
/// </summary>
public static class Permissions
{
    // ============ Suppliers ============
    public const string SuppliersRead   = "suppliers.read";
    public const string SuppliersCreate = "suppliers.create";
    public const string SuppliersUpdate = "suppliers.update";
    public const string SuppliersDelete = "suppliers.delete";

    // ============ Products ============
    public const string ProductsRead   = "products.read";
    public const string ProductsCreate = "products.create";
    public const string ProductsUpdate = "products.update";
    public const string ProductsDelete = "products.delete";

    // ============ Invoices ============
    public const string InvoicesRead    = "invoices.read";
    public const string InvoicesCreate  = "invoices.create";
    public const string InvoicesUpdate  = "invoices.update";
    public const string InvoicesDelete  = "invoices.delete";
    public const string InvoicesArchive = "invoices.archive";

    // ============ Reports / Exports ============
    public const string ReportsRead = "reports.read";
    public const string ExportsRead = "exports.read";

    // ============ Settings ============
    public const string SettingsRead   = "settings.read";
    public const string SettingsUpdate = "settings.update";

    // ============ Users ============
    public const string UsersRead   = "users.read";
    public const string UsersCreate = "users.create";
    public const string UsersUpdate = "users.update";
    public const string UsersDelete = "users.delete";

    // ============ Roles ============
    public const string RolesRead   = "roles.read";
    public const string RolesUpdate = "roles.update";

    /// <summary>كل الصلاحيات — تُسجَّل كـ Policies في Program.cs.</summary>
    public static readonly string[] All =
    {
        SuppliersRead, SuppliersCreate, SuppliersUpdate, SuppliersDelete,
        ProductsRead,  ProductsCreate,  ProductsUpdate,  ProductsDelete,
        InvoicesRead,  InvoicesCreate,  InvoicesUpdate,  InvoicesDelete, InvoicesArchive,
        ReportsRead,
        ExportsRead,
        SettingsRead, SettingsUpdate,
        UsersRead, UsersCreate, UsersUpdate, UsersDelete,
        RolesRead, RolesUpdate
    };
}