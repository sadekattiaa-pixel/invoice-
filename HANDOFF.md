HANDOFF — مشروع Invoice-CSharp
آخر تحديث: 11 أكتوبر 2026
الإصدار الحالي: نسخة تطويرية (لم تُرقّم بعد)
مسار المشروع: C:\Users\ADNAN\projects\cost_profit\Invoice-CSharp

═══════════════════════════════════════════════════════════════
1. نظرة عامة
═══════════════════════════════════════════════════════════════

الهدف
─────
إعادة كتابة تطبيق CostProfitApp (Python/PySide6) كنظام Client-Server بلغة C#/.NET 8:
- الخادم: ASP.NET Core Web API + SQL Server Express.
- العميل: Windows Forms يتصل بالخادم عبر HTTP.
- الهدف: العمل على شبكة LAN (حتى 10 أجهزة) بدون إنترنت.

المستخدم
────────
- مبتدئ في البرمجة.
- يعمل من VS Code (ليس Visual Studio).
- يحتاج خطوة واحدة في كل رسالة.
- يفضّل الكود الكامل عند التعديل (لا يقول "عدّل السطر X").

الشركة الناشرة
──────────────
Alintiqaa Programming & Digital Solutions

═══════════════════════════════════════════════════════════════
2. القرارات التقنية المعتمدة
═══════════════════════════════════════════════════════════════

البند                       القرار
─────────────────────────  ─────────────────────────────────────────
.NET SDK                    8.0.424 LTS
Git                         2.56.0
قاعدة البيانات              SQL Server Express (MSSQL$SQLEXPRESS)
اسم القاعدة                 InvoiceDb
EF Core                     8.0.31
PDF                         QuestPDF 2026.9.1 (Community) — يدعم العربية
Excel                       ClosedXML 0.105.1
dotnet-ef                   8.0.31 (أداة عامة)
الواجهة                     WinForms (code-first — لا مصمم مرئي)
المصادقة                    JWT + BCrypt ✅ مُنفَّذ
النقل                       HTTP + JWT Bearer
حزمة JWT                    Microsoft.AspNetCore.Authentication.JwtBearer 8.0.11
حزمة BCrypt                 BCrypt.Net-Next 4.2.0
الفرع الرئيسي               main
الريموت                     لا يوجد (GitHub لم يُربط بعد)

═══════════════════════════════════════════════════════════════
3. البنية الحالية
═══════════════════════════════════════════════════════════════

3.1 شجرة المجلدات
──────────────────

