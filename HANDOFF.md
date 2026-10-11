HANDOFF — مشروع Invoice-CSharp
آخر تحديث: 11 أكتوبر 2026
الإصدار الحالي: نسخة تطويرية (لم تُرقّم بعد)
مسار المشروع: C:\Users\ADNAN\projects\cost_profit\Invoice-CSharp

1. نظرة عامة
الهدف
إعادة كتابة تطبيق CostProfitApp (Python/PySide6) كنظام Client-Server بلغة C#/.NET 8:

الخادم: ASP.NET Core Web API + SQL Server Express.

العميل: Windows Forms يتصل بالخادم عبر HTTP.

الهدف: العمل على شبكة LAN (حتى 10 أجهزة) بدون إنترنت.

المستخدم
مبتدئ في البرمجة.

يعمل من VS Code (ليس Visual Studio).

يحتاج خطوة واحدة في كل رسالة.

يفضّل الكود الكامل عند التعديل (لا يقول "عدّل السطر X").

2. القرارات التقنية المعتمدة
البند	القرار
.NET SDK	8.0.424 LTS
Git	2.56.0
قاعدة البيانات	SQL Server Express (Instance: MSSQL$SQLEXPRESS)
اسم القاعدة	InvoiceDb
EF Core	8.0.31
PDF	QuestPDF 2026.9.1 (Community) — يدعم العربية + الشعار
Excel	ClosedXML 0.105.1
dotnet-ef	8.0.31 (أداة عامة)
الواجهة	WinForms (code-first — لا مصمم مرئي)
المصادقة	متعدد المستخدمين بأدوار — ✅ مُنفَّذ (JWT + BCrypt)
النقل	HTTP + API Key + JWT
حزم المصادقة	Microsoft.AspNetCore.Authentication.JwtBearer 8.0.11 + BCrypt.Net-Next 4.2.0
الفرع الرئيسي	main (على Git)
الشركة الناشرة	Alintiqaa Programming & Digital Solutions

3. البنية الحالية
3.1 شجرة المجلدات
text
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
│   │   │       ├── 20261009005315_InitialCreate.cs
│   │   │       ├── 20261009230747_AddSettingsTable.cs
│   │   │       ├── 20261010225958_AddAuthTables.cs
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
│   │   │   └── AuthController.cs
│   │   ├── Data/
│   │   │   └── DataSeeder.cs
│   │   ├── Dtos/
│   │   │   └── Auth/
│   │   │       ├── LoginRequest.cs
│   │   │       ├── LoginResponse.cs
│   │   │       ├── UserInfoDto.cs
│   │   │       ├── RefreshRequest.cs
│   │   │       └── ChangePasswordRequest.cs
│   │   ├── Services/
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

3.2 مراجع المشروعات
text
Domain          ← لا مراجع
Application     → Domain
Infrastructure  → Application + Domain
Api             → Infrastructure + Application + Domain
Desktop         → Application + Domain  (لا Infrastructure — معماري صارم)
Tests           → كل المشروعات

4. قاعدة البيانات
4.1 الاتصال
السيرفر: localhost\SQLEXPRESS

القاعدة: InvoiceDb

المصادقة: Trusted_Connection (Windows auth)

Connection String:

text
Server=localhost\SQLEXPRESS;Database=InvoiceDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true;Connect Timeout=120

ملاحظة: Connect Timeout=120 ضرورية — SQL Express يستغرق وقتًا في أول اتصال بعد بداية باردة.

4.2 الجداول (14 جدولًا)
الجدول	الوصف
Settings	مفتاح/قيمة (لبيانات الشركة والإعدادات + مسار الشعار)
Suppliers	الموردون
Products	الأصناف (بطاريات + إطارات)
Invoices	الفواتير
InvoiceItems	بنود الفواتير
InvoicePayments	دفعات
InvoiceDiscounts	خصومات
InvoiceExpenses	مصاريف
Users	المستخدمون
Roles	الأدوار (Admin, Accountant, Viewer)
Permissions	الصلاحيات (resource.action)
UserRoles	ربط مستخدم-دور (Many-to-Many)
RolePermissions	ربط دور-صلاحية (Many-to-Many)
__EFMigrationsHistory	سجل EF (نظامي)

