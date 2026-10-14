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
اللغة                       العربية فقط (لا دعم متعدد اللغات حاليًا)
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
│   │       ├── Supplier.cs, Product.cs
│   │       ├── Invoice.cs, InvoiceItem.cs, InvoicePayment.cs
│   │       ├── InvoiceDiscount.cs, InvoiceExpense.cs
│   │       ├── Settings.cs
│   │       └── User.cs, Role.cs, Permission.cs, UserRole.cs, RolePermission.cs
│   │
│   ├── Invoice.Application/          (class library)
│   │   ├── Exceptions/DomainException.cs
│   │   ├── Calculations/
│   │   │   ├── InvoiceCalculator.cs
│   │   │   ├── InvoiceItemInput.cs
│   │   │   └── InvoiceCalculationResult.cs
│   │   └── Dtos/
│   │       ├── InvoiceSaveRequest.cs, InvoiceItemRequest.cs
│   │       ├── InvoiceAmountRequest.cs, CompanyInfoDto.cs
│   │       └── ReportColumn.cs, ReportRow.cs, ReportSummaryItem.cs, ReportTableDto.cs
│   │
│   ├── Invoice.Infrastructure/       (class library)
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── DesignTimeDbContextFactory.cs
│   │   │   └── Migrations/
│   │   ├── Exporters/
│   │   │   ├── InvoicePdfExporter.cs     ← 31 مفتاحًا + صور ✅
│   │   │   ├── InvoiceExcelExporter.cs   ← نفس الميزات ✅
│   │   │   ├── ReportPdfExporter.cs
│   │   │   └── ReportExcelExporter.cs
│   │   ├── Storage/
│   │   │   ├── LogoStorage.cs
│   │   │   └── ProductImageStorage.cs    ← تخزين صور الأصناف ✅
│   │   └── Services/
│   │       └── ReportDataService.cs
│   │
│   ├── Invoice.Api/                  (webapi)
│   │   ├── Controllers/
│   │   │   ├── SuppliersController.cs, ProductsController.cs
│   │   │   ├── InvoicesController.cs, ReportsController.cs
│   │   │   ├── ExportsController.cs, SettingsController.cs
│   │   │   └── AuthController.cs
│   │   ├── Data/DataSeeder.cs
│   │   ├── Security/Permissions.cs
│   │   ├── Dtos/Auth/...
│   │   ├── Services/...
│   │   ├── Program.cs
│   │   └── appsettings*.json
│   │
│   └── Invoice.Desktop/              (winforms)
│       ├── Program.cs
│       ├── Services/
│       │   ├── ApiClient.cs
│       │   └── PreferencesService.cs
│       ├── Models/InvoiceDtos.cs
│       ├── Shared/
│       │   ├── ColumnsDialog.cs
│       │   ├── ExportOptionsDialog.cs
│       │   └── PdfPreviewForm.cs
│       ├── Forms/
│       │   ├── LoginForm.cs
│       │   └── MainForm.cs
│       └── UserControls/
│           ├── DashboardUserControl.cs
│           ├── SuppliersUserControl.cs
│           ├── ProductsUserControl.cs
│           ├── InvoiceUserControl.cs
│           ├── InvoiceDialogs.cs
│           ├── InvoiceAmountsPanel.cs
│           ├── InvoiceItemsPanel.cs
│           └── ArchiveUserControl.cs
│
└── tests/Invoice.Tests/

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
- company_name, company_address, company_phone,
  company_email, company_website, company_logo_path
- (مستقبلًا) currency_symbol, backup_schedule, backup_folder,
  backup_retention, dark_mode, font_size

4.4 حقول Product كاملة
──────────────────────
Id, Code, Name, Category, Pattern,
Ah, L, W, H, Oh (decimal?),
Layout (int?), LayoutImageFilename (string? — مسار كامل على الخادم),
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
- جميع التصنيفات: الكود إلزامي، والاسم = الكود تلقائيًا.
- بطارية: لا نقشة. حقول إضافية: Ah, L, W, H, Oh, Layout, صورة, Pallet.
- إطار: النقشة إلزامية. باقي الحقول الإضافية = null.
- صور الأصناف: تُخزَّن على الخادم
  (%LOCALAPPDATA%\CostProfitApp\Uploads\products\).
  - رفع: POST /api/products/{id}/image (multipart)
  - جلب: GET /api/products/{id}/image
  - حذف: DELETE /api/products/{id}/image