Invoice-CSharp/
├── Invoice.sln
├── .gitignore
├── HANDOFF.md
├── src/
│   ├── Invoice.Domain/               (class library)
│   │   └── Entities/
│   │       ├── Supplier.cs
│   │       ├── Product.cs
│   │       ├── Invoice.cs
│   │       ├── InvoiceItem.cs
│   │       ├── InvoicePayment.cs
│   │       ├── InvoiceDiscount.cs
│   │       ├── InvoiceExpense.cs
│   │       ├── Settings.cs
│   │       ├── User.cs               ← جديد (Auth)
│   │       ├── Role.cs               ← جديد (Auth)
│   │       ├── Permission.cs         ← جديد (Auth)
│   │       ├── UserRole.cs           ← جديد (Auth)
│   │       └── RolePermission.cs     ← جديد (Auth)
│   │
│   ├── Invoice.Application/          (class library)
│   │   ├── Exceptions/
│   │   │   └── DomainException.cs
│   │   ├── Calculations/
│   │   │   ├── InvoiceCalculator.cs
│   │   │   ├── InvoiceItemInput.cs
│   │   │   └── InvoiceCalculationResult.cs
│   │   └── Dtos/
│   │       ├── InvoiceSaveRequest.cs
│   │       ├── InvoiceItemRequest.cs
│   │       ├── InvoiceAmountRequest.cs
│   │       ├── CompanyInfoDto.cs
│   │       ├── ReportColumn.cs
│   │       ├── ReportRow.cs
│   │       ├── ReportSummaryItem.cs
│   │       └── ReportTableDto.cs
│   │
│   ├── Invoice.Infrastructure/       (class library)
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── DesignTimeDbContextFactory.cs
│   │   │   └── Migrations/
│   │   │       ├── 20261009005315_InitialCreate.cs
│   │   │       ├── 20261009230747_AddSettingsTable.cs
│   │   │       ├── 20261010225958_AddAuthTables.cs    ← جديد (Auth)
│   │   │       └── AppDbContextModelSnapshot.cs
│   │   ├── Exporters/
│   │   │   ├── InvoicePdfExporter.cs
│   │   │   ├── InvoiceExcelExporter.cs
│   │   │   ├── ReportPdfExporter.cs
│   │   │   └── ReportExcelExporter.cs
│   │   ├── Storage/
│   │   │   └── LogoStorage.cs
│   │   └── Services/
│   │       └── ReportDataService.cs
│   │
│   ├── Invoice.Api/                  (webapi)
│   │   ├── Controllers/
│   │   │   ├── SuppliersController.cs
│   │   │   ├── ProductsController.cs
│   │   │   ├── InvoicesController.cs
│   │   │   ├── ReportsController.cs
│   │   │   ├── ExportsController.cs
│   │   │   ├── SettingsController.cs
│   │   │   └── AuthController.cs                       ← جديد (Auth)
│   │   ├── Data/                                       ← جديد (Auth)
│   │   │   └── DataSeeder.cs
│   │   ├── Dtos/                                       ← جديد (Auth)
│   │   │   └── Auth/
│   │   │       ├── LoginRequest.cs
│   │   │       ├── LoginResponse.cs
│   │   │       ├── UserInfoDto.cs
│   │   │       ├── RefreshRequest.cs
│   │   │       └── ChangePasswordRequest.cs
│   │   ├── Services/                                   ← جديد (Auth)
│   │   │   ├── JwtSettings.cs
│   │   │   ├── IJwtService.cs
│   │   │   ├── JwtService.cs
│   │   │   ├── IPasswordHasher.cs
│   │   │   └── BcryptPasswordHasher.cs
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── appsettings.Development.json
│   │
│   └── Invoice.Desktop/              (winforms — لم يُعدّل)
│       ├── Program.cs
│       └── Form1.cs
│
└── tests/
    └── Invoice.Tests/
        ├── PdfArabicSmokeTest.cs
        └── UnitTest1.cs

3.2 مراجع المشروعات (معماري صارم)
─────────────────────────────────

Domain          ← لا مراجع
Application     → Domain
Infrastructure  → Application + Domain
Api             → Infrastructure + Application + Domain
Desktop         → Application + Domain  (لا Infrastructure)
Tests           → كل المشروعات

═══════════════════════════════════════════════════════════════
4. قاعدة البيانات
═══════════════════════════════════════════════════════════════

4.1 الاتصال
───────────
السيرفر:         localhost\SQLEXPRESS
القاعدة:         InvoiceDb
المصادقة:        Trusted_Connection (Windows auth)

Connection String:
──────────────────
Server=localhost\SQLEXPRESS;Database=InvoiceDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=120

ملاحظة: Connect Timeout=120 ضرورية — SQL Express يستغرق وقتًا
في أول اتصال بعد بداية باردة (تجاوز المهلة الافتراضية 15s).

4.2 الجداول (14 جدولًا)
───────────────────────

الجدول                  الوصف
──────────────────────  ─────────────────────────────────────
Settings                مفتاح/قيمة للإعدادات + مسار الشعار
Suppliers               الموردون
Products                الأصناف (بطاريات + إطارات)
Invoices                الفواتير
InvoiceItems            بنود الفواتير
InvoicePayments         دفعات
InvoiceDiscounts        خصومات
InvoiceExpenses         مصاريف
Users                   المستخدمون                      ← Auth
Roles                   الأدوار (Admin, Accountant, Viewer)  ← Auth
Permissions             الصلاحيات (resource.action)     ← Auth
UserRoles               ربط مستخدم-دور (M2M)            ← Auth
RolePermissions         ربط دور-صلاحية (M2M)            ← Auth
__EFMigrationsHistory   سجل EF (نظامي)

