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
المصادقة	متعدد المستخدمين بأدوار (لم يُنفّذ بعد)
النقل	HTTP + API Key + JWT (لم يُنفّذ بعد)
الفرع الرئيسي	main (على Git)
الشركة الناشرة	Alintilqa Programming & Digital Solutions

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
│   │       └── Settings.cs
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
│   │       ├── CompanyInfoDto.cs          ← جديد
│   │       ├── ReportColumn.cs            ← جديد
│   │       ├── ReportRow.cs               ← جديد
│   │       ├── ReportSummaryItem.cs       ← جديد
│   │       └── ReportTableDto.cs          ← جديد
│   │
│   ├── Invoice.Infrastructure/       (class library)
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs
│   │   │   ├── DesignTimeDbContextFactory.cs
│   │   │   └── Migrations/
│   │   │       ├── 20261009005315_InitialCreate.cs
│   │   │       ├── 20261009230747_AddSettingsTable.cs
│   │   │       └── AppDbContextModelSnapshot.cs
│   │   ├── Exporters/
│   │   │   ├── InvoicePdfExporter.cs      ← معدَّل (2 صفحات + شعار)
│   │   │   ├── InvoiceExcelExporter.cs    ← معدَّل (ورقتان + شعار)
│   │   │   ├── ReportPdfExporter.cs       ← جديد
│   │   │   └── ReportExcelExporter.cs     ← جديد
│   │   ├── Storage/                       ← جديد
│   │   │   └── LogoStorage.cs             ← جديد
│   │   └── Services/                      ← جديد
│   │       └── ReportDataService.cs       ← جديد
│   │
│   ├── Invoice.Api/                  (webapi)
│   │   ├── Controllers/
│   │   │   ├── SuppliersController.cs
│   │   │   ├── ProductsController.cs
│   │   │   ├── InvoicesController.cs
│   │   │   ├── ReportsController.cs
│   │   │   ├── ExportsController.cs       ← معدَّل (تقارير PDF/Excel)
│   │   │   └── SettingsController.cs      ← معدَّل (رفع/استرجاع الشعار)
│   │   ├── Program.cs                     ← معدَّل (تسجيل ReportDataService)
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
Server=localhost\SQLEXPRESS;Database=InvoiceDb;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=true

4.2 الجداول (9 جداول)
الجدول	الوصف
Settings	مفتاح/قيمة (لبيانات الشركة والإعدادات + مسار الشعار)
Suppliers	الموردون
Products	الأصناف (بطاريات + إطارات)
Invoices	الفواتير
InvoiceItems	بنود الفواتير
InvoicePayments	دفعات
InvoiceDiscounts	خصومات
InvoiceExpenses	مصاريف
__EFMigrationsHistory	سجل EF (نظامي)

4.3 مفاتيح Settings المستخدمة فعليًا
- company_name
- company_address
- company_phone
- company_email
- company_website
- company_logo_path  ← يُضاف تلقائيًا عند رفع شعار

4.4 علاقات مهمة
Products.SupplierId → Suppliers.Id (Restrict)

Invoices.SupplierId → Suppliers.Id (Restrict)

InvoiceItems.InvoiceId → Invoices.Id (Cascade)

InvoiceItems.ProductId → Products.Id (Restrict)

InvoicePayments/Discounts/Expenses.InvoiceId → Invoices.Id (Cascade)

4.5 حقول RowVersion
كل الجداول الرئيسية (Suppliers, Products, Invoices) بها RowVersion (SQL rowversion) لمنع تعارض التعديلات.

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
GET	report/{type}/pdf	تصدير تقرير PDF (7 أنواع)         ← جديد
GET	report/{type}/excel	تصدير تقرير Excel (7 أنواع)       ← جديد

أنواع التقارير المدعومة في report/{type}:
dashboard, supplier, profit, period, price-changes, most-profitable, best-selling

معاملات query المدعومة: period, supplierId, productId, category, topN, dateFrom, dateTo

Settings (/api/settings)
Method	Path	الوصف
GET	/	كل الإعدادات
PUT	/	حفظ دفعة مفتاح/قيمة
POST	/logo	رفع شعار الشركة (multipart، حقل "file")   ← جديد
GET	/logo	استرجاع شعار الشركة الحالي                  ← جديد
DELETE	/logo	حذف شعار الشركة الحالي                       ← جديد

