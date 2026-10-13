using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Invoice.Desktop.Shared;

/// <summary>
/// نافذة خيارات التصدير — قسمان:
///   1) أجزاء الصفحة (شعار، شركة، مورد، فاتورة، ملاحظات، مدفوعات، خصومات، مصاريف، ملخص).
///   2) أعمدة جدول الأصناف (21 عمودًا) — قابلة للتحديد فرديًا.
/// تُعيد قائمة المفاتيح المخفية التي يفهمها الخادم.
/// </summary>
public class ExportOptionsDialog : Form
{
    // ============ أجزاء الصفحة (Groups) ============
    private static readonly (string Label, string[] Keys)[] PageParts =
    {
        ("شعار الشركة",              new[] { "logo" }),
        ("بيانات الشركة",            new[] { "company_info" }),
        ("بيانات المورد",            new[] { "supplier_info" }),
        ("بيانات الفاتورة",          new[] { "invoice_info" }),
        ("الملاحظات",                new[] { "notes" }),
        ("المدفوعات",                new[] { "payments" }),
        ("الخصومات",                 new[] { "discounts" }),
        ("المصاريف",                 new[] { "expenses" }),
        ("ملخص الحساب (الإجماليات)",  new[] { "summary" }),
    };

    // ============ أعمدة جدول الأصناف (21 عمودًا) ============
    private static readonly (string Label, string Key)[] ItemColumns =
    {
        ("#",                 "items.line_no"),
        ("الكود",             "items.code"),
        ("AH",                "items.ah"),
        ("L",                 "items.l"),
        ("W",                 "items.w"),
        ("H",                 "items.h"),
        ("OH",                "items.oh"),
        ("LAYOUT",            "items.layout"),
        ("الصورة",            "items.image"),
        ("PALLET",            "items.pallet"),
        ("التصنيف",           "items.category"),
        ("النقشة",            "items.pattern"),
        ("الكمية",            "items.quantity"),
        ("الشراء $",          "items.purchase_usd"),
        ("اجمالي الشراء $",   "items.total_purchase_usd"),
        ("التكلفة",           "items.purchase_dzd"),
        ("اجمالي التكلفة",    "items.total_dzd"),
        ("البيع",             "items.sale_dzd"),
        ("اجمالي البيع",      "items.total_sale"),
        ("الربح",             "items.unit_profit"),
        ("اجمالي الربح",      "items.total_profit"),
    };

    private readonly List<CheckBox> _pagePartChecks = new();
    private readonly List<CheckBox> _itemColumnChecks = new();

    /// <summary>مفاتيح الأجزاء/الأعمدة المخفية (التي أُلغي تحديدها).</summary>
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
    /// - null = المستخدم ألغى العملية.
    /// - قائمة فارغة = كل الأجزاء/الأعمدة ظاهرة.
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
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        MinimumSize = new Size(500, 600);
        ClientSize = new Size(560, 740);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.WhiteSmoke;