4.3 مفاتيح Settings المستخدمة فعليًا
────────────────────────────────────
- company_name
- company_address
- company_phone
- company_email
- company_website
- company_logo_path

4.4 العلاقات
────────────
Products.SupplierId             → Suppliers.Id       (Restrict)
Invoices.SupplierId             → Suppliers.Id       (Restrict)
InvoiceItems.InvoiceId          → Invoices.Id        (Cascade)
InvoiceItems.ProductId          → Products.Id        (Restrict)
InvoicePayments.InvoiceId       → Invoices.Id        (Cascade)
InvoiceDiscounts.InvoiceId      → Invoices.Id        (Cascade)
InvoiceExpenses.InvoiceId       → Invoices.Id        (Cascade)
UserRoles.UserId                → Users.Id           (Cascade)   ← Auth
UserRoles.RoleId                → Roles.Id           (Cascade)   ← Auth
RolePermissions.RoleId          → Roles.Id           (Cascade)   ← Auth
RolePermissions.PermissionId    → Permissions.Id     (Cascade)   ← Auth

4.5 فهارس فريدة
───────────────
Suppliers.Code                  UNIQUE
Products.Code                   UNIQUE
Invoices.InvoiceNo              UNIQUE
Users.Username                  UNIQUE                              ← Auth
Roles.Name                      UNIQUE                              ← Auth
Permissions.Code                UNIQUE                              ← Auth

4.6 حقول RowVersion
───────────────────
- Suppliers
- Products
- Invoices
- Users                                                          ← Auth
- Roles                                                          ← Auth

═══════════════════════════════════════════════════════════════
5. قواعد العمل المُنفَّذة
═══════════════════════════════════════════════════════════════

5.1 الأصناف (Products)
──────────────────────
- التصنيفات المسموحة: Batteries, Car Tires, Truck Tires.
- بطارية: الكود إلزامي، الاسم = الكود تلقائيًا، النقشة = null.
  الاسم فريد (بعد NFKC + casefold).
- إطار: الاسم + النقشة إلزاميان، الكود يُولَّد SHA256
  (أول 32 حرف hex + بادئة TIRE-) = 37 حرفًا.

5.2 الفواتير (Invoices)
───────────────────────
- دورة الحياة: Draft → Archived (لا رجوع).
- الأرشفة: تتطلب حفظ أي تعديلات أولًا؛ الفاتورة المؤرشفة لا تُعدَّل.
- الترقيم: INV-{year}-{NNNN} تلقائي متسلسل.
- الحسابات: تُطبَّق في InvoiceCalculator مع MidpointRounding.AwayFromZero.

5.3 معادلات الحساب (مطابقة للنسخة Python الأصلية)
─────────────────────────────────────────────────

net_payments  = total_payments − total_discounts
net_cost      = net_payments + total_expenses
exchange_rate = net_cost / total_usd
purchase_dzd  = purchase_usd × exchange_rate
unit_profit   = sale_dzd − purchase_dzd
total_profit  = quantity × unit_profit
profit_margin = (total_profit / total_sales) × 100

الدقة:
- Money: 2 منازل
- Rate: 6 منازل
- Percent: 4 منازل

5.4 التحقق (Validation)
───────────────────────
- الكميات والأسعار > 0.
- المدفوعات/الخصومات/المصاريف ≥ 0.
- الخصومات ≤ المدفوعات.
- total_usd > 0.

5.5 المصادقة والصلاحيات (Auth) — ✅ مُنفَّذ
──────────────────────────────────────────
- كلمات المرور: BCrypt (Work Factor = 12).
- التوكنات: JWT HMAC-SHA256.
  - Access Token: 60 دقيقة.
  - Refresh Token: 7 أيام.
