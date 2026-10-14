HANDOFF — مشروع Invoice-CSharp
آخر تحديث: 14 أكتوبر 2026
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
التخويل (Authorization)     Policies مبنية على الصلاحيات ✅ مُنفَّذ
النقل                       HTTP + JWT Bearer
حزمة JWT                    Microsoft.AspNetCore.Authentication.JwtBearer 8.0.11
حزمة BCrypt                 BCrypt.Net-Next 4.2.0
حزمة PDF preview            Microsoft.Web.WebView2 1.0.4258.31 ✅
الفرع الرئيسي               main
الريموت                     origin → https://github.com/sadekattiaa-pixel/invoice-.git

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
│   │       ├── User.cs
│   │       ├── Role.cs
│   │       ├── Permission.cs
│   │       ├── UserRole.cs
│   │       └── RolePermission.cs
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
│   │   ├── Exporters/
│   │   │   ├── InvoicePdfExporter.cs     ← 21 عمودًا + صور + خيارات hidden ✅
│   │   │   ├── InvoiceExcelExporter.cs   ← نفس الميزات ✅
│   │   │   ├── ReportPdfExporter.cs
│   │   │   └── ReportExcelExporter.cs
│   │   ├── Storage/
│   │   │   ├── LogoStorage.cs
│   │   │   └── ProductImageStorage.cs    ← تخزين صور الأصناف على الخادم ✅
│   │   └── Services/
│   │       └── ReportDataService.cs
│   │
│   ├── Invoice.Api/                  (webapi)
│   │   ├── Controllers/
│   │   │   ├── SuppliersController.cs
│   │   │   ├── ProductsController.cs     ← CRUD + رفع/جلب/حذف صورة ✅
│   │   │   ├── InvoicesController.cs
│   │   │   ├── ReportsController.cs
│   │   │   ├── ExportsController.cs      ← يدعم ?hidden=a,b,c ✅
│   │   │   ├── SettingsController.cs
│   │   │   └── AuthController.cs
│   │   ├── Data/DataSeeder.cs
│   │   ├── Security/Permissions.cs
│   │   ├── Dtos/Auth/...
│   │   ├── Services/...
│   │   ├── Program.cs
│   │   ├── appsettings.json
│   │   └── appsettings.Development.json
│   │
│   └── Invoice.Desktop/              (winforms)
│       ├── Program.cs
│       ├── Services/
│       │   ├── ApiClient.cs                  ← + رفع/جلب/حذف صور ✅
│       │   └── PreferencesService.cs         ← + ColumnWidths ✅
│       ├── Models/
│       │   └── InvoiceDtos.cs
│       ├── Shared/
│       │   ├── ColumnsDialog.cs
│       │   ├── ExportOptionsDialog.cs        ← 9 أجزاء + 21 عمودًا ✅
│       │   └── PdfPreviewForm.cs             ← WebView2 ✅
│       ├── Forms/
│       │   ├── LoginForm.cs
│       │   └── MainForm.cs
│       └── UserControls/
│           ├── DashboardUserControl.cs       ← + مسودات + فتح مزدوج ✅
│           ├── SuppliersUserControl.cs       ← + عرض أعمدة بالسحب ✅
│           ├── ProductsUserControl.cs        ← + صور من الخادم + عرض أعمدة ✅
│           ├── InvoiceUserControl.cs         ← 3 تبويبات + 21 عمودًا ✅
│           ├── InvoiceDialogs.cs
│           ├── InvoiceAmountsPanel.cs        ← تنسيق جديد (يمين المصاريف) ✅
│           ├── InvoiceItemsPanel.cs          ← 21 عمودًا + صور + ScrollBars ✅
│           └── ArchiveUserControl.cs         ← فتح مزدوج + عرض أعمدة ✅
│
└── tests/
    └── Invoice.Tests/

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
Server=localhost\SQLEXPRESS;Database=InvoiceDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=120

4.2 الجداول (14 جدولًا)
───────────────────────
Settings, Suppliers, Products, Invoices, InvoiceItems,
InvoicePayments, InvoiceDiscounts, InvoiceExpenses,
Users, Roles, Permissions, UserRoles, RolePermissions,
__EFMigrationsHistory