        // ============ العنوان ============
        var title = new Label
        {
            Text = "اختر الأجزاء والأعمدة التي تريد إظهارها في ملف التصدير:",
            Dock = DockStyle.Top,
            Height = 40,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 10, 15, 0),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 60, 120)
        };

        // ============ منطقة التمرير ============
        var flow = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true,
            Padding = new Padding(20, 5, 20, 5),
            BackColor = Color.White
        };

        // ---------- القسم 1: أجزاء الصفحة ----------
        AddSectionTitle(flow, "أجزاء الصفحة");

        foreach (var (label, keys) in PageParts)
        {
            bool allHidden = keys.All(k => hidden.Contains(k));

            var cb = new CheckBox
            {
                Text = label,
                Checked = !allHidden,
                Tag = keys,
                AutoSize = false,
                Height = 28,
                Padding = new Padding(0, 5, 0, 0)
            };
            _pagePartChecks.Add(cb);
            flow.Controls.Add(cb);
        }

        // ---------- فاصل ----------
        flow.Controls.Add(new Panel
        {
            Height = 15,
            Width = 10,
            BackColor = Color.WhiteSmoke
        });

        // ---------- القسم 2: أعمدة جدول الأصناف ----------
        AddSectionTitle(flow, "أعمدة جدول الأصناف");

        // أزرار تحكم سريعة
        var quickPanel = new Panel
        {
            Height = 32,
            Width = 300,
            BackColor = Color.Transparent,
            Margin = new Padding(0, 0, 0, 6)
        };

        var btnShowAllItems = new Button
        {
            Text = "إظهار كل الأعمدة",
            Width = 150,
            Height = 28,
            Location = new Point(0, 2),
            BackColor = Color.FromArgb(46, 160, 67),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9F)
        };
        btnShowAllItems.FlatAppearance.BorderSize = 0;

        var btnHideAllItems = new Button
        {
            Text = "إخفاء كل الأعمدة",
            Width = 150,
            Height = 28,
            Location = new Point(158, 2),
            BackColor = Color.FromArgb(200, 50, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9F)
        };
        btnHideAllItems.FlatAppearance.BorderSize = 0;

        quickPanel.Controls.Add(btnShowAllItems);
        quickPanel.Controls.Add(btnHideAllItems);
        flow.Controls.Add(quickPanel);

        btnShowAllItems.Click += (_, _) =>
        {
            foreach (var cb in _itemColumnChecks) cb.Checked = true;
        };
        btnHideAllItems.Click += (_, _) =>
        {
            foreach (var cb in _itemColumnChecks) cb.Checked = false;
        };

        // الـ 21 عمودًا
        foreach (var (label, key) in ItemColumns)
        {
            bool isHidden = hidden.Contains(key);

            var cb = new CheckBox
            {
                Text = label,
                Checked = !isHidden,
                Tag = key,
                AutoSize = false,
                Height = 26,
                Padding = new Padding(0, 4, 0, 0)
            };
            _itemColumnChecks.Add(cb);
            flow.Controls.Add(cb);
        }

        // ضبط العرض لكل عنصر تلقائيًا حسب عرض الحاوية
        void StretchWidths()
        {
            int w = flow.ClientSize.Width - flow.Padding.Horizontal - 25;
            if (w < 150) w = 150;
            foreach (Control c in flow.Controls)
            {
                // لا نمدّد الفاصل الرأسي
                if (c is Panel p && p.Height == 15) continue;
                c.Width = w;
            }
        }
        flow.Resize += (_, _) => StretchWidths();

        // ============ الأزرار السفلية ============
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
            Location = new Point(420, 12),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = Color.FromArgb(46, 160, 67),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };
        btnOk.FlatAppearance.BorderSize = 0;
        btnOk.Click += (_, _) => Confirm();

        var btnCancel = new Button
        {
            Text = "إلغاء",
            Width = 110,
            Height = 38,
            Location = new Point(300, 12),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        btnPanel.Resize += (_, _) =>
        {
            btnOk.Location = new Point(btnPanel.Width - 140, 12);
            btnCancel.Location = new Point(btnPanel.Width - 260, 12);
        };

        btnPanel.Controls.Add(btnOk);
        btnPanel.Controls.Add(btnCancel);

        // ============ التركيب ============
        Controls.Add(flow);
        Controls.Add(btnPanel);
        Controls.Add(title);

        AcceptButton = btnOk;
        CancelButton = btnCancel;

        // ضبط العرض الأولي بعد فتح النافذة
        Shown += (_, _) => StretchWidths();
    }

    private static void AddSectionTitle(FlowLayoutPanel parent, string titleText)
    {
        var lbl = new Label
        {
            Text = titleText,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 60, 120),
            AutoSize = false,
            Height = 30,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 10, 10, 0)
        };
        parent.Controls.Add(lbl);
    }

    private void Confirm()
    {
        var hidden = new List<string>();

        // أجزاء الصفحة
        foreach (var cb in _pagePartChecks)
        {
            if (cb.Checked) continue;
            if (cb.Tag is string[] keys)
                hidden.AddRange(keys);
        }

        // أعمدة الأصناف
        foreach (var cb in _itemColumnChecks)
        {
            if (cb.Checked) continue;
            if (cb.Tag is string key)
                hidden.Add(key);
        }

        HiddenKeys = hidden
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        DialogResult = DialogResult.OK;
        Close();
    }
}