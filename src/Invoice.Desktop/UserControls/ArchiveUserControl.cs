using Invoice.Desktop.Models;
using Invoice.Desktop.Services;
using Invoice.Desktop.Shared;

namespace Invoice.Desktop.UserControls;

/// <summary>
/// صفحة أرشيف الفواتير — عرض كل الفواتير (مسودة + مؤرشفة) مع فلترة وتصدير.
/// - فلترة: الحالة، المورد، نطاق تاريخ، بحث.
/// - إخفاء الأعمدة (ColumnsDialog).
/// - تصدير PDF/Excel مع نافذة خيارات الأقسام (ExportOptionsDialog).
/// - PDF: معاينة أولًا عبر PdfPreviewForm ثم الحفظ (بند 16).
/// - النقر المزدوج على فاتورة يطلب فتحها (يُطلِق حدث InvoiceOpenRequested).
/// </summary>
public class ArchiveUserControl : UserControl
{
    private const string GridKey = "Archive";

    private Button _btnRefresh = null!;
    private Button _btnColumns = null!;
    private Button _btnExportPdf = null!;
    private Button _btnExportExcel = null!;

    private TextBox _txtSearch = null!;
    private ComboBox _cmbStatus = null!;
    private ComboBox _cmbSupplier = null!;
    private CheckBox _chkUseDates = null!;
    private DateTimePicker _dtFrom = null!;
    private DateTimePicker _dtTo = null!;

    private DataGridView _grid = null!;
    private Label _lblStatus = null!;

    private List<InvoiceListRow> _all = new();
    private List<SupplierItem> _suppliers = new();

    private bool _suppressWidthSave = false;

    /// <summary>يُطلَق عند النقر المزدوج على فاتورة — يمرر معرّف الفاتورة.</summary>
    public event Action<long>? InvoiceOpenRequested;

    public ArchiveUserControl()
    {
        BuildUi();
        ColumnsDialog.Apply(_grid, GridKey);
        ApplyColumnWidths();
        HookColumnWidthTracking();
        Load += async (_, _) => await LoadAllAsync();
    }

    private void BuildUi()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.White;
        RightToLeft = RightToLeft.Yes;
        Font = new Font("Segoe UI", 10F);
        Padding = new Padding(10);