7. مكان تخزين الشعار
text
%LOCALAPPDATA%\CostProfitApp\Uploads\company\logo.<ext>
يُدار عبر Invoice.Infrastructure.Storage.LogoStorage.

8. حالة Git
الفرع: main

آخر Commit: 2ab8e29 (Add PDF/Excel report exporters for all 7 report types)

عدد Commits: ~24

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

✅ إعادة ترتيب الملاحظات + إعادة ترتيب PDF/Excel بالكامل:
   - PDF: صفحتان (صفحة 1: الترويسة + المورد + الأصناف + ملاحظات / صفحة 2: مدفوعات + خصومات + مصاريف + ملخص)
   - Excel: ورقتان بنفس الترتيب

9.2 نواقص عامة
لا مصادقة (Auth/JWT) بعد.

لا Audit Log بعد (رغم أنه ضمن الخطة).

لا اختبارات وحدة حقيقية (فقط smoke test للـ PDF).

لا ربط WinForms بالـ API — Desktop ما زال فارغًا.

لا تمييز بين بطاريات الرصاص والليثيوم (لم يُطلب).

لا حقل "موقع الكتروني" للمورد (Supplier) — فقط: Code/Name/Address/Email/Phone.

10. أولويات العمل القادم
المسار المتفق عليه (أولوية عالية)
Auth + JWT + Users + Roles

جداول: Users, Roles, Permissions, RolePermissions

Endpoints: login, refresh, me, change-password

Middleware للتحقق

WinForms Desktop (الخطوة الكبيرة)

ApiClient (HttpClient مركزي)

LoginForm + MainForm

7 صفحات UserControl (Dashboard, Suppliers, Products, Invoice, Archive, Reports, Settings)

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

مثال: http://localhost:5124/api/suppliers

إيقاف الخادم
text
Ctrl + C   ← في الطرفية
Y          ← إن طُلب تأكيد

بناء الحل
text
dotnet build Invoice.sln
تنظيف كامل (عند الشك بمشاكل cache)
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
حفظ في Git
text
git add . ; git commit -m "message"

12. بيانات الاختبار الحالية في قاعدة البيانات
النوع	القيم
مورد	S-001 / مورد الاختبار الأول / طرابلس، ليبيا / 0910000000
بطارية	id=1, BAT-100, Batteries, 100AH
إطار	id=3, TIRE-9f4e..., Continental HDR2, Truck Tires
فاتورة	id=1, INV-2026-0001, SUP-2026-001, مؤرشفة
بيانات الفاتورة	10 وحدات BAT-100، شراء 50$، بيع 750 د.ل
النتائج	total_usd=500، net_cost=6300، total_sales=7500، profit=1200، margin=16%
إعدادات الشركة	اسم/عنوان/هاتف/إيميل/موقع (تم إدخالها)
شعار الشركة	مرفوع في %LOCALAPPDATA%\CostProfitApp\Uploads\company\logo.png

13. قواعد التعامل مع المساعد (مهمة!)
✅ خطوة واحدة في كل رسالة.

✅ الكود المعدَّل يُعطى كاملًا — لا "عدّل السطر".

✅ انتظر الناتج قبل الانتقال للخطوة التالية.

✅ لا تفترض نجاح أمر — تحقق دائمًا.

✅ الصق الأخطاء كاملة عند الحدوث.

✅ مرجع واحد للبناء: VS Code + Terminal.

✅ Git commit بعد كل وحدة منطقية.

✅ اللغة: عربية للشرح، إنجليزية للكود.

✅ عند تعديل ملف، أعطِ أولًا أمر `code path` لفتحه.

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
Desktop	⚪ لم يُعدّل	❌
التقييم العام: الخادم جاهز وظيفيًا للإنتاج في بيئة LAN، ينقصه المصادقة. تطبيق سطح المكتب لم يبدأ.

17. نقطة الاستئناف الموصى بها
اعمل على نقطة 10: Auth + JWT + Users + Roles.

الخطوة التالية الدقيقة: تصميم جداول Users/Roles/Permissions وإنشاء Migration.

نهاية HANDOFF.

عند بدء جلسة جديدة، اقرأ هذا الملف بالكامل وابدأ من القسم 17.