4.3 مفاتيح Settings المستخدمة فعليًا
- company_name
- company_address
- company_phone
- company_email
- company_website
- company_logo_path

4.4 علاقات مهمة
Products.SupplierId → Suppliers.Id (Restrict)

Invoices.SupplierId → Suppliers.Id (Restrict)

InvoiceItems.InvoiceId → Invoices.Id (Cascade)

InvoiceItems.ProductId → Products.Id (Restrict)

InvoicePayments/Discounts/Expenses.InvoiceId → Invoices.Id (Cascade)

UserRoles.UserId → Users.Id (Cascade)

UserRoles.RoleId → Roles.Id (Cascade)

RolePermissions.RoleId → Roles.Id (Cascade)

RolePermissions.PermissionId → Permissions.Id (Cascade)

4.5 حقول RowVersion
الجداول التالية بها RowVersion (SQL rowversion):
- Suppliers, Products, Invoices
- Users, Roles

5. قواعد العمل المُنفَّذة
5.1 الأصناف
التصنيفات المسموحة: Batteries, Car Tires, Truck Tires.

بطارية: الكود إلزامي، الاسم = الكود تلقائيًا، النقشة = null. الاسم فريد (بعد NFKC + casefold).

إطار: الاسم + النقشة إلزاميان، الكود يُولَّد SHA256 (أول 32 حرف hex + بادئة TIRE-) = 37 حرفًا.

5.2 الفواتير
دورة الحياة: Draft → Archived (لا رجوع).

الأرشفة: تتطلب حفظ أي تعديلات أولًا؛ الفاتورة المؤرشفة لا تُعدَّل.

الترقيم: INV-{year}-{NNNN} تلقائي متسلسل.

الحسابات: تُطبَّق في InvoiceCalculator مع MidpointRounding.AwayFromZero.

5.3 معادلات الحساب (مطابقة للنسخة Python الأصلية)
text
net_payments  = total_payments − total_discounts
net_cost      = net_payments + total_expenses
exchange_rate = net_cost / total_usd
purchase_dzd  = purchase_usd × exchange_rate
unit_profit   = sale_dzd − purchase_dzd
total_profit  = quantity × unit_profit
profit_margin = (total_profit / total_sales) × 100
الدقة:

Money: 2 منازل

Rate: 6 منازل

Percent: 4 منازل

5.4 التحقق
الكميات والأسعار > 0.

المدفوعات/الخصومات/المصاريف ≥ 0.

الخصومات ≤ المدفوعات.

total_usd > 0.

5.5 المصادقة والصلاحيات
كلمات المرور: BCrypt (Work Factor = 12).

التوكنات: JWT (HMAC-SHA256) — Access 60 دقيقة، Refresh 7 أيام.

Claims محمولة: sub (Id), unique_name (Username), role (اسم الدور), permission (كود الصلاحية), full_name.

الأدوار الافتراضية: Admin (كل الصلاحيات 23)، Accountant (16)، Viewer (6).

الصلاحيات بصيغة resource.action (مثل suppliers.read, invoices.create).

مستخدم admin افتراضي: admin / admin123 (يُنصح بتغييرها بعد أول دخول).

6. API المُنفَّذ (كل النقاط مُختبَرة)
Suppliers (/api/suppliers)
Method	Path	الوصف
GET	/	كل الموردين
GET	/{id}	مورد واحد
POST	/	إنشاء
PUT	/{id}	تعديل
DELETE	/{id}	حذف (يُمنع إن له أصناف/فواتير)