- Claims محمولة في التوكن:
  - sub              → معرّف المستخدم (Id)
  - unique_name      → اسم المستخدم
  - role             → اسم الدور (ClaimTypes.Role)
  - permission       → كود الصلاحية (claim متعدد)
  - full_name        → الاسم الكامل
  - token_type       → "access" أو "refresh"
  - jti              → معرّف فريد للتوكن
- الأدوار الافتراضية:
  - Admin       → 23 صلاحية (كل شيء)
  - Accountant  → 16 صلاحية (كل شيء ما عدا users/roles/settings.update)
  - Viewer      → 6 صلاحيات (قراءة فقط)
- الصلاحيات بصيغة resource.action.
- مستخدم admin افتراضي: admin / admin123
  ⚠️ يُنصح بتغييرها فورًا بعد أول تسجيل دخول.

═══════════════════════════════════════════════════════════════
6. الصلاحيات المعرَّفة (23 صلاحية)
═══════════════════════════════════════════════════════════════

المورد      الصلاحيات                                    العدد
──────────  ──────────────────────────────────────────  ─────
suppliers   read, create, update, delete                 4
products    read, create, update, delete                 4
invoices    read, create, update, delete, archive        5
reports     read                                         1
exports     read                                         1
settings    read, update                                 2
users       read, create, update, delete                 4
roles       read, update                                 2
───────────────────────────────────────────────────────────
الإجمالي:                                                23

توزيع الأدوار:
- Admin       → 23/23
- Accountant  → 16/23 (يستثني: users.* + roles.* + settings.update)
- Viewer      → 6/23  (suppliers.read, products.read, invoices.read,
                       reports.read, exports.read, settings.read)

═══════════════════════════════════════════════════════════════
7. API المُنفَّذ (كل النقاط مُختبَرة)
═══════════════════════════════════════════════════════════════

7.1 Suppliers (/api/suppliers)
──────────────────────────────
Method  Path          الوصف
──────  ────────────  ─────────────────────────────
GET     /             كل الموردين
GET     /{id}         مورد واحد
POST    /             إنشاء
PUT     /{id}         تعديل
DELETE  /{id}         حذف (يُمنع إن له أصناف/فواتير)

7.2 Products (/api/products)
────────────────────────────
Method  Path          الوصف
──────  ────────────  ─────────────────────────────
GET     /             كل الأصناف (بحث + فلترة)
GET     /{id}         صنف واحد
POST    /             إنشاء (مع قواعد البطارية/الإطار)
PUT     /{id}         تعديل
DELETE  /{id}         حذف (يُمنع إن استُخدم في فاتورة)

7.3 Invoices (/api/invoices)
────────────────────────────
Method  Path                  الوصف
──────  ────────────────────  ─────────────────────────────
GET     /                     الفواتير (فلترة status/supplierId/date)
GET     /{id}                 فاتورة كاملة (projection)
GET     next-number           الرقم التالي
POST    calculate             حساب معاينة بدون حفظ
POST    /                     إنشاء مسودة
PUT     /{id}                 تعديل مسودة
POST    /{id}/archive         أرشفة
POST    /{id}/duplicate       تكرار كمسودة جديدة
DELETE  /{id}                 حذف (cascade)

7.4 Auth (/api/auth) — ✅ جديد
──────────────────────────────
Method  Path                Auth              الوصف
──────  ──────────────────  ────────────────  ────────────────────────
POST    login               AllowAnonymous    تسجيل دخول → Access + Refresh + UserInfo
POST    refresh             AllowAnonymous    تجديد Access من Refresh Token
GET     me                  [Authorize]       بيانات المستخدم الحالي + أدواره + صلاحياته
POST    change-password     [Authorize]       تغيير كلمة المرور

7.5 Reports (/api/reports) — 7 نقاط
────────────────────────────────────
Method  Path                          الوصف
──────  ────────────────────────────  ─────────────────────
GET     dashboard                     KPIs عامة
GET     supplier/{id}                 تقرير مورد
GET     profit                        تقرير الأرباح
GET     period                        تقرير فترة (dateFrom/dateTo)
GET     price-changes/{productId}     تغيّر أسعار صنف
GET     most-profitable               الأكثر ربحية
GET     best-selling                  الأكثر مبيعًا