5.2 الفواتير (Invoices)
───────────────────────
- دورة الحياة: Draft → Archived (لا رجوع).
- الأرشفة: تتطلب حفظ أي تعديلات أولًا.
- الترقيم: INV-{year}-{NNNN} تلقائي متسلسل.
- الحسابات: InvoiceCalculator مع MidpointRounding.AwayFromZero.

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

5.4 المصادقة والصلاحيات
───────────────────────
- BCrypt Work Factor = 12.
- JWT HMAC-SHA256: Access 60 دقيقة، Refresh 7 أيام.
- Policies مبنية على claim "permission".
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

توزيع الأدوار:
- Admin:      23/23
- Accountant: 16/23
- Viewer:     6/23

═══════════════════════════════════════════════════════════════
7. API المُنفَّذ
═══════════════════════════════════════════════════════════════

7.1 Suppliers (/api/suppliers) — CRUD كامل
7.2 Products (/api/products) — CRUD + صور (رفع/جلب/حذف)
7.3 Invoices (/api/invoices) — 9 نقاط
7.4 Auth (/api/auth) — login, refresh, me, change-password
7.5 Reports (/api/reports) — 7 نقاط
7.6 Exports (/api/exports) — invoice/pdf, invoice/excel (يدعم ?hidden=)
7.7 Settings (/api/settings) — GET, PUT, POST/GET/DELETE logo

مفاتيح الإخفاء (31 مفتاحًا):
  logo, company_info, supplier_info, invoice_info, notes,
  payments, discounts, expenses, summary,
  items.line_no, items.code, items.ah, items.l, items.w, items.h,
  items.oh, items.layout, items.image, items.pallet,
  items.category, items.pattern, items.quantity,
  items.purchase_usd, items.total_purchase_usd,
  items.purchase_dzd, items.total_dzd,
  items.sale_dzd, items.total_sale,
  items.unit_profit, items.total_profit

═══════════════════════════════════════════════════════════════
8. حالة Desktop
═══════════════════════════════════════════════════════════════

المُنجَز ✅:
- ApiClient (تسجيل + auto-refresh + رفع/جلب/حذف صور)
- LoginForm, MainForm
- PreferencesService (أعمدة مخفية + عروض أعمدة + حجم خط + وضع داكن)
- Shared/ColumnsDialog, ExportOptionsDialog, PdfPreviewForm
- DashboardUserControl (KPIs + مسودات + فتح مزدوج)
- SuppliersUserControl (CRUD + عرض أعمدة)
- ProductsUserControl (CRUD + صور من الخادم)
- InvoiceUserControl (3 تبويبات + 21 عمودًا + معاينة PDF)
- InvoiceDialogs, InvoiceAmountsPanel, InvoiceItemsPanel
- ArchiveUserControl (قائمة + تصدير + فتح مزدوج)

═══════════════════════════════════════════════════════════════
9. مسارات التخزين
═══════════════════════════════════════════════════════════════

شعار الشركة (على الخادم):
  %LOCALAPPDATA%\CostProfitApp\Uploads\company\logo.<ext>

صور الأصناف (على الخادم):
  %LOCALAPPDATA%\CostProfitApp\Uploads\products\product_<guid>.<ext>

مجلد WebView2 (Desktop):
  %LOCALAPPDATA%\CostProfitApp\Desktop\WebView2\

تفضيلات المستخدم (Desktop):
  %LOCALAPPDATA%\CostProfitApp\Desktop\preferences.json

═══════════════════════════════════════════════════════════════
10. حالة Git
═══════════════════════════════════════════════════════════════

الفرع:               main
الريموت:             origin → https://github.com/sadekattiaa-pixel/invoice-.git
هوية Git:            ADNAN / adnan@alintiqaa.local

═══════════════════════════════════════════════════════════════
11. المشاكل المعروفة (Pending)
═══════════════════════════════════════════════════════════════