Products (/api/products)
Method	Path	الوصف
GET	/	كل الأصناف (بحث + فلترة)
GET	/{id}	صنف واحد
POST	/	إنشاء (مع قواعد البطارية/الإطار)
PUT	/{id}	تعديل
DELETE	/{id}	حذف (يُمنع إن استُخدم في فاتورة)

Invoices (/api/invoices)
Method	Path	الوصف
GET	/	الفواتير (فلترة status/supplierId/date)
GET	/{id}	فاتورة كاملة (projection بدون دورات JSON)
GET	next-number	الرقم التالي
POST	calculate	حساب معاينة بدون حفظ
POST	/	إنشاء مسودة
PUT	/{id}	تعديل مسودة
POST	/{id}/archive	أرشفة
POST	/{id}/duplicate	تكرار كمسودة جديدة
DELETE	/{id}	حذف (cascade)

Auth (/api/auth) — ✅ جديد
Method	Path	Auth	الوصف
POST	login	AllowAnonymous	تسجيل دخول (يرجع Access + Refresh + UserInfo)
POST	refresh	AllowAnonymous	تجديد Access من Refresh Token
GET	me	Authorize	بيانات المستخدم الحالي + أدواره + صلاحياته
POST	change-password	Authorize	تغيير كلمة المرور

Reports (/api/reports) — 7 نقاط
Method	Path	الوصف
GET	dashboard	KPIs عامة
GET	supplier/{id}	تقرير مورد
GET	profit	تقرير الأرباح
GET	period	تقرير فترة (dateFrom/dateTo)
GET	price-changes/{productId}	تغيّر أسعار صنف
GET	most-profitable	الأكثر ربحية
GET	best-selling	الأكثر مبيعًا

Exports (/api/exports)
Method	Path	الوصف
GET	invoice/{id}/pdf	تنزيل فاتورة PDF
GET	invoice/{id}/excel	تنزيل فاتورة Excel
GET	report/{type}/pdf	تصدير تقرير PDF (7 أنواع)
GET	report/{type}/excel	تصدير تقرير Excel (7 أنواع)

أنواع التقارير المدعومة في report/{type}:
dashboard, supplier, profit, period, price-changes, most-profitable, best-selling

معاملات query المدعومة: period, supplierId, productId, category, topN, dateFrom, dateTo

Settings (/api/settings)
Method	Path	الوصف
GET	/	كل الإعدادات
PUT	/	حفظ دفعة مفتاح/قيمة
POST	/logo	رفع شعار الشركة (multipart، حقل "file")
GET	/logo	استرجاع شعار الشركة الحالي
DELETE	/logo	حذف شعار الشركة الحالي

7. مكان تخزين الشعار
text
%LOCALAPPDATA%\CostProfitApp\Uploads\company\logo.<ext>
يُدار عبر Invoice.Infrastructure.Storage.LogoStorage.

8. حالة Git
الفرع: main

آخر Commit: يُحدَّث بعد commit HANDOFF

عدد Commits: ~35

لا ريموت (GitHub لم يُربط بعد)

أوامر Git المستخدمة
text
git add . ; git commit -m "message"
git log --oneline -N
git status

9. المشاكل المعروفة (Pending)
9.1 ملاحظات على المُصدِّرَين (تم إنجازها ✅)
✅ ترويسة الشركة في PDF/Excel

✅ شعار الشركة (رفع + عرض في ترويسة PDF/Excel)

✅ بيانات المورد الكاملة (عنوان/هاتف/بريد) في PDF/Excel

✅ حدود جدول Excel

✅ إعادة ترتيب الملاحظات + إعادة ترتيب PDF/Excel بالكامل

9.2 نواقص عامة
لا Audit Log بعد (رغم أنه ضمن الخطة) — مؤجَّل.

لا Refresh Token Blacklist (لا يمكن إبطال Refresh قبل انتهائه) — مؤجَّل.

لا اختبارات وحدة حقيقية (فقط smoke test للـ PDF).

لا ربط WinForms بالـ API — Desktop ما زال فارغًا.