7.6 Exports (/api/exports)
──────────────────────────
Method  Path                       الوصف
──────  ─────────────────────────  ─────────────────────────────
GET     invoice/{id}/pdf           تنزيل فاتورة PDF
GET     invoice/{id}/excel         تنزيل فاتورة Excel
GET     report/{type}/pdf          تصدير تقرير PDF (7 أنواع)
GET     report/{type}/excel        تصدير تقرير Excel (7 أنواع)

أنواع التقارير المدعومة في report/{type}:
dashboard, supplier, profit, period, price-changes, most-profitable, best-selling

معاملات query المدعومة:
period, supplierId, productId, category, topN, dateFrom, dateTo

7.7 Settings (/api/settings)
────────────────────────────
Method  Path       الوصف
──────  ─────────  ──────────────────────────────────
GET     /          كل الإعدادات
PUT     /          حفظ دفعة مفتاح/قيمة
POST    /logo      رفع شعار الشركة (multipart، حقل "file")
GET     /logo      استرجاع شعار الشركة الحالي
DELETE  /logo      حذف شعار الشركة الحالي

═══════════════════════════════════════════════════════════════
8. تدفق المصادقة (Authentication Flow)
═══════════════════════════════════════════════════════════════

8.1 تسجيل الدخول
────────────────
POST /api/auth/login
Body: { "username": "admin", "password": "admin123" }

Response 200:
{
  "accessToken": "eyJhbGc...",
  "refreshToken": "eyJhbGc...",
  "accessTokenExpiresAt": "2026-10-11T00:57:07Z",
  "user": {
    "id": 1,
    "username": "admin",
    "fullName": "مدير النظام",
    "email": null,
    "roles": ["Admin"],
    "permissions": ["suppliers.read", ... 23 items]
  }
}

8.2 استخدام التوكن
──────────────────
في Swagger:
1. انسخ accessToken (بدون علامات تنصيص).
2. اضغط Authorize → الصق التوكن (Swagger يضيف "Bearer " تلقائيًا).
3. Authorize → Close.

في PowerShell:
$headers = @{ Authorization = "Bearer $token" }

في Desktop (قادمًا):
HttpClient.DefaultRequestHeaders.Authorization =
    new AuthenticationHeaderValue("Bearer", token);

8.3 تجديد التوكن
────────────────
POST /api/auth/refresh
Body: { "refreshToken": "eyJhbGc..." }

التحقق:
- التوكن صالح (لم ينته).
- token_type = "refresh".
- المستخدم موجود و IsActive.

Response: نفس شكل login (Access جديد + Refresh جديد).

8.4 قواعد الأمان المطبَّقة
──────────────────────────
- رسالة موحّدة لفشل login (منع user enumeration).
- ClockSkew = TimeSpan.Zero (لا تسامح في انتهاء الصلاحية).
- Jwt:Key يجب ≥ 32 حرفًا (HMAC-SHA256).
- Work Factor = 12 لـ BCrypt (~250ms لكل hash).
- Refresh Token لا يُقبل كنقطة دخول.

═══════════════════════════════════════════════════════════════
9. مكان تخزين الشعار
═══════════════════════════════════════════════════════════════

%LOCALAPPDATA%\CostProfitApp\Uploads\company\logo.<ext>

يُدار عبر: Invoice.Infrastructure.Storage.LogoStorage

═══════════════════════════════════════════════════════════════
10. حالة Git
═══════════════════════════════════════════════════════════════

الفرع:               main
آخر Commit:          يُحدَّث بعد commit HANDOFF
عدد Commits:         ~35
ريموت:               لا يوجد (GitHub لم يُربط بعد)

أوامر Git المستخدمة
───────────────────
git add . ; git commit -m "message"
git log --oneline -N
git status
git show --stat HEAD

═══════════════════════════════════════════════════════════════
11. المشاكل المعروفة (Pending)
═══════════════════════════════════════════════════════════════