4.3 مفاتيح Settings
────────────────────
company_name, company_address, company_phone, company_email,
company_website, company_logo_path

4.4 حقول Product كاملة
──────────────────────
Id, Code, Name, Category, Pattern,
Ah, L, W, H, Oh (decimal?),
Layout (int?), LayoutImageFilename (string? — مسار كامل على الخادم الآن),
Pallet (int?), SupplierId, CreatedAt, UpdatedAt, RowVersion

4.5 حقول RowVersion
───────────────────
Suppliers, Products, Invoices, Users, Roles

═══════════════════════════════════════════════════════════════
5. قواعد العمل المُنفَّذة
═══════════════════════════════════════════════════════════════

5.1 الأصناف (Products)
──────────────────────
- التصنيفات: Batteries, Car Tires, Truck Tires.
- **جميع التصنيفات**: الكود إلزامي، والاسم = الكود تلقائيًا.
- بطارية: لا نقشة. حقول إضافية: Ah, L, W, H, Oh, Layout, صورة, Pallet.
- إطار: النقشة إلزامية. باقي الحقول الإضافية = null.
- **صور الأصناف**: تُخزَّن على الخادم (%LOCALAPPDATA%\CostProfitApp\Uploads\products\).
  - يُخزَّن المسار الكامل في DB.
  - رفع: POST /api/products/{id}/image (multipart).
  - جلب: GET /api/products/{id}/image.
  - حذف: DELETE /api/products/{id}/image.

5.2 الفواتير (Invoices)
───────────────────────
- دورة الحياة: Draft → Archived (لا رجوع).
- الأرشفة: تتطلب حفظ أي تعديلات أولًا؛ الفاتورة المؤرشفة لا تُعدَّل.
- الترقيم: INV-{year}-{NNNN} تلقائي متسلسل.
- الحسابات: تُطبَّق في InvoiceCalculator مع MidpointRounding.AwayFromZero.

5.3 معادلات الحساب
──────────────────
net_payments  = total_payments − total_discounts
net_cost      = net_payments + total_expenses
exchange_rate = net_cost / total_usd
purchase_dzd  = purchase_usd × exchange_rate
unit_profit   = sale_dzd − purchase_dzd
total_profit  = quantity × unit_profit
profit_margin = (total_profit / total_sales) × 100

الدقة: Money: 2 منازل، Rate: 6 منازل، Percent: 4 منازل.

5.4 المصادقة والصلاحيات — ✅ مكتمل
───────────────────────────────────
- BCrypt Work Factor = 12.
- JWT HMAC-SHA256: Access 60 دقيقة، Refresh 7 أيام.
- Policies مبنية على claim "permission" بصيغة resource.action.
- المستخدمون:
  - admin / admin123 → Admin (23 صلاحية)
  - accountant / acct123 → Accountant (16 صلاحية)
  - viewer / view123 → Viewer (6 صلاحيات)

═══════════════════════════════════════════════════════════════
6. الصلاحيات المعرَّفة (23 صلاحية)
═══════════════════════════════════════════════════════════════

suppliers: read/create/update/delete (4)
products:  read/create/update/delete (4)
invoices:  read/create/update/delete/archive (5)
reports:   read (1)
exports:   read (1)
settings:  read/update (2)
users:     read/create/update/delete (4)
roles:     read/update (2)
───────────────────────────────────
الإجمالي: 23

توزيع الأدوار:
- Admin:      23/23
- Accountant: 16/23
- Viewer:     6/23

═══════════════════════════════════════════════════════════════
7. API المُنفَّذ (كل النقاط محمية بـ [Authorize] + Policies)
═══════════════════════════════════════════════════════════════

7.1 Suppliers (/api/suppliers)
──────────────────────────────
GET /, GET /{id}, POST /, PUT /{id}, DELETE /{id}

7.2 Products (/api/products)
────────────────────────────
GET /, GET /{id}, POST /, PUT /{id}, DELETE /{id}
POST /{id}/image, GET /{id}/image, DELETE /{id}/image     ← جديد