11.1 تم إنجازها ✅
──────────────────
✅ المصادقة (JWT + BCrypt)
✅ Policies على كل نقاط API
✅ كل الشاشات الرئيسية (Dashboard/Suppliers/Products/Invoice/Archive)
✅ عرض الأعمدة بالسحب مع حفظ تلقائي
✅ PDF/Excel يدعمان 31 مفتاح إخفاء
✅ نافذة خيارات التصدير (9 أجزاء + 21 عمودًا)
✅ معاينة PDF عبر WebView2
✅ تخزين صور الأصناف على الخادم + عرضها في التصدير
✅ Dashboard: مسودات + فتح مزدوج
✅ Archive: فتح مزدوج
✅ Invoice: 3 تبويبات + 21 عمودًا + Enter navigation

11.2 نواقص عامة
───────────────
⚠️ لا Audit Log.
⚠️ لا Refresh Token Blacklist.
⚠️ لا اختبارات وحدة حقيقية.
⚠️ شاشة Reports/Settings/Users في Desktop لم تُبنَ بعد.
⚠️ لا Controllers لإدارة Users/Roles.
⚠️ Jwt:Key في appsettings.json مكشوف.
⚠️ كلمات المرور الافتراضية ضعيفة.
⚠️ رمز العملة القابل للتخصيص — لم يُنفَّذ.
⚠️ تقارير PDF/Excel لا تدعم hidden بعد.
⚠️ لم يُنفَّذ: الوضع الداكن، حجم الخط في الواجهة.
⚠️ لا Backup/Restore.

═══════════════════════════════════════════════════════════════
12. خطة شاشة الإعدادات (SettingsUserControl)
═══════════════════════════════════════════════════════════════

القرارات المعتمدة:
- اللغة: العربية فقط (لا دعم متعدد اللغات).
- تبويب الصور (رفع + ربط): ملغى.
- الاستعادة وإعادة التعيين: تُبنى كاملة في الواجهة (بتأكيدات مضاعفة).
- رمز العملة: مؤجل.
- إدارة المستخدمين: شاشة مستقلة (بعد الإعدادات) — انظر القسم 13.

التبويبات (6 تبويبات):

| # | التبويب | المحتوى | الحالة |
|---|---------|---------|--------|
| 1 | عام | حجم الخط + الوضع الداكن/الفاتح | ⏳ |
| 2 | بيانات الشركة | اسم/عنوان/هاتف/بريد/موقع + شعار | ⏳ |
| 3 | الامان | تغيير اسم المستخدم + كلمة المرور | ⏳ |
| 4 | النسخ الاحتياطي | يدوي + تلقائي + استعادة + إعادة تعيين | ⏳ |
| 5 | حول البرنامج | معلومات ثابتة + إصدار | ⏳ |

الموجات المقترحة (~14 مرحلة):

الموجة 1:
- S1: شاشة Settings (TabControl فارغة) + ربط في MainForm
- S2: تبويب "حول البرنامج"
- S3: تبويب "بيانات الشركة" (نصوص)
- S4: تبويب "بيانات الشركة" (شعار: رفع/عرض/حذف)

الموجة 2:
- S5: تبويب "الامان" — تغيير كلمة المرور
- S6: تبويب "الامان" — تغيير اسم المستخدم (يحتاج API جديد)

الموجة 3:
- S7: تبويب "عام" — حجم الخط
- S8: تبويب "عام" — الوضع الداكن/الفاتح

الموجة 4:
- S9: تبويب "النسخ الاحتياطي" — نسخ يدوي
- S10: تبويب "النسخ الاحتياطي" — نسخ تلقائي (BackgroundService)
- S11: تبويب "النسخ الاحتياطي" — عرض النسخ المتاحة
- S12: تبويب "النسخ الاحتياطي" — استعادة (⚠️ خطر)
- S13: تبويب "النسخ الاحتياطي" — إعادة تعيين (⚠️ خطر أقصى)

═══════════════════════════════════════════════════════════════
13. خطة شاشة إدارة المستخدمين (UsersUserControl) — مؤجلة
═══════════════════════════════════════════════════════════════

القرار: تُبنى بعد إكمال شاشة الإعدادات.

الوصف:
- صفحة مستقلة في القائمة الجانبية (فقط لمن يملك users.read).
- يظهر زرها فقط لمستخدم Admin.

التبويبات:
1. المستخدمون: جدول + إضافة/تعديل/حذف/تعطيل/إعادة تعيين كلمة المرور
2. الأدوار والصلاحيات: جدول الأدوار + مصفوفة صلاحيات (23 × N)