11.1 تم إنجازها ✅
──────────────────
✅ ترويسة الشركة في PDF/Excel
✅ شعار الشركة (رفع + عرض في ترويسة PDF/Excel)
✅ بيانات المورد الكاملة في PDF/Excel
✅ حدود جدول Excel
✅ إعادة ترتيب PDF/Excel بالكامل (PDF صفحتان، Excel ورقتان)
✅ المصادقة الكاملة (JWT + BCrypt)
✅ Seed تلقائي (admin + 3 أدوار + 23 صلاحية)

11.2 نواقص عامة
───────────────
⚠️ لا Audit Log (مؤجَّل).
⚠️ لا Refresh Token Blacklist (لا يمكن إبطال Refresh قبل انتهائه).
⚠️ لا اختبارات وحدة حقيقية (فقط smoke test للـ PDF).
⚠️ لا ربط WinForms بالـ API — Desktop ما زال فارغًا.
⚠️ لا تمييز بين بطاريات الرصاص والليثيوم (لم يُطلب).
⚠️ لا حقل "موقع الكتروني" للمورد.
⚠️ نقاط API القديمة (Suppliers/Products/Invoices/Reports/Exports/Settings)
   لا تحتوي [Authorize] بعد — قابلة للإضافة لاحقًا.
⚠️ لا Policies مبنية على الصلاحيات (resource.action) — قابلة للإضافة.
⚠️ Jwt:Key في appsettings.json مكشوف — يجب نقله إلى متغيرات البيئة في الإنتاج.
⚠️ كلمة مرور admin الافتراضية admin123 ضعيفة — يجب تغييرها فورًا.

═══════════════════════════════════════════════════════════════
12. أولويات العمل القادم
═══════════════════════════════════════════════════════════════

المسار المتفق عليه (أولوية عالية)
─────────────────────────────────
~~1. Auth + JWT + Users + Roles~~ ✅ مكتمل

2. ⏭️ حماية نقاط API القديمة بـ [Authorize] + Policies
   (خطوة موصى بها قبل Desktop — تجعل النظام آمنًا بالكامل)

3. WinForms Desktop (الخطوة الكبيرة):
   a. ApiClient (HttpClient مركزي يحمل التوكن + تجديد تلقائي)
   b. LoginForm
   c. MainForm
   d. 7 صفحات UserControl:
      - Dashboard
      - Suppliers
      - Products
      - Invoice
      - Archive
      - Reports
      - Settings

4. Import من Excel
5. Backup/Restore
6. Audit Log

أولوية منخفضة
─────────────
- نقل الاختبارات إلى مشروع tests/ (بعد إضافة اختبارات حقيقية).
- تحسين الأداء.
- Refresh Token Blacklist.

═══════════════════════════════════════════════════════════════
13. الأوامر السريعة المرجعية
═══════════════════════════════════════════════════════════════

تشغيل الخادم
────────────
dotnet run --project src/Invoice.Api

ثم:
- Swagger: http://localhost:5124/swagger
- API Base: http://localhost:5124/api

إيقاف الخادم
────────────
Ctrl + C   (في الطرفية)
Y          (إن طُلب تأكيد)

بناء الحل
─────────
dotnet build Invoice.sln

تنظيف كامل (عند الشك بمشاكل cache)
──────────────────────────────────
taskkill /F /IM dotnet.exe
dotnet clean Invoice.sln
dotnet build Invoice.sln

إنشاء Migration
───────────────
dotnet ef migrations add NAME \
  --project src/Invoice.Infrastructure \
  --startup-project src/Invoice.Infrastructure \
  --output-dir Persistence/Migrations

تطبيق Migration
───────────────
dotnet ef database update \
  --project src/Invoice.Infrastructure \
  --startup-project src/Invoice.Infrastructure

قائمة Migrations
────────────────
dotnet ef migrations list \
  --project src/Invoice.Infrastructure \
  --startup-project src/Invoice.Infrastructure