7.3 Invoices (/api/invoices)
────────────────────────────
GET /, GET /{id}, GET next-number,
POST calculate, POST /, PUT /{id},
POST /{id}/archive, POST /{id}/duplicate, DELETE /{id}

7.4 Auth (/api/auth)
────────────────────
POST login, POST refresh, GET me, POST change-password

7.5 Reports (/api/reports) — 7 نقاط
────────────────────────────────────
dashboard, supplier/{id}, profit, period,
price-changes/{productId}, most-profitable, best-selling

7.6 Exports (/api/exports) — يدعم ?hidden=a,b,c
───────────────────────────────────────────────
GET invoice/{id}/pdf?hidden=...
GET invoice/{id}/excel?hidden=...
GET report/{type}/pdf
GET report/{type}/excel

مفاتيح الإخفاء المتاحة (InvoicePdfExporter.Keys) — 31 مفتاحًا:
  logo, company_info, supplier_info, invoice_info, notes,
  payments, discounts, expenses, summary,
  items.line_no, items.code, items.ah, items.l, items.w, items.h,
  items.oh, items.layout, items.image, items.pallet,
  items.category, items.pattern, items.quantity,
  items.purchase_usd, items.total_purchase_usd,
  items.purchase_dzd, items.total_dzd,
  items.sale_dzd, items.total_sale,
  items.unit_profit, items.total_profit

7.7 Settings (/api/settings)
────────────────────────────
GET /, PUT /, POST /logo, GET /logo, DELETE /logo

═══════════════════════════════════════════════════════════════
8. حالة Desktop
═══════════════════════════════════════════════════════════════

المُنجَز ✅:
- ApiClient (تسجيل + auto-refresh + تنزيل + رفع/جلب/حذف صور)
- LoginForm
- MainForm (قائمة جانبية + 7 أزرار)
- PreferencesService (أعمدة مخفية + عروض أعمدة + حجم خط + وضع داكن)
- Shared/ColumnsDialog (نافذة إخفاء الأعمدة)
- Shared/ExportOptionsDialog (9 أجزاء + 21 عمودًا + إظهار/إخفاء الكل)
- Shared/PdfPreviewForm (معاينة PDF عبر WebView2)
- DashboardUserControl (KPIs + جدول مسودات + نقر مزدوج)
- SuppliersUserControl (CRUD + عرض أعمدة بالسحب)
- ProductsUserControl (CRUD + صور من الخادم + عرض أعمدة)
- InvoiceUserControl (3 تبويبات + 21 عمودًا + معاينة PDF)
- InvoiceDialogs (اختيار صنف + إدخال مبلغ)
- InvoiceAmountsPanel (مدفوعات/خصومات/مصاريف + ScrollBars)
- InvoiceItemsPanel (21 عمودًا + صور + ScrollBars + Enter)
- ArchiveUserControl (قائمة + فلترة + تصدير + نقر مزدوج + عرض أعمدة)

ملاحظات إضافية:
- PdfPreviewForm يستخدم WebView2 → يجب تثبيت WebView2 Runtime (مُضمَّن مع Windows 10/11).
- InvoiceItemsPanel.GetHiddenServerKeys() تُرجِع مفاتيح الخادم من أسماء الأعمدة.

═══════════════════════════════════════════════════════════════
9. مسارات التخزين
═══════════════════════════════════════════════════════════════

شعار الشركة (على الخادم):
  %LOCALAPPDATA%\CostProfitApp\Uploads\company\logo.<ext>
  يُدار عبر: Invoice.Infrastructure.Storage.LogoStorage

صور الأصناف (على الخادم — جديد):
  %LOCALAPPDATA%\CostProfitApp\Uploads\products\product_<guid>.<ext>
  يُدار عبر: Invoice.Infrastructure.Storage.ProductImageStorage

مجلد WebView2 (Desktop):
  %LOCALAPPDATA%\CostProfitApp\Desktop\WebView2\

تفضيلات المستخدم (Desktop):
  %LOCALAPPDATA%\CostProfitApp\Desktop\preferences.json

═══════════════════════════════════════════════════════════════
10. حالة Git
═══════════════════════════════════════════════════════════════

