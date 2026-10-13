using System.Text.Json;
using Invoice.Desktop.Models;
using Invoice.Desktop.Services;

namespace Invoice.Desktop.UserControls;

/// <summary>
/// لوحة التحكم — تعرض KPIs من /api/reports/dashboard + جدول الفواتير غير المؤرشفة (Draft).
/// النقر المزدوج على فاتورة في الجدول يُطلِق حدث InvoiceOpenRequested.
/// </summary>
public class DashboardUserControl : UserControl
{
    private const string GridKey = "DashboardDrafts";

    private ComboBox _cmbPeriod = null!;
    private Button _btnRefresh = null!;
    private Label _lblStatus = null!;
    private FlowLayoutPanel _cardsPanel = null!;

    // ============ قائمة المسودات ============
    private Panel _draftPanel = null!;
    private DataGridView _draftGrid = null!;
    private Label _lblDraftTitle = null!;
    private Label _lblDraftCount = null!;

    private List<InvoiceListRow> _drafts = new();

    private bool _suppressWidthSave = false;

    /// <summary>يُطلَق عند النقر المزدوج على مسودة — يمرر معرّف الفاتورة.</summary>
    public event Action<long>? InvoiceOpenRequested;

    public DashboardUserControl()
    {
        BuildUi();
        ApplyColumnWidths();
        HookColumnWidthTracking();
        Load += async (_, _) => await LoadDataAsync();
    }

    private void BuildUi()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.White;
        RightToLeft = RightToLeft.Yes;
        Font = new Font("Segoe UI", 10F);
        Padding = new Padding(15);