تشغيل الاختبارات
────────────────
dotnet test tests/Invoice.Tests/Invoice.Tests.csproj --filter "FullyQualifiedName~NAME"

تحقق من الجداول
───────────────
sqlcmd -S localhost\SQLEXPRESS -E -d InvoiceDb -C -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME"

فحص مستخدمي النظام
──────────────────
sqlcmd -S localhost\SQLEXPRESS -E -d InvoiceDb -C -Q "SELECT Id, Username, FullName, IsActive FROM Users"

فحص الأدوار والصلاحيات
──────────────────────
sqlcmd -S localhost\SQLEXPRESS -E -d InvoiceDb -C -Q "SELECT r.Name, COUNT(rp.PermissionId) AS PermCount FROM Roles r LEFT JOIN RolePermissions rp ON rp.RoleId=r.Id GROUP BY r.Name ORDER BY r.Name"

اختبار تسجيل دخول (PowerShell)
──────────────────────────────
$body = @{ username = "admin"; password = "admin123" } | ConvertTo-Json
$r = Invoke-WebRequest -Uri "http://localhost:5124/api/auth/login" -Method POST -ContentType "application/json" -Body $body
$r.StatusCode

حفظ في Git
──────────
git add . ; git commit -m "message"

═══════════════════════════════════════════════════════════════
14. بيانات الاختبار الحالية في قاعدة البيانات
═══════════════════════════════════════════════════════════════

النوع              القيم
─────────────────  ──────────────────────────────────────────
مورد               S-001 / مورد الاختبار الأول / طرابلس، ليبيا / 0910000000
بطارية             id=1, BAT-100, Batteries, 100AH
إطار               id=3, TIRE-9f4e..., Continental HDR2, Truck Tires
فاتورة             id=1, INV-2026-0001, SUP-2026-001، مؤرشفة
بيانات الفاتورة    10 وحدات BAT-100، شراء 50$، بيع 750 د.ل
النتائج            total_usd=500، net_cost=6300، total_sales=7500، profit=1200، margin=16%
إعدادات الشركة     اسم/عنوان/هاتف/إيميل/موقع (تم إدخالها)
شعار الشركة        %LOCALAPPDATA%\CostProfitApp\Uploads\company\logo.png
مستخدم النظام      admin / admin123 (دور Admin، 23 صلاحية)
أدوار النظام       Admin (23)، Accountant (16)، Viewer (6)
صلاحيات النظام     23 صلاحية بصيغة resource.action

═══════════════════════════════════════════════════════════════
15. قواعد التعامل مع المساعد (مهمة!)
═══════════════════════════════════════════════════════════════

✅ خطوة واحدة في كل رسالة.
✅ الكود المعدَّل يُعطى كاملًا — لا "عدّل السطر".
✅ انتظر الناتج قبل الانتقال للخطوة التالية.
✅ لا تفترض نجاح أمر — تحقق دائمًا.
✅ الصق الأخطاء كاملة عند الحدوث.
✅ مرجع واحد للبناء: VS Code + Terminal.
✅ Git commit بعد كل وحدة منطقية.
✅ اللغة: عربية للشرح، إنجليزية للكود.
✅ عند تعديل ملف، أعطِ أولًا أمر code path لفتحه.
✅ إن كان التعديل على جزء من ملف، اذكر القسم بدقة، ثم أعطِ الملف كاملًا.
✅ عند التحقق من قاعدة البيانات، استخدم sqlcmd بعد تشغيل الخادم.
✅ لا تفترض أن أمرًا نُفّذ — اطلب الناتج الفعلي (لا الصورة، لا الأمر).

═══════════════════════════════════════════════════════════════
16. نماذج رسائل جاهزة
═══════════════════════════════════════════════════════════════

رسالة استئناف جديدة
───────────────────
اقرأ HANDOFF.md ثم استأنف من [اسم المهمة].

طلب اختبار
──────────
اختبر [endpoint] عبر Swagger وأعطني Response body.

طلب إصلاح
─────────
أصلح [المشكلة] وأعطني الكود الكامل.