        var toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 55,
            BackColor = Color.WhiteSmoke,
            Padding = new Padding(10)
        };

        _btnRefresh = MakeButton("تحديث", Color.FromArgb(30, 100, 200), 10);
        _btnRefresh.Click += async (_, _) => await LoadAllAsync();

        _btnColumns = MakeButton("الأعمدة", Color.FromArgb(90, 90, 120), 115);
        _btnColumns.Click += (_, _) => OnChooseColumns();

        _btnExportPdf = MakeButton("تصدير PDF", Color.FromArgb(200, 60, 60), 220);
        _btnExportPdf.Click += async (_, _) => await OnExportAsync("pdf");

        _btnExportExcel = MakeButton("تصدير Excel", Color.FromArgb(60, 120, 60), 335);
        _btnExportExcel.Click += async (_, _) => await OnExportAsync("excel");

        toolbar.Controls.AddRange(new Control[]
        {
            _btnRefresh, _btnColumns, _btnExportPdf, _btnExportExcel
        });

        var filterPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 95,
            BackColor = Color.FromArgb(245, 248, 252),
            Padding = new Padding(10, 8, 10, 8)
        };

        const int row1Y = 8;
        const int row2Y = 50;

        var lblSearch = new Label { Text = "بحث:", AutoSize = true, Location = new Point(filterPanel.Width - 65, row1Y + 5), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _txtSearch = new TextBox { Width = 200, Location = new Point(filterPanel.Width - 270, row1Y), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _txtSearch.TextChanged += (_, _) => ApplyFilter();

        var lblStatus = new Label { Text = "الحالة:", AutoSize = true, Location = new Point(filterPanel.Width - 355, row1Y + 5), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _cmbStatus = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 160,
            Location = new Point(filterPanel.Width - 520, row1Y),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _cmbStatus.Items.AddRange(new object[] { "الكل", "Draft", "Archived" });
        _cmbStatus.SelectedIndex = 0;
        _cmbStatus.SelectedIndexChanged += (_, _) => ApplyFilter();

        var lblSupplier = new Label { Text = "المورد:", AutoSize = true, Location = new Point(filterPanel.Width - 620, row1Y + 5), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _cmbSupplier = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 240,
            Location = new Point(filterPanel.Width - 865, row1Y),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            DisplayMember = "Name",
            ValueMember = "Id"
        };
        _cmbSupplier.SelectedIndexChanged += (_, _) => ApplyFilter();

        _chkUseDates = new CheckBox
        {
            Text = "تقييد بنطاق تاريخ",
            AutoSize = true,
            Location = new Point(filterPanel.Width - 175, row2Y + 5),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Checked = false
        };
        _chkUseDates.CheckedChanged += (_, _) =>
        {
            _dtFrom.Enabled = _chkUseDates.Checked;
            _dtTo.Enabled = _chkUseDates.Checked;
            ApplyFilter();
        };

        var lblFrom = new Label { Text = "من:", AutoSize = true, Location = new Point(filterPanel.Width - 260, row2Y + 5), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _dtFrom = new DateTimePicker
        {
            Width = 130,
            Location = new Point(filterPanel.Width - 390, row2Y),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today.AddMonths(-1),
            Enabled = false
        };
        _dtFrom.ValueChanged += (_, _) => ApplyFilter();

        var lblTo = new Label { Text = "إلى:", AutoSize = true, Location = new Point(filterPanel.Width - 470, row2Y + 5), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _dtTo = new DateTimePicker
        {
            Width = 130,
            Location = new Point(filterPanel.Width - 600, row2Y),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today,
            Enabled = false
        };
        _dtTo.ValueChanged += (_, _) => ApplyFilter();

        filterPanel.Controls.AddRange(new Control[]
        {
            lblSearch, _txtSearch,
            lblStatus, _cmbStatus,
            lblSupplier, _cmbSupplier,
            _chkUseDates, lblFrom, _dtFrom, lblTo, _dtTo
        });

        filterPanel.Resize += (_, _) =>
        {
            _txtSearch.Location = new Point(filterPanel.Width - 270, row1Y);
            lblSearch.Location = new Point(filterPanel.Width - 65, row1Y + 5);

            _cmbStatus.Location = new Point(filterPanel.Width - 520, row1Y);
            lblStatus.Location = new Point(filterPanel.Width - 355, row1Y + 5);

            _cmbSupplier.Location = new Point(filterPanel.Width - 865, row1Y);
            lblSupplier.Location = new Point(filterPanel.Width - 620, row1Y + 5);

            _chkUseDates.Location = new Point(filterPanel.Width - 175, row2Y + 5);
            lblFrom.Location = new Point(filterPanel.Width - 260, row2Y + 5);
            _dtFrom.Location = new Point(filterPanel.Width - 390, row2Y);
            lblTo.Location = new Point(filterPanel.Width - 470, row2Y + 5);
            _dtTo.Location = new Point(filterPanel.Width - 600, row2Y);
        };

        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AllowUserToResizeRows = false,
            AllowUserToResizeColumns = true,
            ReadOnly = true,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false,
            RowHeadersVisible = false,
            BackgroundColor = Color.White,
            BorderStyle = BorderStyle.FixedSingle,
            RightToLeft = RightToLeft.Yes,
            Font = new Font("Segoe UI", 10F),
            ColumnHeadersHeight = 36,
            EnableHeadersVisualStyles = false,
            ScrollBars = ScrollBars.Both   // بند 4: تمرير أفقي ورأسي
        };
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 60, 120);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 220, 250);
        _grid.DefaultCellStyle.SelectionForeColor = Color.Black;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 250);
        _grid.RowTemplate.Height = 30;
        _grid.CellDoubleClick += (_, _) => OnGridDoubleClick();

        AddGridColumn("InvoiceNo", "رقم الفاتورة", 150);
        AddGridColumn("SupplierInvoiceNo", "رقم فاتورة المورد", 140);
        AddGridColumn("InvoiceDate", "التاريخ", 110, "yyyy-MM-dd", DataGridViewContentAlignment.MiddleCenter);
        AddGridColumn("SupplierName", "المورد", 0, null, DataGridViewContentAlignment.MiddleRight, fill: true);
        AddGridColumn("Status", "الحالة", 90, null, DataGridViewContentAlignment.MiddleCenter);
        AddGridColumn("TotalQty", "الكمية", 90, "N0", DataGridViewContentAlignment.MiddleCenter);
        AddGridColumn("TotalPayments", "المدفوعات", 110, "N2", DataGridViewContentAlignment.MiddleCenter);
        AddGridColumn("NetCost", "صافي التكلفة", 110, "N2", DataGridViewContentAlignment.MiddleCenter);
        AddGridColumn("TotalUsd", "القيمة $", 100, "N2", DataGridViewContentAlignment.MiddleCenter);
        AddGridColumn("TotalProfit", "الربح", 110, "N2", DataGridViewContentAlignment.MiddleCenter);
        AddGridColumn("ProfitMargin", "الهامش %", 90, "N4", DataGridViewContentAlignment.MiddleCenter);
        AddGridColumn("ArchivedAt", "تاريخ الأرشفة", 140, "yyyy-MM-dd HH:mm", DataGridViewContentAlignment.MiddleCenter);

        _lblStatus = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 10, 0),
            ForeColor = Color.DimGray,
            Font = new Font("Segoe UI", 9F)
        };

        Controls.Add(_grid);
        Controls.Add(_lblStatus);
        Controls.Add(filterPanel);
        Controls.Add(toolbar);
    }

    private void AddGridColumn(
        string name,
        string header,
        int width,
        string? format = null,
        DataGridViewContentAlignment align = DataGridViewContentAlignment.MiddleRight,
        bool fill = false)
    {
        var col = new DataGridViewTextBoxColumn
        {
            Name = name,
            DataPropertyName = name,
            HeaderText = header,
            Width = width,
            AutoSizeMode = fill
                ? DataGridViewAutoSizeColumnMode.Fill
                : DataGridViewAutoSizeColumnMode.None
        };

        var style = new DataGridViewCellStyle { Alignment = align };
        if (!string.IsNullOrEmpty(format)) style.Format = format;
        col.DefaultCellStyle = style;

        _grid.Columns.Add(col);
    }

    private static Button MakeButton(string text, Color back, int x)
    {
        var b = new Button
        {
            Text = text,
            Width = 105,
            Height = 34,
            Location = new Point(x, 10),
            BackColor = back,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10F)
        };
        b.FlatAppearance.BorderSize = 0;
        return b;
    }

    // ============ Column Widths (بند 12) ============

    private void ApplyColumnWidths()
    {
        var prefs = PreferencesService.Instance;
        _suppressWidthSave = true;
        try
        {
            foreach (DataGridViewColumn col in _grid.Columns)
            {
                if (col.AutoSizeMode == DataGridViewAutoSizeColumnMode.Fill) continue;
                var w = prefs.GetColumnWidth(GridKey, col.Name);
                if (w.HasValue && w.Value > 0) col.Width = w.Value;
            }
        }
        finally
        {
            _suppressWidthSave = false;
        }
    }

    private void HookColumnWidthTracking()
    {
        _grid.ColumnWidthChanged += (_, e) =>
        {
            if (_suppressWidthSave) return;
            if (e.Column == null) return;
            if (e.Column.AutoSizeMode == DataGridViewAutoSizeColumnMode.Fill) return;
            if (e.Column.Width <= 0) return;

            PreferencesService.Instance.SetColumnWidth(GridKey, e.Column.Name, e.Column.Width);
        };
    }

    // ============================================================
    // Data Loading
    // ============================================================

    private async Task LoadAllAsync()
    {
        SetStatus("جارٍ التحميل...", Color.DimGray);
        try
        {
            _suppliers = await ApiClient.Instance.GetAsync<List<SupplierItem>>("suppliers") ?? new();
            var supItems = new List<SupplierItem> { new() { Id = 0, Name = "الكل" } };
            supItems.AddRange(_suppliers);
            _cmbSupplier.DataSource = null;
            _cmbSupplier.DataSource = supItems;
            _cmbSupplier.DisplayMember = "Name";
            _cmbSupplier.ValueMember = "Id";
            _cmbSupplier.SelectedIndex = 0;

            _all = await ApiClient.Instance.GetAsync<List<InvoiceListRow>>("invoices") ?? new();
            ApplyFilter();
        }
        catch (Exception ex)
        {
            SetStatus("خطأ: " + ex.Message, Color.Firebrick);
        }
    }

    private void ApplyFilter()
    {
        var q = _txtSearch.Text.Trim();
        var st = _cmbStatus.SelectedItem as string ?? "الكل";
        var supId = (_cmbSupplier.SelectedItem as SupplierItem)?.Id ?? 0;
        bool useDates = _chkUseDates.Checked;
        var from = DateOnly.FromDateTime(_dtFrom.Value);
        var to = DateOnly.FromDateTime(_dtTo.Value);

        IEnumerable<InvoiceListRow> result = _all;

        if (st != "الكل")
            result = result.Where(i => i.Status == st);

        if (supId != 0)
            result = result.Where(i => i.SupplierId == supId);

        if (useDates)
            result = result.Where(i => i.InvoiceDate >= from && i.InvoiceDate <= to);

        if (!string.IsNullOrWhiteSpace(q))
        {
            result = result.Where(i =>
                (i.InvoiceNo?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (i.SupplierInvoiceNo?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (i.SupplierName?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        _grid.DataSource = result.ToList();
        SetStatus($"عدد الفواتير: {_grid.RowCount} / {_all.Count}", Color.SeaGreen);
    }

    // ============================================================
    // Actions
    // ============================================================

    private void OnChooseColumns()
    {
        ColumnsDialog.ShowFor(_grid, GridKey, this);
    }

    private void OnGridDoubleClick()
    {
        if (_grid.CurrentRow?.DataBoundItem is not InvoiceListRow row) return;
        InvoiceOpenRequested?.Invoke(row.Id);
    }

    private async Task OnExportAsync(string format)
    {
        if (_grid.CurrentRow?.DataBoundItem is not InvoiceListRow row)
        {
            MessageBox.Show("الرجاء اختيار فاتورة أولاً.", "تنبيه",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!ApiClient.Instance.HasPermission("exports.read"))
        {
            MessageBox.Show("لا تملك صلاحية التصدير.", "مرفوض",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var exportHidden = ExportOptionsDialog.ShowFor(this, null);
        if (exportHidden == null) return;

        try
        {
            SetStatus("جارٍ توليد الملف...", Color.DimGray);

            var endpoint = $"exports/invoice/{row.Id}/{format}";
            var bytes = await ApiClient.Instance.DownloadAsync(endpoint, exportHidden);

            if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            {
                var suggestedName = $"{row.InvoiceNo}.pdf";
                var saved = PdfPreviewForm.ShowPreview(this, bytes, suggestedName);

                if (saved)
                {
                    SetStatus("تم حفظ ملف PDF.", Color.SeaGreen);
                }
                else
                {
                    SetStatus("تم إلغاء الحفظ (لم تُحفظ النسخة).", Color.DimGray);
                }
                return;
            }

            using var sfd = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"{row.InvoiceNo}.xlsx"
            };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;

            await File.WriteAllBytesAsync(sfd.FileName, bytes);
            SetStatus($"تم التصدير إلى: {sfd.FileName}", Color.SeaGreen);

            var open = MessageBox.Show("تم التصدير بنجاح. فتح الملف؟", "نجاح",
                MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (open == DialogResult.Yes)
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = sfd.FileName,
                    UseShellExecute = true
                });
        }
        catch (Exception ex)
        {
            SetStatus("فشل التصدير: " + ex.Message, Color.Firebrick);
            MessageBox.Show("فشل التصدير: " + ex.Message, "خطأ",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SetStatus(string text, Color color)
    {
        _lblStatus.Text = text;
        _lblStatus.ForeColor = color;
    }

    // ============================================================
    // Models
    // ============================================================

    public class SupplierItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}