        // ============ شريط الأدوات ============
        var toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Color.WhiteSmoke,
            Padding = new Padding(10)
        };

        var lblPeriod = new Label
        {
            Text = "الفترة:",
            AutoSize = true,
            Location = new Point(toolbar.Width - 80, 15),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        _cmbPeriod = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 140,
            Location = new Point(toolbar.Width - 230, 12),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        _cmbPeriod.Items.AddRange(new object[]
        {
            "اليوم", "هذا الأسبوع", "هذا الشهر", "هذا الربع", "هذه السنة", "الكل"
        });
        _cmbPeriod.SelectedIndex = 4;
        _cmbPeriod.SelectedIndexChanged += async (_, _) => await LoadDataAsync();

        _btnRefresh = new Button
        {
            Text = "تحديث",
            Width = 100,
            Height = 32,
            Location = new Point(10, 10),
            BackColor = Color.FromArgb(30, 100, 200),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnRefresh.FlatAppearance.BorderSize = 0;
        _btnRefresh.Click += async (_, _) => await LoadDataAsync();

        toolbar.Controls.AddRange(new Control[] { _btnRefresh, _cmbPeriod, lblPeriod });

        // ============ شريط الحالة ============
        _lblStatus = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 30,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 10, 0),
            ForeColor = Color.DimGray,
            Font = new Font("Segoe UI", 9F)
        };

        // ============ منطقة البطاقات ============
        _cardsPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = true,
            Padding = new Padding(0, 10, 0, 10),
            BackColor = Color.White
        };

        // ============ منطقة المسودات ============
        _draftPanel = BuildDraftPanel();

        // ============ التركيب ============
        Controls.Add(_cardsPanel);
        Controls.Add(_draftPanel);
        Controls.Add(_lblStatus);
        Controls.Add(toolbar);
    }

    private Panel BuildDraftPanel()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 240,
            BackColor = Color.White,
            Padding = new Padding(0, 8, 0, 0)
        };

        // ============ رأس القسم ============
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 32,
            BackColor = Color.FromArgb(230, 240, 250)
        };

        _lblDraftTitle = new Label
        {
            Text = "الفواتير غير المؤرشفة (مسودات)",
            Dock = DockStyle.Right,
            Width = 300,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 12, 0),
            Font = new Font("Segoe UI", 10.5F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 60, 120)
        };

        _lblDraftCount = new Label
        {
            Text = "",
            Dock = DockStyle.Left,
            Width = 200,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            Font = new Font("Segoe UI", 9F),
            ForeColor = Color.DimGray
        };

        header.Controls.Add(_lblDraftTitle);
        header.Controls.Add(_lblDraftCount);

        // ============ الجدول ============
        _draftGrid = new DataGridView
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
            Font = new Font("Segoe UI", 9.5F),
            ColumnHeadersHeight = 32,
            EnableHeadersVisualStyles = false,
            ScrollBars = ScrollBars.Both   // بند 4: تمرير أفقي ورأسي
        };
        _draftGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 60, 120);
        _draftGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _draftGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9.5F, FontStyle.Bold);
        _draftGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _draftGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 220, 250);
        _draftGrid.DefaultCellStyle.SelectionForeColor = Color.Black;
        _draftGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 250);
        _draftGrid.RowTemplate.Height = 28;
        _draftGrid.CellDoubleClick += (_, _) => OnDraftDoubleClick();

        _draftGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "InvoiceNo", DataPropertyName = "InvoiceNo", HeaderText = "رقم الفاتورة", Width = 150 });
        _draftGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "SupplierInvoiceNo", DataPropertyName = "SupplierInvoiceNo", HeaderText = "رقم فاتورة المورد", Width = 150
        });
        _draftGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "InvoiceDate", DataPropertyName = "InvoiceDate", HeaderText = "التاريخ", Width = 110,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd", Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _draftGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "SupplierName", DataPropertyName = "SupplierName", HeaderText = "المورد",
            AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill
        });
        _draftGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "TotalQty", DataPropertyName = "TotalQty", HeaderText = "الكمية", Width = 90,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _draftGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "TotalUsd", DataPropertyName = "TotalUsd", HeaderText = "القيمة $", Width = 110,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _draftGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "TotalProfit", DataPropertyName = "TotalProfit", HeaderText = "الربح", Width = 110,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleCenter }
        });

        // ============ تلميح صغير ============
        var lblHint = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 20,
            Text = "انقر نقرًا مزدوجًا على أي مسودة لفتحها في شاشة الفاتورة.",
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 10, 0),
            Font = new Font("Segoe UI", 8.5F),
            ForeColor = Color.Gray
        };

        panel.Controls.Add(_draftGrid);
        panel.Controls.Add(lblHint);
        panel.Controls.Add(header);

        return panel;
    }

    // ============ Column Widths (بند 12) ============

    private void ApplyColumnWidths()
    {
        var prefs = PreferencesService.Instance;
        _suppressWidthSave = true;
        try
        {
            foreach (DataGridViewColumn col in _draftGrid.Columns)
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
        _draftGrid.ColumnWidthChanged += (_, e) =>
        {
            if (_suppressWidthSave) return;
            if (e.Column == null) return;
            if (e.Column.AutoSizeMode == DataGridViewAutoSizeColumnMode.Fill) return;
            if (e.Column.Width <= 0) return;

            PreferencesService.Instance.SetColumnWidth(GridKey, e.Column.Name, e.Column.Width);
        };
    }

    private void OnDraftDoubleClick()
    {
        if (_draftGrid.CurrentRow?.DataBoundItem is not InvoiceListRow row) return;
        InvoiceOpenRequested?.Invoke(row.Id);
    }

    private async Task LoadDataAsync()
    {
        _cardsPanel.Controls.Clear();
        _lblStatus.Text = "جارٍ التحميل...";
        _lblStatus.ForeColor = Color.DimGray;
        _lblDraftCount.Text = "";

        try
        {
            var period = MapPeriod(_cmbPeriod.SelectedIndex);
            var data = await ApiClient.Instance
                .GetAsync<JsonElement>($"reports/dashboard?period={period}");

            AddSectionTitle("الفواتير");
            AddCard("إجمالي الفواتير المؤرشفة", GetInt(data, "invoices", "total"));
            AddCard("مسودات (غير مؤرشفة)", GetInt(data, "invoices", "drafts"));

            AddSectionTitle("المبالغ المالية");
            AddCard("إجمالي المدفوعات", GetDecimal(data, "amounts", "totalPayments"));
            AddCard("إجمالي الدولار", GetDecimal(data, "amounts", "totalUsd"));
            AddCard("إجمالي المبيعات", GetDecimal(data, "amounts", "totalSales"));
            AddCard("إجمالي الأرباح", GetDecimal(data, "amounts", "totalProfit"), highlight: true);

            AddSectionTitle("الفهرس");
            AddCard("عدد الموردين", GetInt(data, "catalog", "suppliers"));
            AddCard("عدد الأصناف", GetInt(data, "catalog", "products"));

            await LoadDraftsAsync();

            _lblStatus.Text = $"آخر تحديث: {DateTime.Now:HH:mm:ss}";
            _lblStatus.ForeColor = Color.SeaGreen;
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "خطأ: " + ex.Message;
            _lblStatus.ForeColor = Color.Firebrick;
        }
    }

    private async Task LoadDraftsAsync()
    {
        try
        {
            var drafts = await ApiClient.Instance
                .GetAsync<List<InvoiceListRow>>("invoices?status=Draft") ?? new();

            _drafts = drafts;
            _draftGrid.DataSource = null;
            _draftGrid.DataSource = _drafts.ToList();
            _lblDraftCount.Text = $"عدد المسودات: {_drafts.Count}";
        }
        catch (Exception ex)
        {
            _draftGrid.DataSource = null;
            _lblDraftCount.Text = "تعذّر تحميل المسودات: " + ex.Message;
            _lblDraftCount.ForeColor = Color.Firebrick;
        }
    }

    private void AddSectionTitle(string title)
    {
        var lbl = new Label
        {
            Text = title,
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 60, 120),
            AutoSize = false,
            Width = _cardsPanel.ClientSize.Width - 25,
            Height = 30,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 10, 10, 0),
            Margin = new Padding(0)
        };
        _cardsPanel.Controls.Add(lbl);
    }

    private void AddCard(string label, object value, bool highlight = false)
    {
        var card = new Panel
        {
            Width = 220,
            Height = 100,
            BackColor = highlight ? Color.FromArgb(46, 160, 67) : Color.FromArgb(30, 100, 200),
            Margin = new Padding(8),
            Padding = new Padding(10)
        };

        var lblLabel = new Label
        {
            Text = label,
            Dock = DockStyle.Top,
            Height = 30,
            ForeColor = Color.WhiteSmoke,
            Font = new Font("Segoe UI", 9F),
            TextAlign = ContentAlignment.MiddleRight
        };

        var lblValue = new Label
        {
            Text = FormatValue(value),
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 20F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        card.Controls.Add(lblValue);
        card.Controls.Add(lblLabel);
        _cardsPanel.Controls.Add(card);
    }

    private static string FormatValue(object v)
    {
        if (v is decimal d)
            return d.ToString("N2");
        if (v is int i)
            return i.ToString("N0");
        if (v is long l)
            return l.ToString("N0");
        return v?.ToString() ?? "—";
    }

    private static string MapPeriod(int index) => index switch
    {
        0 => "today",
        1 => "week",
        2 => "month",
        3 => "quarter",
        4 => "year",
        _ => "all"
    };

    private static decimal GetDecimal(JsonElement root, string section, string prop)
    {
        if (root.TryGetProperty(section, out var s) &&
            s.TryGetProperty(prop, out var p) &&
            p.TryGetDecimal(out var val))
            return val;
        return 0m;
    }

    private static int GetInt(JsonElement root, string section, string prop)
    {
        if (root.TryGetProperty(section, out var s) &&
            s.TryGetProperty(prop, out var p) &&
            p.TryGetInt32(out var val))
            return val;
        return 0;
    }
}