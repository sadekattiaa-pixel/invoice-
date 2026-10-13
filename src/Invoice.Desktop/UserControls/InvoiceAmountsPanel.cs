using Invoice.Desktop.Models;

namespace Invoice.Desktop.UserControls;

/// <summary>
/// لوحة قابلة لإعادة الاستخدام لعرض قائمة مبالغ (مدفوعات/خصومات/مصاريف).
/// - إضافة / تعديل / حذف.
/// - تعرض الإجمالي.
/// - تُطلق حدث Changed عند أي تعديل.
/// </summary>
public class InvoiceAmountsPanel : UserControl
{
    private DataGridView _grid = null!;
    private Button _btnAdd = null!;
    private Button _btnEdit = null!;
    private Button _btnDelete = null!;
    private Label _lblTotal = null!;
    private Label _lblTitle = null!;

    private readonly List<InvoiceAmountRequest> _items = new();

    /// <summary>عنوان اللوحة (مثال: "المدفوعات").</summary>
    public string Title
    {
        get => _lblTitle.Text;
        set => _lblTitle.Text = value;
    }

    /// <summary>يُطلق عند تغيّر القائمة (إضافة/تعديل/حذف).</summary>
    public event Action? Changed;

    public IReadOnlyList<InvoiceAmountRequest> Items => _items;

    public decimal Total => _items.Sum(x => x.Amount);

    public InvoiceAmountsPanel()
    {
        BuildUi();
    }

    private void BuildUi()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.White;
        RightToLeft = RightToLeft.Yes;
        Font = new Font("Segoe UI", 10F);
        BorderStyle = BorderStyle.FixedSingle;

        // ============ العنوان + الأزرار ============
        var headerPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 40,
            BackColor = Color.FromArgb(230, 240, 250),
            Padding = new Padding(8, 0, 8, 0)
        };

        _lblTitle = new Label
        {
            Text = "المبالغ",
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 60, 120),
            Dock = DockStyle.Right,
            Width = 180,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 10, 0)
        };

        _btnAdd = MakeSmallButton("إضافة", Color.FromArgb(46, 160, 67), DockStyle.Left);
        _btnAdd.Click += (_, _) => OnAdd();

        _btnEdit = MakeSmallButton("تعديل", Color.FromArgb(230, 160, 30), DockStyle.Left);
        _btnEdit.Click += (_, _) => OnEdit();

        _btnDelete = MakeSmallButton("حذف", Color.FromArgb(200, 50, 50), DockStyle.Left);
        _btnDelete.Click += (_, _) => OnDelete();

        // ترتيب من اليسار: حذف، تعديل، إضافة (لأن Dock=Left يُضاف بترتيب معكوس)
        headerPanel.Controls.Add(_btnAdd);
        headerPanel.Controls.Add(_btnEdit);
        headerPanel.Controls.Add(_btnDelete);
        headerPanel.Controls.Add(_lblTitle);

        // ============ الإجمالي ============
        var footerPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 34,
            BackColor = Color.FromArgb(245, 245, 250),
            Padding = new Padding(8, 0, 8, 0)
        };
        _lblTotal = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 60, 120),
            Padding = new Padding(0, 0, 10, 0)
        };
        footerPanel.Controls.Add(_lblTotal);

        // ============ الجدول ============
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.None,
            RightToLeft = RightToLeft.Yes,
            Font = new Font("Segoe UI", 10F),
            ColumnHeadersHeight = 30,
            EnableHeadersVisualStyles = false
        };
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 60, 120);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 220, 250);
        _grid.DefaultCellStyle.SelectionForeColor = Color.Black;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 250);
        _grid.RowTemplate.Height = 26;
        _grid.CellDoubleClick += (_, _) => OnEdit();

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Label", HeaderText = "الوصف",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            DataPropertyName = "Amount", HeaderText = "المبلغ",
            Width = 130,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                Format = "N2",
                Alignment = DataGridViewContentAlignment.MiddleCenter
            }
        });

        Controls.Add(_grid);
        Controls.Add(footerPanel);
        Controls.Add(headerPanel);

        RefreshGrid();
    }

    private static Button MakeSmallButton(string text, Color back, DockStyle dock)
    {
        var b = new Button
        {
            Text = text,
            Width = 75,
            Dock = dock,
            BackColor = back,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 9F),
            Margin = new Padding(2)
        };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    // ============================================================
    // Public API
    // ============================================================

    /// <summary>تحميل قائمة من الـ API (عند فتح فاتورة موجودة).</summary>
    public void LoadItems(IEnumerable<InvoiceAmountDto> items)
    {
        _items.Clear();
        foreach (var i in items)
        {
            _items.Add(new InvoiceAmountRequest
            {
                Label = i.Label ?? "",
                Amount = i.Amount
            });
        }
        RefreshGrid();
    }

    /// <summary>مسح كل العناصر (فاتورة جديدة).</summary>
    public void ClearAll()
    {
        _items.Clear();
        RefreshGrid();
    }

    // ============================================================
    // Actions
    // ============================================================

    private void OnAdd()
    {
        using var dlg = new AmountEntryDialog($"إضافة إلى {Title}", "");
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        _items.Add(new InvoiceAmountRequest { Label = dlg.Label, Amount = dlg.Amount });
        RefreshGrid();
        Changed?.Invoke();
    }

    private void OnEdit()
    {
        if (_grid.CurrentRow?.DataBoundItem is not InvoiceAmountRequest row) return;
        var idx = _items.IndexOf(row);
        if (idx < 0) return;

        using var dlg = new AmountEntryDialog($"تعديل في {Title}", row.Label ?? "", row.Amount);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        _items[idx] = new InvoiceAmountRequest { Label = dlg.Label, Amount = dlg.Amount };
        RefreshGrid();
        Changed?.Invoke();
    }

    private void OnDelete()
    {
        if (_grid.CurrentRow?.DataBoundItem is not InvoiceAmountRequest row) return;
        var idx = _items.IndexOf(row);
        if (idx < 0) return;

        var confirm = MessageBox.Show(
            $"هل تريد حذف «{row.Label}»؟",
            "تأكيد الحذف",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;

        _items.RemoveAt(idx);
        RefreshGrid();
        Changed?.Invoke();
    }

    private void RefreshGrid()
    {
        _grid.DataSource = null;
        _grid.DataSource = _items.ToList();
        _lblTotal.Text = $"الإجمالي: {Total:N2} د.ل";
    }
}