طلب migration
─────────────
أضف جدول [الاسم] مع الحقول [القائمة] وأنشئ migration.

فحص حالة سريعة
──────────────
git status ; git log --oneline -5

═══════════════════════════════════════════════════════════════
17. حالة الاستقرار الحالية
═══════════════════════════════════════════════════════════════

المكوّن                          الحالة                              مُختبَر
──────────────────────────────  ──────────────────────────────────  ───────
Domain                          ✅ مستقر                            ✅
Application                     ✅ مستقر                            ✅
Infrastructure (DB)             ✅ مستقر                            ✅
Infrastructure (PDF)            ✅ يعمل مع العربية + الشعار         ✅
Infrastructure (Excel)          ✅ يعمل مع الحدود + الشعار          ✅
API — Suppliers                 ✅ مستقر                            ✅
API — Products                  ✅ مستقر                            ✅
API — Invoices                  ✅ مستقر                            ✅
API — Reports                   ✅ 7 تقارير                         ✅
API — Exports (فواتير)          ✅ PDF + Excel                      ✅
API — Exports (تقارير)          ✅ PDF + Excel (7 أنواع)            ✅
API — Settings                  ✅ مستقر + شعار                     ✅
API — Auth (JWT)                ✅ login/refresh/me/change-pass     ✅
DataSeeder                      ✅ ينشئ admin + 3 أدوار + 23 صلاحية ✅
Desktop                         ⚪ لم يُعدّل                         ❌

التقييم العام:
الخادم جاهز وظيفيًا للإنتاج في بيئة LAN مع مصادقة JWT كاملة.
تطبيق سطح المكتب لم يبدأ بعد.

═══════════════════════════════════════════════════════════════
18. نقطة الاستئناف الحالية
═══════════════════════════════════════════════════════════════

الحالة: Auth + JWT مكتمل بنجاح ✅

ما تم إنجازه (10 خطوات من القسم السابق):
────────────────────────────────────────
1.  ✅ كيانات المصادقة في Domain (User, Role, Permission, UserRole, RolePermission)
2.  ✅ تهيئة العلاقات في AppDbContext (M2M + فهارس + RowVersion)
3.  ✅ Migration AddAuthTables + تطبيقه على القاعدة
4.  ✅ تثبيت Microsoft.AspNetCore.Authentication.JwtBearer 8.0.11
5.  ✅ إعدادات JWT في appsettings.json
6.  ✅ JwtService (توليد Access + Refresh + التحقق)
7.  ✅ DTOs + BcryptPasswordHasher (BCrypt 4.2.0)
8.  ✅ AuthController (login, refresh, me, change-password)
9.  ✅ تفعيل Middleware في Program.cs + Swagger Bearer
10. ✅ DataSeeder + اختبار كامل عبر Swagger و PowerShell

القرارات التقنية المطبَّقة في Auth:
──────────────────────────────────
- تخزين كلمات المرور:      BCrypt.Net-Next 4.2.0 (Work Factor = 12)
- Tokens:                   JWT HMAC-SHA256
                            Access: 60 دقيقة | Refresh: 7 أيام
- Issuer / Audience:        Invoice.Api / Invoice.Desktop
- الأدوار:                  Admin (23)، Accountant (16)، Viewer (6)
- الصلاحيات:                صيغة resource.action
- مستخدم افتراضي:           admin / admin123
- Middleware Order:         UseAuthentication → UseAuthorization
- Swagger Security:         Bearer scheme مُعرَّف + SecurityRequirement عام

المهمة القادمة (اختر):
──────────────────────
الخيار أ (مُوصى به):
حماية نقاط API القديمة (Suppliers, Products, Invoices, Reports,
Exports, Settings) بـ [Authorize] + Policies للصلاحيات.

الخيار ب:
البدء بتطبيق WinForms Desktop — ApiClient + LoginForm + MainForm
+ 7 صفحات UserControl.

نهاية HANDOFF.

عند بدء جلسة جديدة، اقرأ هذا الملف بالكامل وابدأ من القسم 18.