لا تمييز بين بطاريات الرصاص والليثيوم (لم يُطلب).

لا حقل "موقع الكتروني" للمورد (Supplier) — فقط: Code/Name/Address/Email/Phone.

لا توجد `[Authorize]` على نقاط الـ API القديمة (Suppliers/Products/Invoices...) — قابلة للإضافة لاحقًا.

لا Policies مبنية على صلاحيات (resource.action) — قابلة للإضافة عبر `AddPolicy`.

10. أولويات العمل القادم
المسار المتفق عليه
~~Auth + JWT + Users + Roles~~ ✅ مكتمل

WinForms Desktop (الخطوة الكبيرة)
- ApiClient (HttpClient مركزي يحمل التوكن)
- LoginForm + MainForm
- 7 صفحات UserControl (Dashboard, Suppliers, Products, Invoice, Archive, Reports, Settings)

حماية نقاط الـ API القديمة بـ [Authorize] + Policies للصلاحيات

Import من Excel

Backup/Restore

Audit Log

أولوية منخفضة
نقل الاختبارات إلى مشروع tests/ (بعد إضافة اختبارات حقيقية)

تحسين الأداء

11. الأوامر السريعة المرجعية
تشغيل الخادم
text
dotnet run --project src/Invoice.Api
ثم:

Swagger: http://localhost:5124/swagger

إيقاف الخادم
text
Ctrl + C   ← في الطرفية
Y          ← إن طُلب تأكيد

بناء الحل
text
dotnet build Invoice.sln
تنظيف كامل
text
taskkill /F /IM dotnet.exe
dotnet clean Invoice.sln
dotnet build Invoice.sln
إنشاء Migration
text
dotnet ef migrations add NAME --project src/Invoice.Infrastructure --startup-project src/Invoice.Infrastructure --output-dir Persistence/Migrations
تطبيق Migration
text
dotnet ef database update --project src/Invoice.Infrastructure --startup-project src/Invoice.Infrastructure
تشغيل الاختبارات
text
dotnet test tests/Invoice.Tests/Invoice.Tests.csproj --filter "FullyQualifiedName~NAME"
تحقق من الجداول
text
sqlcmd -S localhost\SQLEXPRESS -E -d InvoiceDb -C -Q "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE='BASE TABLE' ORDER BY TABLE_NAME"
فحص مستخدمي النظام
text
sqlcmd -S localhost\SQLEXPRESS -E -d InvoiceDb -C -Q "SELECT Id, Username, FullName, IsActive FROM Users"
اختبار تسجيل دخول (PowerShell)
text
$body = @{ username = "admin"; password = "admin123" } | ConvertTo-Json
$r = Invoke-WebRequest -Uri "http://localhost:5124/api/auth/login" -Method POST -ContentType "application/json" -Body $body
$r.StatusCode
حفظ في Git
text
git add . ; git commit -m "message"

12. بيانات الاختبار الحالية في قاعدة البيانات
النوع	القيم
مورد	S-001 / مورد الاختبار الأول / طرابلس، ليبيا / 0910000000
بطارية	id=1, BAT-100, Batteries, 100AH
إطار	id=3, TIRE-9f4e..., Continental HDR2, Truck Tires
فاتورة	id=1, INV-2026-0001, SUP-2026-001، مؤرشفة
بيانات الفاتورة	10 وحدات BAT-100، شراء 50$، بيع 750 د.ل
النتائج	total_usd=500، net_cost=6300، total_sales=7500، profit=1200، margin=16%
إعدادات الشركة	اسم/عنوان/هاتف/إيميل/موقع (تم إدخالها)
شعار الشركة	مرفوع في %LOCALAPPDATA%\CostProfitApp\Uploads\company\logo.png
مستخدم النظام	admin / admin123 (دور Admin، 23 صلاحية)
أدوار النظام	Admin (23)، Accountant (16)، Viewer (6)
صلاحيات النظام	23 صلاحية بصيغة resource.action

