using Invoice.Desktop.Services;

namespace Invoice.Desktop.Shared;

/// <summary>
/// نافذة اختيار الأعمدة — قابلة لإعادة الاستخدام في كل الشاشات.
/// تُعرض قائمة checkboxes، ويُحفظ الاختيار في PreferencesService.
/// </summary>
public class ColumnsDialog : Form
{
    private readonly string _gridKey;
    private readonly List<(string Key, string Header)> _columns;
    private readonly List<CheckBox> _checkBoxes = new();

    /// <summary>
    /// إنشاء نافذة اختيار الأعمدة.
    /// </summary>
    /// <param name="gridKey">معرّف فريد للجدول (مثال: "Products", "Invoices", "InvoiceItems").</param>
    /// <param name="columns">قائمة الأعمدة (المفتاح، العنوان).</param>
    public ColumnsDialog(string gridKey, IEnumerable<(string Key, string Header)> columns)
    {
        _gridKey = gridKey;
        _columns = columns.ToList();
        BuildUi();
    }

    private void BuildUi()
    {
        Text = "إظهار / إخفاء الأعمدة";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(340, 480);
        RightToLeft = RightToLeft.Yes;
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.WhiteSmoke;

        var title = new Label
        {
            Text = "اختر الأعمدة التي تريد عرضها:",
            Dock = DockStyle.Top,
            Height = 32,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 8, 15, 0),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };

        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(20, 5, 20, 5),
            BackColor = Color.White
        };

        var prefs = PreferencesService.Instance;
        foreach (var (key, header) in _columns)
        {
            var cb = new CheckBox
            {
                Text = header,
                Checked = !prefs.IsColumnHidden(_gridKey, key),
                Tag = key,
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(0, 5, 0, 0)
            };
            _checkBoxes.Add(cb);
            panel.Controls.Add(cb);
        }

        // الأزرار
        var btnPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 55,
            BackColor = Color.WhiteSmoke
        };

        var btnSave = new Button
        {
            Text = "حفظ",
            Width = 110,
            Height = 36,
            Location = new Point(210, 10),
            BackColor = Color.FromArgb(46, 160, 67),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnSave.FlatAppearance.BorderSize = 0;
        btnSave.Click += (_, _) => SaveAndClose();

        var btnShowAll = new Button
        {
            Text = "إظهار الكل",
            Width = 110,
            Height = 36,
            Location = new Point(90, 10),
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
            Width = 80,
            Height = 36,
            Location = new Point(5, 10),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        btnPanel.Controls.Add(btnSave);
        btnPanel.Controls.Add(btnShowAll);
        btnPanel.Controls.Add(btnCancel);

        Controls.Add(panel);
        Controls.Add(btnPanel);
        Controls.Add(title);

        AcceptButton = btnSave;
        CancelButton = btnCancel;
    }

    private void SaveAndClose()
    {
        var prefs = PreferencesService.Instance;
        foreach (var cb in _checkBoxes)
        {
            var key = cb.Tag as string ?? "";
            if (string.IsNullOrEmpty(key)) continue;
            prefs.SetColumnVisible(_gridKey, key, cb.Checked);
        }
        DialogResult = DialogResult.OK;
        Close();
    }

    // ============================================================
    // Static helpers — تُستخدم من الشاشات
    // ============================================================

    /// <summary>يُطبّق تفضيلات الإخفاء على DataGridView.</summary>
    public static void Apply(DataGridView grid, string gridKey)
    {
        var prefs = PreferencesService.Instance;
        foreach (DataGridViewColumn col in grid.Columns)
        {
            col.Visible = !prefs.IsColumnHidden(gridKey, col.Name);
        }
    }

    /// <summary>يفتح النافذة من قائمة أعمدة DataGridView.</summary>
    public static void ShowFor(DataGridView grid, string gridKey, IWin32Window? owner = null)
    {
        var cols = new List<(string, string)>();
        foreach (DataGridViewColumn col in grid.Columns)
        {
            var header = string.IsNullOrEmpty(col.HeaderText) ? col.Name : col.HeaderText;
            cols.Add((col.Name, header));
        }
        using var dlg = new ColumnsDialog(gridKey, cols);
        if (owner != null) dlg.ShowDialog(owner);
        else dlg.ShowDialog();

        Apply(grid, gridKey);
    }

    /// <summary>يُرجع قائمة المفاتيح المخفية للجدول (لاستخدامها في التصدير).</summary>
    public static List<string> GetHiddenKeys(string gridKey)
    {
        var prefs = PreferencesService.Instance;
        var hidden = prefs.Data.HiddenColumns.TryGetValue(gridKey, out var set)
            ? set.ToList()
            : new List<string>();
        return hidden;
    }
}