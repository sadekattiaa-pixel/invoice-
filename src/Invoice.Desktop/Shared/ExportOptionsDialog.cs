using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Invoice.Desktop.Shared;

/// <summary>
/// نافذة اختيار الأقسام التي تظهر عند تصدير الفاتورة إلى PDF/Excel.
/// تُعيد قائمة المفاتيح المخفية (hidden keys) التي يفهمها الخادم.
///
/// المفاتيح المدعومة من الخادم (InvoicePdfExporter.Keys / InvoiceExcelExporter):
///   logo, company_info, supplier_info, invoice_info,
///   items.line_no, items.product, items.quantity,
///   items.purchase_usd, items.sale_dzd,
///   items.total_sale, items.total_profit,
///   notes, payments, discounts, expenses, summary
/// </summary>
public class ExportOptionsDialog : Form
{
    // كل صف يمثّل مجموعة أقسام تتحكم بها مربع اختيار واحد.
    // عند إلغاء تحديد الصف → تُضاف كل مفاتيحه إلى قائمة hidden.
    private static readonly (string GroupLabel, string[] Keys)[] Groups =
    {
        ("شعار الشركة",              new[] { "logo" }),
        ("بيانات الشركة",            new[] { "company_info" }),
        ("بيانات المورد",            new[] { "supplier_info" }),
        ("بيانات الفاتورة",          new[] { "invoice_info" }),
        ("جدول الأصناف",             new[]
        {
            "items.line_no", "items.product", "items.quantity",
            "items.purchase_usd", "items.sale_dzd",
            "items.total_sale", "items.total_profit"
        }),
        ("الملاحظات",                new[] { "notes" }),
        ("المدفوعات",                new[] { "payments" }),
        ("الخصومات",                 new[] { "discounts" }),
        ("المصاريف",                 new[] { "expenses" }),
        ("ملخص الحساب (الإجماليات)",  new[] { "summary" }),
    };

    private readonly List<CheckBox> _checkBoxes = new();

    /// <summary>مفاتيح الأقسام المخفية (التي أُلغي تحديدها).</summary>
    public List<string> HiddenKeys { get; private set; } = new();

    private ExportOptionsDialog(IEnumerable<string>? initiallyHidden)
    {
        var hidden = new HashSet<string>(
            initiallyHidden ?? Enumerable.Empty<string>(),
            StringComparer.OrdinalIgnoreCase);

        BuildUi(hidden);
    }

    /// <summary>
    /// يفتح النافذة ويُعيد قائمة المفاتيح المخفية.
    /// - null      = المستخدم ألغى العملية.
    /// - قائمة فارغة = كل الأقسام ظاهرة (لا شيء مخفي).
    /// </summary>
    public static List<string>? ShowFor(
        IWin32Window? owner = null,
        IEnumerable<string>? initiallyHidden = null)
    {
        using var dlg = new ExportOptionsDialog(initiallyHidden);
        var result = owner == null ? dlg.ShowDialog() : dlg.ShowDialog(owner);
        return result == DialogResult.OK ? dlg.HiddenKeys : null;
    }

    private void BuildUi(HashSet<string> hidden)
    {
        Text = "خيارات التصدير";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        ClientSize = new Size(420, 500);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.WhiteSmoke;

        // ============ العنوان ============
        var title = new Label
        {
            Text = "اختر الأقسام التي تريد إظهارها في ملف التصدير:",
            Dock = DockStyle.Top,
            Height = 40,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 10, 15, 0),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 60, 120)
        };

        // ============ منطقة قائمة الخيارات ============
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(20, 5, 20, 5),
            BackColor = Color.White
        };

        foreach (var (label, keys) in Groups)
        {
            // ظاهر افتراضيًا إلا إن كانت كل مفاتيحه مخفية سابقًا
            bool allHidden = keys.All(k => hidden.Contains(k));

            var cb = new CheckBox
            {
                Text = label,
                Checked = !allHidden,
                Tag = keys,
                Dock = DockStyle.Top,
                Height = 32,
                Padding = new Padding(0, 6, 0, 0)
            };
            _checkBoxes.Add(cb);
            panel.Controls.Add(cb);
        }

        // ============ الأزرار ============
        var btnPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.WhiteSmoke
        };

        var btnOk = new Button
        {
            Text = "موافق",
            Width = 120,
            Height = 38,
            Location = new Point(290, 12),
            BackColor = Color.FromArgb(46, 160, 67),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += (_, _) => Confirm();

        var btnShowAll = new Button
        {
            Text = "إظهار الكل",
            Width = 110,
            Height = 38,
            Location = new Point(170, 12),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnShowAll.Click += (_, _) =>
        {
            foreach (var cb in _checkBoxes) cb.Checked = true;
        };

        var btnCancel = new Button
        {
            Text = "إلغاء",
            Width = 110,
            Height = 38,
            Location = new Point(50, 12),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        btnPanel.Controls.Add(btnOk);
        btnPanel.Controls.Add(btnShowAll);
        btnPanel.Controls.Add(btnCancel);

        // ============ التركيب ============
        Controls.Add(panel);
        Controls.Add(btnPanel);
        Controls.Add(title);

        AcceptButton = btnOk;
        CancelButton = btnCancel;
    }

    private void Confirm()
    {
        var hidden = new List<string>();
        foreach (var cb in _checkBoxes)
        {
            if (cb.Checked) continue; // ظاهر → لا نضيفه لقائمة hidden
            if (cb.Tag is string[] keys)
                hidden.AddRange(keys);
        }

        HiddenKeys = hidden
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        DialogResult = DialogResult.OK;
        Close();
    }
}