13. قواعد التعامل مع المساعد (مهمة!)
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

14. نماذج رسائل جاهزة
رسالة استئناف جديدة
اقرأ HANDOFF.md ثم استأنف من [اسم المهمة].

طلب اختبار
اختبر [endpoint] عبر Swagger وأعطني Response body.

طلب إصلاح
أصلح [المشكلة] وأعطني الكود الكامل.

طلب migration
أضف جدول [الاسم] مع الحقول [القائمة] وأنشئ migration.

15. روابط ومراجع
Swagger: http://localhost:5124/swagger (عند تشغيل الخادم)

API Base: http://localhost:5124/api

Solution: C:\Users\ADNAN\projects\cost_profit\Invoice-CSharp\Invoice.sln

قاعدة البيانات: SQL Server Express - InvoiceDb

مشروع Python الأصلي (مرجع): C:\Users\ADNAN\projects\cost_profit\CostProfitApp

16. حالة الاستقرار الحالية
المكوّن	الحالة	مُختبَر
Domain	✅ مستقر	✅
Application	✅ مستقر	✅
Infrastructure (DB)	✅ مستقر	✅
Infrastructure (PDF)	✅ يعمل مع العربية + الشعار	✅
Infrastructure (Excel)	✅ يعمل مع الحدود + الشعار	✅
API — Suppliers	✅ مستقر	✅
API — Products	✅ مستقر	✅
API — Invoices	✅ مستقر	✅
API — Reports	✅ 7 تقارير	✅
API — Exports (فواتير)	✅ PDF + Excel	✅
API — Exports (تقارير)	✅ PDF + Excel (7 أنواع)	✅
API — Settings	✅ مستقر + شعار	✅
API — Auth	✅ مُنفَّذ (login/refresh/me/change-password)	✅
DataSeeder	✅ ينشئ admin + 3 أدوار + 23 صلاحية	✅
Desktop	⚪ لم يُعدّل	❌

التقييم العام: الخادم جاهز وظيفيًا للإنتاج في بيئة LAN مع مصادقة JWT. تطبيق سطح المكتب لم يبدأ بعد.

17. نقطة الاستئناف الحالية
الحالة: Auth + JWT مكتمل بنجاح ✅

ما تم إنجازه (10 خطوات):
1. ✅ كيانات المصادقة في Domain (User, Role, Permission, UserRole, RolePermission)
2. ✅ تهيئة العلاقات في AppDbContext
3. ✅ Migration AddAuthTables + تطبيقه على القاعدة
4. ✅ تثبيت Microsoft.AspNetCore.Authentication.JwtBearer 8.0.11
5. ✅ إعدادات JWT في appsettings.json
6. ✅ JwtService (توليد Access + Refresh + التحقق)
7. ✅ DTOs + BcryptPasswordHasher
8. ✅ AuthController (login, refresh, me, change-password)
9. ✅ تفعيل Middleware في Program.cs
10. ✅ DataSeeder + اختبار كامل عبر Swagger

القرارات التقنية المطبَّقة:
- تخزين كلمات المرور: BCrypt.Net-Next 4.2.0 (Work Factor = 12)
- Tokens: JWT (Access 60 دقيقة، Refresh 7 أيام)
- الأدوار: Admin (23)، Accountant (16)، Viewer (6)
- الصلاحيات: صيغة resource.action
- مستخدم افتراضي: admin / admin123

المهمة القادمة (المسار المتفق عليه):
WinForms Desktop — إنشاء ApiClient + LoginForm + MainForm + 7 صفحات UserControl.

قبل ذلك (اختياري لكن مُوصى به):
إضافة [Authorize] + Policies على نقاط API القديمة لحمايتها بالصلاحيات.

نهاية HANDOFF.

عند بدء جلسة جديدة، اقرأ هذا الملف بالكامل وابدأ من القسم 17 (نقطة الاستئناف).