الفرع:               main
الريموت:             origin → https://github.com/sadekattiaa-pixel/invoice-.git
آخر Commit محلي:     [يُحدَّث بعد كل حفظ]
هوية Git:            ADNAN / adnan@alintiqaa.local

═══════════════════════════════════════════════════════════════
11. المشاكل المعروفة (Pending)
═══════════════════════════════════════════════════════════════

11.1 تم إنجازها ✅
──────────────────
✅ المصادقة (JWT + BCrypt)
✅ Policies على كل نقاط API
✅ Desktop: كل الشاشات الرئيسية (Dashboard/Suppliers/Products/Invoice/Archive)
✅ Desktop: عرض الأعمدة بالسحب مع حفظ تلقائي
✅ Server: PDF/Excel يدعمان 31 مفتاح إخفاء
✅ Desktop: نافذة خيارات التصدير (9 أجزاء + 21 عمودًا)
✅ Desktop: معاينة PDF عبر WebView2
✅ Server: تخزين صور الأصناف على الخادم
✅ Export: صور الأصناف في PDF/Excel
✅ Dashboard: جدول المسودات + فتح مزدوج
✅ Archive: فتح مزدوج (تعديل/عرض)
✅ Invoice: 3 تبويبات + 21 عمودًا + Enter navigation

11.2 نواقص عامة
───────────────
⚠️ لا Audit Log.
⚠️ لا Refresh Token Blacklist.
⚠️ لا اختبارات وحدة حقيقية.
⚠️ شاشة Reports/Settings في Desktop لم تُبنَ بعد.
⚠️ الوضع الداكن + حجم الخط (التفضيلات موجودة لكن الميزة غير مطبقة).
⚠️ لا Controllers لإدارة Users/Roles.
⚠️ Jwt:Key في appsettings.json مكشوف.
⚠️ كلمات المرور الافتراضية ضعيفة.
⚠️ رمز العملة القابل للتخصيص (د.ل/$/€) — لم يُنفَّذ.
⚠️ ترحيل الصور القديمة (المخزَّنة بأسماء ملفات محلية) — تحتاج خطوة منفصلة.
⚠️ تقارير PDF/Excel لا تدعم hidden بعد (فقط الفواتير).

═══════════════════════════════════════════════════════════════
12. أولويات العمل القادم
═══════════════════════════════════════════════════════════════

1. ⏭️ شاشة Settings (مفتاح/قيمة + رفع شعار + رمز عملة)
2. ⏭️ شاشة Reports (7 تقارير + تصدير PDF/Excel)
3. ميزة حجم الخط
4. ميزة الوضع الداكن
5. ترحيل صور الأصناف القديمة
6. تقارير PDF/Excel تدعم hidden keys
7. Import من Excel
8. Backup/Restore
9. Audit Log

═══════════════════════════════════════════════════════════════
13. الأوامر السريعة المرجعية
═══════════════════════════════════════════════════════════════

تشغيل الخادم
────────────
dotnet run --project src/Invoice.Api
- Swagger: http://localhost:5124/swagger

إيقاف الخادم (إن كان يعمل)
──────────────────────────
Get-Process -Name "Invoice.Api" -ErrorAction SilentlyContinue | Stop-Process -Force
أو:
taskkill /F /IM Invoice.Api.exe
taskkill /F /IM Invoice.Desktop.exe
taskkill /F /IM dotnet.exe

تشغيل Desktop
─────────────
dotnet run --project src/Invoice.Desktop

بناء الحل
─────────
dotnet build Invoice.sln

تنظيف
─────
dotnet clean Invoice.sln

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

فحص المستخدمين
──────────────
sqlcmd -S localhost\SQLEXPRESS -E -d InvoiceDb -C -Q "SELECT u.Id, u.Username, r.Name AS RoleName FROM Users u LEFT JOIN UserRoles ur ON ur.UserId=u.Id LEFT JOIN Roles r ON r.Id=ur.RoleId ORDER BY u.Id"

فحص مسار صور الأصناف
────────────────────
sqlcmd -S localhost\SQLEXPRESS -E -d InvoiceDb -C -Q "SELECT Id, Code, LayoutImageFilename FROM Products WHERE LayoutImageFilename IS NOT NULL"