المكونات المطلوبة (Backend):
- UsersController (CRUD + إعادة تعيين كلمة المرور)
- RolesController (CRUD)
- DTOs جديدة

المكونات المطلوبة (Desktop):
- UsersUserControl (تبويبان)
- ربطها في MainForm + إخفاؤها لمن لا يملك users.read

التعقيد: 🔴 معقد جدًا — 12-16 مرحلة كاملة.
المخاطر: عالية (خطأ في تعديل الأدوار → فقدان السيطرة على النظام).
الترتيب: بعد الإعدادات. قبل التقارير أو بعدها (يُحدَّد لاحقًا).

═══════════════════════════════════════════════════════════════
14. أولويات العمل القادم
═══════════════════════════════════════════════════════════════

1. ⏭️ شاشة Settings (الموجة 1 → 4)
2. شاشة إدارة المستخدمين (UsersUserControl)
3. شاشة Reports
4. رمز العملة (مؤجل)
5. تقارير PDF/Excel تدعم hidden
6. Import من Excel
7. Audit Log

═══════════════════════════════════════════════════════════════
15. الأوامر السريعة المرجعية
═══════════════════════════════════════════════════════════════

تشغيل الخادم
────────────
dotnet run --project src/Invoice.Api

إيقاف الخادم
────────────
Get-Process -Name "Invoice.Api" -ErrorAction SilentlyContinue | Stop-Process -Force
taskkill /F /IM Invoice.Api.exe
taskkill /F /IM Invoice.Desktop.exe
taskkill /F /IM dotnet.exe

تشغيل Desktop
─────────────
dotnet run --project src/Invoice.Desktop

بناء الحل
─────────
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

حفظ في Git
──────────
git add . ; git commit -m "message"

═══════════════════════════════════════════════════════════════
16. بيانات الاختبار الحالية
═══════════════════════════════════════════════════════════════

مورد:             S-001 / مورد الاختبار الأول / طرابلس، ليبيا
بطارية:           id=1, BAT-100, Batteries, 100AH
إطار:             id=3, TIRE-..., Continental HDR2, Truck Tires
فاتورة:           id=1, INV-2026-0001, SUP-2026-001، مؤرشفة
مستخدمو النظام:   admin / accountant / viewer
أدوار:            Admin (23)، Accountant (16)، Viewer (6)

═══════════════════════════════════════════════════════════════
17. قواعد التعامل مع المساعد (مهمة!)
═══════════════════════════════════════════════════════════════

✅ خطوة واحدة في كل رسالة.
✅ الكود المعدَّل يُعطى كاملًا — لا "عدّل السطر".
✅ انتظر الناتج قبل الانتقال للخطوة التالية.
✅ لا تفترض نجاح أمر — تحقق دائمًا.
✅ الصق الأخطاء كاملة عند الحدوث.
✅ اللغة: عربية للشرح، إنجليزية للكود.
✅ عند تعديل ملف، أعطِ أولًا أمر code path لفتحه.
✅ أمر واحد فقط في كل رسالة، وملف واحد فقط في كل رسالة.
✅ الحفظ في Git بعد كل مرحلة كاملة.

═══════════════════════════════════════════════════════════════
18. حالة الاستقرار الحالية
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
Desktop — كل الشاشات            ✅ تعمل                             ✅
Desktop — Settings              ⚪ لم يبدأ                          ❌
Desktop — Users                 ⚪ لم يبدأ                          ❌
Desktop — Reports               ⚪ لم يبدأ                          ❌

═══════════════════════════════════════════════════════════════
19. نقطة الاستئناف الحالية
═══════════════════════════════════════════════════════════════

الحالة: اكتملت تعديلات شاشة الفاتورة والتصدير.
جاهزون لبدء **شاشة الإعدادات**.

الخطوة القادمة:
المرحلة S1 من شاشة الإعدادات:
- إنشاء `src/Invoice.Desktop/UserControls/SettingsUserControl.cs`
- TabControl بـ 5 تبويبات فارغة:
  (عام / بيانات الشركة / الامان / النسخ الاحتياطي / حول البرنامج)
- ربطها في MainForm عند النقر على "الإعدادات"

نهاية HANDOFF.

عند بدء جلسة جديدة، اقرأ هذا الملف بالكامل وابدأ من القسم 19.