حفظ في Git
──────────
git add . ; git commit -m "message"

═══════════════════════════════════════════════════════════════
14. بيانات الاختبار الحالية
═══════════════════════════════════════════════════════════════

مورد:             S-001 / مورد الاختبار الأول / طرابلس، ليبيا
بطارية:           id=1, BAT-100, Batteries, 100AH (+ حقول إضافية)
إطار:             id=3, TIRE-..., Continental HDR2, Truck Tires
فاتورة:           id=1, INV-2026-0001, SUP-2026-001، مؤرشفة
مستخدمو النظام:   admin / accountant / viewer
أدوار:            Admin (23)، Accountant (16)، Viewer (6)

═══════════════════════════════════════════════════════════════
15. قواعد التعامل مع المساعد (مهمة!)
═══════════════════════════════════════════════════════════════

✅ خطوة واحدة في كل رسالة.
✅ الكود المعدَّل يُعطى كاملًا — لا "عدّل السطر".
✅ انتظر الناتج قبل الانتقال للخطوة التالية.
✅ لا تفترض نجاح أمر — تحقق دائمًا.
✅ الصق الأخطاء كاملة عند الحدوث.
✅ اللغة: عربية للشرح، إنجليزية للكود.
✅ عند تعديل ملف، أعطِ أولًا أمر code path لفتحه.
✅ عند التحقق من قاعدة البيانات، استخدم sqlcmd.
✅ أمر واحد فقط في كل رسالة، وملف واحد فقط في كل رسالة.
✅ الحفظ في Git يكون بعد نهاية كل **مرحلة كاملة** — وليس بعد كل كود أو ملف.

═══════════════════════════════════════════════════════════════
16. نماذج رسائل جاهزة
═══════════════════════════════════════════════════════════════

رسالة استئناف جديدة
───────────────────
اقرأ HANDOFF.md ثم استأنف من [اسم المهمة].

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
Infrastructure (PDF/Excel)      ✅ 31 مفتاحًا + صور                 ✅
API — كل الـ Controllers        ✅ محمي بـ [Authorize]              ✅
API — Products images           ✅ رفع/جلب/حذف                     ✅
Authorization Policies          ✅ 403 على النقصان                  ✅
DataSeeder                      ✅ 3 مستخدمين + 3 أدوار + 23 صلاحية ✅
Desktop — ApiClient             ✅ مستقر                            ✅
Desktop — LoginForm             ✅ يعمل                             ✅
Desktop — MainForm              ✅ يعمل                             ✅
Desktop — Dashboard             ✅ KPIs + مسودات + فتح مزدوج       ✅
Desktop — Suppliers             ✅ CRUD + عرض أعمدة                 ✅
Desktop — Products              ✅ CRUD + صور + عرض أعمدة           ✅
Desktop — Invoice               ✅ 3 تبويبات + 21 عمودًا + معاينة   ✅
Desktop — Archive               ✅ قائمة + تصدير + فتح مزدوج        ✅
Desktop — Reports               ⚪ لم يبدأ                          ❌
Desktop — Settings              ⚪ لم يبدأ                          ❌

═══════════════════════════════════════════════════════════════
18. نقطة الاستئناف الحالية
═══════════════════════════════════════════════════════════════

الحالة: اكتملت **16 تعديلًا** مُخطَّطًا + تعديلات إضافية على شاشة الفاتورة والتصدير.
جميع الميزات الرئيسية تعمل ومُختبَرة.

آخر ما تم إنجازه:
- المرحلة 1-15: تعديلات شاملة على جميع الشاشات والتصدير.
- تحسينات تصدير PDF: خطوط أكبر + إزالة عناوين + توسيط رأسي/أفقي.
- صور الأصناف: نُقلت للخادم، تظهر في PDF و Excel.

الخطوة القادمة المقترحة:
بناء شاشة **Settings** (مفتاح/قيمة + رفع شعار + رمز العملة القابل للتخصيص).

نهاية HANDOFF.

عند بدء جلسة جديدة، اقرأ هذا الملف بالكامل وابدأ من القسم 18.