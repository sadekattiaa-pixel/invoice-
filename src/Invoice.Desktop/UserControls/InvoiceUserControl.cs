using System.Text.Json;
using Invoice.Desktop.Models;
using Invoice.Desktop.Services;
using Invoice.Desktop.Shared;

namespace Invoice.Desktop.UserControls;

/// <summary>
/// شاشة الفاتورة — إنشاء/تحرير/حفظ/أرشفة فاتورة + حساب معاينة + تصدير PDF/Excel.
/// الشاشة مُقسَّمة إلى 3 تبويبات:
///   1) بيانات المورد والملاحظات
///   2) الأصناف
///   3) المدفوعات والخصومات والمصاريف
/// </summary>
public class InvoiceUserControl : UserControl
{
    // ============ Header ============
    private ComboBox _cmbSupplier = null!;
    private TextBox _txtInvoiceNo = null!;
    private DateTimePicker _dtInvoiceDate = null!;
    private TextBox _txtSupplierInvoiceNo = null!;

    // ============ Panels ============
    private InvoiceItemsPanel _itemsPanel = null!;
    private InvoiceAmountsPanel _paymentsPanel = null!;
    private InvoiceAmountsPanel _discountsPanel = null!;
    private InvoiceAmountsPanel _expensesPanel = null!;

    // ============ Notes ============
    private TextBox _txtNotes = null!;
    private TextBox _txtNotes2 = null!;

    // ============ Summary ============
    private Label _lblTotalPayments = null!;
    private Label _lblTotalDiscounts = null!;
    private Label _lblNetPayments = null!;
    private Label _lblTotalExpenses = null!;
    private Label _lblNetCost = null!;
    private Label _lblTotalUsd = null!;
    private Label _lblExchangeRate = null!;
    private Label _lblTotalSales = null!;
    private Label _lblTotalProfit = null!;
    private Label _lblProfitMargin = null!;

    // ============ Toolbar ============
    private Button _btnNew = null!;
    private Button _btnLoad = null!;
    private Button _btnSave = null!;
    private Button _btnArchive = null!;
    private Button _btnCalculate = null!;
    private Button _btnExportPdf = null!;
    private Button _btnExportExcel = null!;
    private Button _btnColumns = null!;
    private Label _lblMode = null!;
    private Label _lblStatus = null!;

    // ============ State ============
    private long? _currentInvoiceId = null;
    private string _currentStatus = "Draft";
    private List<SupplierItem> _suppliers = new();
    private List<ProductPickerDialog.ProductItem> _products = new();
    private InvoiceCalculationResult? _lastCalculation;

    // ============ Programmatic open ============
    private long? _pendingOpenInvoiceId = null;
    private bool _lookupsLoaded = false;

    private const string GridKey = "InvoiceItems";

    public InvoiceUserControl()
    {
        BuildUi();
        Load += async (_, _) => await LoadLookupsAsync();
    }

    /// <summary>
    /// طلب فتح فاتورة برمجيًا (يُستخدم من صفحة الأرشيف أو لوحة التحكم).
    /// </summary>
    public void RequestOpenInvoice(long id)
    {
        if (_lookupsLoaded)
        {
            _ = LoadInvoiceByIdAsync(id);
        }
        else
        {
            _pendingOpenInvoiceId = id;
        }
    }

    // ============================================================
    // UI Construction
    // ============================================================

    private void BuildUi()
    {
        Dock = DockStyle.Fill;
        BackColor = Color.White;
        RightToLeft = RightToLeft.Yes;
        Font = new Font("Segoe UI", 10F);
        Padding = new Padding(8);

        var toolbar = BuildToolbar();
        var tabs = BuildTabs();
        var summary = BuildSummary();

        _lblStatus = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 26,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 10, 0),
            ForeColor = Color.DimGray,
            Font = new Font("Segoe UI", 9F)
        };

        Controls.Add(tabs);
        Controls.Add(summary);
        Controls.Add(_lblStatus);
        Controls.Add(toolbar);
    }

    private Panel BuildToolbar()
    {
        var tb = new Panel
        {
            Dock = DockStyle.Top,
            Height = 58,
            BackColor = Color.WhiteSmoke,
            Padding = new Padding(8)
        };

        _btnNew = MakeButton("جديدة", Color.FromArgb(46, 160, 67), 8);
        _btnNew.Click += async (_, _) => await OnNewAsync();

        _btnLoad = MakeButton("فتح", Color.FromArgb(30, 100, 200), 118);
        _btnLoad.Click += async (_, _) => await OnLoadAsync();

        _btnSave = MakeButton("حفظ", Color.FromArgb(46, 160, 67), 228);
        _btnSave.Click += async (_, _) => await OnSaveAsync();

        _btnArchive = MakeButton("أرشفة", Color.FromArgb(180, 80, 180), 338);
        _btnArchive.Click += async (_, _) => await OnArchiveAsync();

        _btnCalculate = MakeButton("حساب", Color.FromArgb(230, 160, 30), 448);
        _btnCalculate.Click += async (_, _) => await OnCalculateAsync();

        _btnExportPdf = MakeButton("PDF", Color.FromArgb(200, 60, 60), 558);
        _btnExportPdf.Click += async (_, _) => await OnExportAsync("pdf");

        _btnExportExcel = MakeButton("Excel", Color.FromArgb(60, 120, 60), 668);
        _btnExportExcel.Click += async (_, _) => await OnExportAsync("excel");

        _btnColumns = MakeButton("الأعمدة", Color.FromArgb(90, 90, 120), 778);
        _btnColumns.Click += (_, _) => OnChooseColumns();

        _lblMode = new Label
        {
            Text = "وضع: جديدة",
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 60, 120),
            AutoSize = true,
            Location = new Point(tb.Width - 300, 18),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        tb.Resize += (_, _) =>
        {
            _lblMode.Location = new Point(tb.Width - 300, 18);
        };

        tb.Controls.AddRange(new Control[]
        {
            _btnNew, _btnLoad, _btnSave, _btnArchive,
            _btnCalculate, _btnExportPdf, _btnExportExcel, _btnColumns,
            _lblMode
        });

        return tb;
    }

    private TabControl BuildTabs()
    {
        var tabs = new TabControl
        {
            Dock = DockStyle.Fill,
            RightToLeft = RightToLeft.Yes,
            Font = new Font("Segoe UI", 10F),
            Padding = new Point(22, 8)
        };

        var tabSupplier = new TabPage("بيانات المورد والملاحظات")
        {
            BackColor = Color.White,
            Padding = new Padding(0)
        };
        tabSupplier.Controls.Add(BuildNotesTab());
        tabSupplier.Controls.Add(BuildHeader());

        var tabItems = new TabPage("الأصناف")
        {
            BackColor = Color.White,
            Padding = new Padding(6)
        };
        _itemsPanel = new InvoiceItemsPanel { Dock = DockStyle.Fill };
        _itemsPanel.Changed += () => _lastCalculation = null;
        tabItems.Controls.Add(_itemsPanel);

        var tabAmounts = new TabPage("المدفوعات والخصومات والمصاريف")
        {
            BackColor = Color.White,
            Padding = new Padding(6)
        };
        tabAmounts.Controls.Add(BuildAmountsTab());

        tabs.TabPages.Add(tabSupplier);
        tabs.TabPages.Add(tabItems);
        tabs.TabPages.Add(tabAmounts);

        return tabs;
    }

    private Control BuildAmountsTab()
    {
        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            BackColor = Color.White,
            Padding = new Padding(0)
        };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));

        var leftSide = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            BackColor = Color.White,
            Padding = new Padding(0)
        };
        leftSide.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        leftSide.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

        _paymentsPanel = new InvoiceAmountsPanel { Dock = DockStyle.Fill, Title = "المدفوعات" };
        _paymentsPanel.Changed += () => _lastCalculation = null;

        _discountsPanel = new InvoiceAmountsPanel { Dock = DockStyle.Fill, Title = "الخصومات" };
        _discountsPanel.Changed += () => _lastCalculation = null;

        leftSide.Controls.Add(_paymentsPanel, 0, 0);
        leftSide.Controls.Add(_discountsPanel, 0, 1);

        _expensesPanel = new InvoiceAmountsPanel { Dock = DockStyle.Fill, Title = "المصاريف" };
        _expensesPanel.Changed += () => _lastCalculation = null;

        layout.Controls.Add(leftSide, 0, 0);
        layout.Controls.Add(_expensesPanel, 1, 0);

        return layout;
    }

    private void OnChooseColumns()
    {
        var grid = FindGrid(_itemsPanel);
        if (grid == null)
        {
            MessageBox.Show("لم يتم العثور على جدول البنود.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        ColumnsDialog.ShowFor(grid, GridKey, this);
    }

    private static DataGridView? FindGrid(Control c)
    {
        if (c is DataGridView g) return g;
        foreach (Control child in c.Controls)
        {
            var result = FindGrid(child);
            if (result != null) return result;
        }
        return null;
    }

    private Panel BuildHeader()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 100,
            BackColor = Color.FromArgb(245, 248, 252),
            Padding = new Padding(10, 8, 10, 8)
        };

        const int row1Y = 8;
        const int row2Y = 50;

        var lblSup = new Label { Text = "المورد:", Location = new Point(panel.Width - 80, row1Y + 5), AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _cmbSupplier = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Width = 260,
            Location = new Point(panel.Width - 350, row1Y),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            DisplayMember = "Name",
            ValueMember = "Id"
        };

        var lblNo = new Label { Text = "رقم الفاتورة:", Location = new Point(panel.Width - 470, row1Y + 5), AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _txtInvoiceNo = new TextBox
        {
            Width = 150,
            Location = new Point(panel.Width - 620, row1Y),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            ReadOnly = true,
            BackColor = Color.WhiteSmoke
        };

        var lblDate = new Label { Text = "التاريخ:", Location = new Point(panel.Width - 720, row1Y + 5), AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _dtInvoiceDate = new DateTimePicker
        {
            Width = 130,
            Location = new Point(panel.Width - 800, row1Y),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Format = DateTimePickerFormat.Short,
            Value = DateTime.Today
        };

        var lblSupNo = new Label { Text = "رقم فاتورة المورد:", Location = new Point(panel.Width - 170, row2Y + 5), AutoSize = true, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _txtSupplierInvoiceNo = new TextBox
        {
            Width = 250,
            Location = new Point(panel.Width - 430, row2Y),
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        panel.Controls.AddRange(new Control[]
        {
            lblSup, _cmbSupplier,
            lblNo, _txtInvoiceNo,
            lblDate, _dtInvoiceDate,
            lblSupNo, _txtSupplierInvoiceNo
        });

        panel.Resize += (_, _) =>
        {
            lblSup.Location = new Point(panel.Width - 80, row1Y + 5);
            _cmbSupplier.Location = new Point(panel.Width - 350, row1Y);
            lblNo.Location = new Point(panel.Width - 470, row1Y + 5);
            _txtInvoiceNo.Location = new Point(panel.Width - 620, row1Y);
            lblDate.Location = new Point(panel.Width - 720, row1Y + 5);
            _dtInvoiceDate.Location = new Point(panel.Width - 800, row1Y);
            lblSupNo.Location = new Point(panel.Width - 170, row2Y + 5);
            _txtSupplierInvoiceNo.Location = new Point(panel.Width - 430, row2Y);
        };

        return panel;
    }

    private Control BuildNotesTab()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.WhiteSmoke,
            Padding = new Padding(12)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 4,
            BackColor = Color.WhiteSmoke
        };
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 50f));

        var lblN1 = new Label
        {
            Text = "ملاحظات 1:",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 10, 0),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 60, 120)
        };
        _txtNotes = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            AcceptsReturn = true,
            Font = new Font("Segoe UI", 10F),
            BackColor = Color.White
        };

        var lblN2 = new Label
        {
            Text = "ملاحظات 2:",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 10, 0),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 60, 120)
        };
        _txtNotes2 = new TextBox
        {
            Dock = DockStyle.Fill,
            Multiline = true,
            ScrollBars = ScrollBars.Vertical,
            AcceptsReturn = true,
            Font = new Font("Segoe UI", 10F),
            BackColor = Color.White
        };

        layout.Controls.Add(lblN1, 0, 0);
        layout.Controls.Add(_txtNotes, 0, 1);
        layout.Controls.Add(lblN2, 0, 2);
        layout.Controls.Add(_txtNotes2, 0, 3);

        panel.Controls.Add(layout);
        return panel;
    }

    private Panel BuildSummary()
    {
        var panel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 100,
            BackColor = Color.FromArgb(245, 248, 252),
            Padding = new Padding(8),
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblTitle = new Label
        {
            Text = "ملخص الحساب",
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 60, 120),
            Dock = DockStyle.Right,
            Width = 120,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 10, 0)
        };

        var cards = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            AutoScroll = true,
            BackColor = Color.Transparent,
            Padding = new Padding(0)
        };

        _lblTotalPayments = MakeSummaryCard(cards, "إجمالي المدفوعات");
        _lblTotalDiscounts = MakeSummaryCard(cards, "إجمالي الخصومات");
        _lblNetPayments = MakeSummaryCard(cards, "صافي المدفوعات");
        _lblTotalExpenses = MakeSummaryCard(cards, "إجمالي المصاريف");
        _lblNetCost = MakeSummaryCard(cards, "صافي التكلفة", highlight: true);
        _lblTotalUsd = MakeSummaryCard(cards, "القيمة $");
        _lblExchangeRate = MakeSummaryCard(cards, "سعر الصرف");
        _lblTotalSales = MakeSummaryCard(cards, "إجمالي البيع");
        _lblTotalProfit = MakeSummaryCard(cards, "إجمالي الربح", highlight: true);
        _lblProfitMargin = MakeSummaryCard(cards, "هامش الربح %");

        panel.Controls.Add(cards);
        panel.Controls.Add(lblTitle);
        return panel;
    }

    private static Label MakeSummaryCard(FlowLayoutPanel parent, string caption, bool highlight = false)
    {
        var card = new Panel
        {
            Width = 135,
            Height = 74,
            BackColor = highlight ? Color.FromArgb(46, 160, 67) : Color.FromArgb(30, 100, 200),
            Margin = new Padding(4),
            Padding = new Padding(4)
        };

        var lblCap = new Label
        {
            Text = caption,
            Dock = DockStyle.Top,
            Height = 22,
            ForeColor = Color.WhiteSmoke,
            Font = new Font("Segoe UI", 8.5F),
            TextAlign = ContentAlignment.MiddleCenter
        };

        var lblVal = new Label
        {
            Text = "—",
            Dock = DockStyle.Fill,
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 13F, FontStyle.Bold),
            TextAlign = ContentAlignment.MiddleCenter
        };

        card.Controls.Add(lblVal);
        card.Controls.Add(lblCap);
        parent.Controls.Add(card);
        return lblVal;
    }

    private static Button MakeButton(string text, Color back, int x)
    {
        var b = new Button
        {
            Text = text,
            Width = 100,
            Height = 40,
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

    // ============================================================
    // Data Loading
    // ============================================================

    private async Task LoadLookupsAsync()
    {
        SetStatus("جارٍ التحميل...", Color.DimGray);
        try
        {
            var sup = await ApiClient.Instance.GetAsync<List<SupplierItem>>("suppliers") ?? new();
            _suppliers = sup;
            _cmbSupplier.DataSource = sup.ToList();
            _cmbSupplier.DisplayMember = "Name";
            _cmbSupplier.ValueMember = "Id";
            if (sup.Count > 0) _cmbSupplier.SelectedIndex = 0;

            var prods = await ApiClient.Instance.GetAsync<List<ProductPickerDialog.ProductItem>>("products") ?? new();
            _products = prods;
            _itemsPanel.SetAvailableProducts(prods);

            _lookupsLoaded = true;

            if (_pendingOpenInvoiceId.HasValue)
            {
                var id = _pendingOpenInvoiceId.Value;
                _pendingOpenInvoiceId = null;
                await LoadInvoiceByIdAsync(id);
            }
            else
            {
                await OnNewAsync();
            }

            SetStatus("جاهز", Color.SeaGreen);
        }
        catch (Exception ex)
        {
            SetStatus("خطأ: " + ex.Message, Color.Firebrick);
        }
    }

    // ============================================================
    // Actions
    // ============================================================

    private async Task OnNewAsync()
    {
        _currentInvoiceId = null;
        _currentStatus = "Draft";
        _lastCalculation = null;

        _cmbSupplier.SelectedIndex = _suppliers.Count > 0 ? 0 : -1;
        _dtInvoiceDate.Value = DateTime.Today;
        _txtSupplierInvoiceNo.Text = "";
        _txtNotes.Text = "";
        _txtNotes2.Text = "";

        _itemsPanel.ClearAll();
        _paymentsPanel.ClearAll();
        _discountsPanel.ClearAll();
        _expensesPanel.ClearAll();

        ResetSummary();
        SetButtonsByStatus();
        _lblMode.Text = "وضع: جديدة";

        try
        {
            var resp = await ApiClient.Instance.GetAsync<NextNumberResponse>("invoices/next-number");
            _txtInvoiceNo.Text = resp?.InvoiceNo ?? "";
        }
        catch
        {
            _txtInvoiceNo.Text = "";
        }
    }

    private async Task OnLoadAsync()
    {
        if (ApiClient.Instance.CurrentUser == null) return;

        using var dlg = new InvoicePickerDialog(_currentInvoiceId);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;
        if (!dlg.SelectedId.HasValue) return;

        await LoadInvoiceByIdAsync(dlg.SelectedId.Value);
    }

    public async Task LoadInvoiceByIdAsync(long id)
    {
        try
        {
            var inv = await ApiClient.Instance.GetAsync<InvoiceDetail>($"invoices/{id}");
            if (inv == null)
            {
                MessageBox.Show("تعذر تحميل الفاتورة.", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
            LoadInvoiceIntoUi(inv);
        }
        catch (Exception ex)
        {
            MessageBox.Show("فشل التحميل: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void LoadInvoiceIntoUi(InvoiceDetail inv)
    {
        _currentInvoiceId = inv.Id;
        _currentStatus = inv.Status;
        _lastCalculation = null;

        var sup = _suppliers.FirstOrDefault(s => s.Id == inv.SupplierId);
        if (sup != null) _cmbSupplier.SelectedItem = sup;

        _txtInvoiceNo.Text = inv.InvoiceNo;
        _txtSupplierInvoiceNo.Text = inv.SupplierInvoiceNo ?? "";
        _dtInvoiceDate.Value = inv.InvoiceDate.ToDateTime(TimeOnly.MinValue);
        _txtNotes.Text = inv.Notes ?? "";
        _txtNotes2.Text = inv.Notes2 ?? "";

        _itemsPanel.LoadItems(inv.Items);
        _paymentsPanel.LoadItems(inv.Payments);
        _discountsPanel.LoadItems(inv.Discounts);
        _expensesPanel.LoadItems(inv.Expenses);

        _lblTotalPayments.Text = inv.TotalPayments.ToString("N2");
        _lblTotalDiscounts.Text = inv.TotalDiscounts.ToString("N2");
        _lblNetPayments.Text = inv.NetPayments.ToString("N2");
        _lblTotalExpenses.Text = inv.TotalExpenses.ToString("N2");
        _lblNetCost.Text = inv.NetCost.ToString("N2");
        _lblTotalUsd.Text = inv.TotalUsd.ToString("N2");
        _lblExchangeRate.Text = inv.ExchangeRate.ToString("N6");
        _lblTotalSales.Text = inv.TotalSales.ToString("N2");
        _lblTotalProfit.Text = inv.TotalProfit.ToString("N2");
        _lblProfitMargin.Text = inv.ProfitMargin.ToString("N4");

        _lblMode.Text = $"وضع: {(_currentStatus == "Archived" ? "مؤرشفة (قراءة فقط)" : "تعديل فاتورة")}";
        SetButtonsByStatus();
        SetStatus($"تم تحميل الفاتورة {inv.InvoiceNo}", Color.SeaGreen);
    }

    private void SetButtonsByStatus()
    {
        bool archived = _currentStatus == "Archived";
        _btnSave.Enabled = !archived;
        _btnArchive.Enabled = !archived && _currentInvoiceId.HasValue;
        _itemsPanel.Enabled = !archived;
        _paymentsPanel.Enabled = !archived;
        _discountsPanel.Enabled = !archived;
        _expensesPanel.Enabled = !archived;
        _txtSupplierInvoiceNo.ReadOnly = archived;
        _dtInvoiceDate.Enabled = !archived;
        _cmbSupplier.Enabled = !archived;
        _txtNotes.ReadOnly = archived;
        _txtNotes2.ReadOnly = archived;
    }

    private async Task OnCalculateAsync()
    {
        var req = BuildRequest();
        if (req == null) return;

        SetStatus("جارٍ الحساب...", Color.DimGray);
        try
        {
            var result = await ApiClient.Instance.PostAsync("invoices/calculate", req);
            var json = await result.Content.ReadAsStringAsync();
            var calc = JsonSerializer.Deserialize<InvoiceCalculationResult>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (calc == null)
            {
                SetStatus("استجابة حساب فارغة.", Color.Firebrick);
                return;
            }

            _lastCalculation = calc;
            ApplyCalculation(calc);
            SetStatus("تم الحساب.", Color.SeaGreen);
        }
        catch (Exception ex)
        {
            SetStatus("فشل الحساب: " + ex.Message, Color.Firebrick);
            MessageBox.Show("فشل الحساب: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplyCalculation(InvoiceCalculationResult c)
    {
        _lblTotalPayments.Text = c.TotalPayments.ToString("N2");
        _lblTotalDiscounts.Text = c.TotalDiscounts.ToString("N2");
        _lblNetPayments.Text = c.NetPayments.ToString("N2");
        _lblTotalExpenses.Text = c.TotalExpenses.ToString("N2");
        _lblNetCost.Text = c.NetCost.ToString("N2");
        _lblTotalUsd.Text = c.TotalUsd.ToString("N2");
        _lblExchangeRate.Text = c.ExchangeRate.ToString("N6");
        _lblTotalSales.Text = c.TotalSales.ToString("N2");
        _lblTotalProfit.Text = c.TotalProfit.ToString("N2");
        _lblProfitMargin.Text = c.ProfitMargin.ToString("N4");
    }

    private void ResetSummary()
    {
        _lblTotalPayments.Text = "—";
        _lblTotalDiscounts.Text = "—";
        _lblNetPayments.Text = "—";
        _lblTotalExpenses.Text = "—";
        _lblNetCost.Text = "—";
        _lblTotalUsd.Text = "—";
        _lblExchangeRate.Text = "—";
        _lblTotalSales.Text = "—";
        _lblTotalProfit.Text = "—";
        _lblProfitMargin.Text = "—";
    }

    private InvoiceSaveRequest? BuildRequest()
    {
        if (_cmbSupplier.SelectedItem is not SupplierItem sup)
        {
            MessageBox.Show("الرجاء اختيار مورد.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }
        if (_itemsPanel.Items.Count == 0)
        {
            MessageBox.Show("يجب إضافة بند واحد على الأقل.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return null;
        }

        return new InvoiceSaveRequest
        {
            SupplierId = sup.Id,
            SupplierInvoiceNo = string.IsNullOrWhiteSpace(_txtSupplierInvoiceNo.Text) ? null : _txtSupplierInvoiceNo.Text.Trim(),
            InvoiceDate = DateOnly.FromDateTime(_dtInvoiceDate.Value),
            Items = _itemsPanel.Items.Select(i => new InvoiceItemRequest
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity,
                PurchaseUsd = i.PurchaseUsd,
                SaleDzd = i.SaleDzd,
                Pallet = i.Pallet
            }).ToList(),
            Payments = _paymentsPanel.Items.Select(p => new InvoiceAmountRequest { Label = p.Label, Amount = p.Amount }).ToList(),
            Discounts = _discountsPanel.Items.Select(d => new InvoiceAmountRequest { Label = d.Label, Amount = d.Amount }).ToList(),
            Expenses = _expensesPanel.Items.Select(e => new InvoiceAmountRequest { Label = e.Label, Amount = e.Amount }).ToList(),
            Notes = string.IsNullOrWhiteSpace(_txtNotes.Text) ? null : _txtNotes.Text.Trim(),
            Notes2 = string.IsNullOrWhiteSpace(_txtNotes2.Text) ? null : _txtNotes2.Text.Trim()
        };
    }

    private async Task OnSaveAsync()
    {
        if (!ApiClient.Instance.HasPermission(_currentInvoiceId.HasValue ? "invoices.update" : "invoices.create"))
        {
            MessageBox.Show("لا تملك صلاحية حفظ الفواتير.", "مرفوض", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var req = BuildRequest();
        if (req == null) return;

        SetStatus("جارٍ الحفظ...", Color.DimGray);
        try
        {
            if (_currentInvoiceId.HasValue)
            {
                await ApiClient.Instance.PutAsync($"invoices/{_currentInvoiceId.Value}", req);
                SetStatus("تم تحديث الفاتورة.", Color.SeaGreen);
                MessageBox.Show("تم تحديث الفاتورة بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
                await LoadInvoiceByIdAsync(_currentInvoiceId.Value);
            }
            else
            {
                var resp = await ApiClient.Instance.PostAsync("invoices", req);
                var json = await resp.Content.ReadAsStringAsync();
                var created = JsonSerializer.Deserialize<InvoiceSaveResponse>(json,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (created == null)
                {
                    SetStatus("استجابة حفظ فارغة.", Color.Firebrick);
                    return;
                }

                _currentInvoiceId = created.Id;
                _currentStatus = created.Status;
                _txtInvoiceNo.Text = created.InvoiceNo;
                _lblMode.Text = "وضع: تعديل فاتورة";
                SetButtonsByStatus();
                SetStatus($"تم إنشاء الفاتورة {created.InvoiceNo}", Color.SeaGreen);
                MessageBox.Show($"تم حفظ الفاتورة {created.InvoiceNo} بنجاح.", "نجاح",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);

                await LoadInvoiceByIdAsync(created.Id);
            }
        }
        catch (Exception ex)
        {
            SetStatus("فشل الحفظ: " + ex.Message, Color.Firebrick);
            MessageBox.Show("فشل الحفظ: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task OnArchiveAsync()
    {
        if (!_currentInvoiceId.HasValue)
        {
            MessageBox.Show("احفظ الفاتورة أولًا قبل الأرشفة.", "تنبيه",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!ApiClient.Instance.HasPermission("invoices.archive"))
        {
            MessageBox.Show("لا تملك صلاحية أرشفة الفواتير.", "مرفوض", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            "بعد الأرشفة لا يمكن تعديل الفاتورة. متابعة؟",
            "تأكيد الأرشفة", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes) return;

        var req = BuildRequest();
        if (req == null) return;

        SetStatus("جارٍ الأرشفة...", Color.DimGray);
        try
        {
            await ApiClient.Instance.PostAsync($"invoices/{_currentInvoiceId.Value}/archive", req);
            SetStatus("تم أرشفة الفاتورة.", Color.SeaGreen);
            MessageBox.Show("تم أرشفة الفاتورة بنجاح.", "نجاح", MessageBoxButtons.OK, MessageBoxIcon.Information);
            await LoadInvoiceByIdAsync(_currentInvoiceId.Value);
        }
        catch (Exception ex)
        {
            SetStatus("فشل الأرشفة: " + ex.Message, Color.Firebrick);
            MessageBox.Show("فشل الأرشفة: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task OnExportAsync(string format)
    {
        if (!_currentInvoiceId.HasValue)
        {
            MessageBox.Show("احفظ الفاتورة أولاً.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!ApiClient.Instance.HasPermission("exports.read"))
        {
            MessageBox.Show("لا تملك صلاحية التصدير.", "مرفوض", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var hidden = ColumnsDialog.GetHiddenKeys(GridKey);
        var exportHidden = ExportOptionsDialog.ShowFor(this, hidden);
        if (exportHidden == null) return;

        var mergedHidden = hidden
            .Concat(exportHidden)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        try
        {
            SetStatus("جارٍ توليد الملف...", Color.DimGray);

            var endpoint = $"exports/invoice/{_currentInvoiceId.Value}/{format}";
            var bytes = await ApiClient.Instance.DownloadAsync(endpoint, mergedHidden);

            // ============ PDF: معاينة أولًا (بند 16) ============
            if (format.Equals("pdf", StringComparison.OrdinalIgnoreCase))
            {
                var suggestedName = $"{_txtInvoiceNo.Text}.pdf";
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

            // ============ Excel: حفظ مباشر ============
            using var sfd = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"{_txtInvoiceNo.Text}.xlsx"
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
            MessageBox.Show("فشل التصدير: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
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

    // ============================================================
    // Invoice Picker Dialog
    // ============================================================

    private class InvoicePickerDialog : Form
    {
        private DataGridView _grid = null!;
        private TextBox _txtSearch = null!;
        private ComboBox _cmbStatus = null!;
        private Label _lblStatus = null!;
        private readonly List<InvoiceListRow> _all = new();
        private readonly long? _currentId;

        public long? SelectedId { get; private set; }

        public InvoicePickerDialog(long? currentId)
        {
            _currentId = currentId;
            BuildUi();
            Load += async (_, _) => await LoadDataAsync();
        }

        private void BuildUi()
        {
            Text = "اختيار فاتورة";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.Sizable;
            MinimumSize = new Size(900, 500);
            ClientSize = new Size(1000, 600);
            RightToLeft = RightToLeft.Yes;
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.WhiteSmoke;

            var toolbar = new Panel { Dock = DockStyle.Top, Height = 55, BackColor = Color.WhiteSmoke, Padding = new Padding(10) };

            var lblSearch = new Label { Text = "بحث:", AutoSize = true, Location = new Point(toolbar.Width - 70, 20), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            _txtSearch = new TextBox { Width = 220, Location = new Point(toolbar.Width - 300, 17), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            _txtSearch.TextChanged += (_, _) => ApplyFilter();

            var lblSt = new Label { Text = "الحالة:", AutoSize = true, Location = new Point(toolbar.Width - 380, 20), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            _cmbStatus = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 120, Location = new Point(toolbar.Width - 520, 17), Anchor = AnchorStyles.Top | AnchorStyles.Right };
            _cmbStatus.Items.AddRange(new object[] { "الكل", "Draft", "Archived" });
            _cmbStatus.SelectedIndex = 0;
            _cmbStatus.SelectedIndexChanged += (_, _) => ApplyFilter();

            toolbar.Controls.AddRange(new Control[] { lblSearch, _txtSearch, lblSt, _cmbStatus });
            toolbar.Resize += (_, _) =>
            {
                _txtSearch.Location = new Point(toolbar.Width - 300, 17);
                lblSearch.Location = new Point(toolbar.Width - 70, 20);
                _cmbStatus.Location = new Point(toolbar.Width - 520, 17);
                lblSt.Location = new Point(toolbar.Width - 380, 20);
            };

            _grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                AutoGenerateColumns = false,
                AllowUserToAddRows = false,
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                RowHeadersVisible = false,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle,
                RightToLeft = RightToLeft.Yes,
                Font = new Font("Segoe UI", 10F),
                ColumnHeadersHeight = 34,
                EnableHeadersVisualStyles = false
            };
            _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 60, 120);
            _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
            _grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 220, 250);
            _grid.DefaultCellStyle.SelectionForeColor = Color.Black;
            _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 250);
            _grid.RowTemplate.Height = 28;
            _grid.CellDoubleClick += (_, _) => Confirm();

            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "InvoiceNo", HeaderText = "الرقم", Width = 150 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "InvoiceDate", HeaderText = "التاريخ", Width = 110,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "yyyy-MM-dd", Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SupplierName", HeaderText = "المورد", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
            _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Status", HeaderText = "الحالة", Width = 90 });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "TotalUsd", HeaderText = "$", Width = 100,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "TotalProfit", HeaderText = "الربح", Width = 110,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleCenter }
            });
            _grid.Columns.Add(new DataGridViewTextBoxColumn
            {
                DataPropertyName = "TotalQty", HeaderText = "الكمية", Width = 90,
                DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleCenter }
            });

            _lblStatus = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 26,
                TextAlign = ContentAlignment.MiddleRight,
                ForeColor = Color.DimGray,
                Padding = new Padding(0, 0, 15, 0),
                Font = new Font("Segoe UI", 9F)
            };

            var btnPanel = new Panel { Dock = DockStyle.Bottom, Height = 55, BackColor = Color.WhiteSmoke };
            var btnOk = new Button
            {
                Text = "فتح",
                Width = 130,
                Height = 36,
                Location = new Point(btnPanel.Width - 150, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                BackColor = Color.FromArgb(46, 160, 67),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnOk.FlatAppearance.BorderSize = 0;
            btnOk.Click += (_, _) => Confirm();

            var btnCancel = new Button
            {
                Text = "إلغاء",
                Width = 110,
                Height = 36,
                Location = new Point(btnPanel.Width - 280, 10),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

            btnPanel.Resize += (_, _) =>
            {
                btnOk.Location = new Point(btnPanel.Width - 150, 10);
                btnCancel.Location = new Point(btnPanel.Width - 280, 10);
            };
            btnPanel.Controls.Add(btnOk);
            btnPanel.Controls.Add(btnCancel);

            Controls.Add(_grid);
            Controls.Add(_lblStatus);
            Controls.Add(btnPanel);
            Controls.Add(toolbar);

            AcceptButton = btnOk;
            CancelButton = btnCancel;
        }

        private async Task LoadDataAsync()
        {
            try
            {
                _lblStatus.Text = "جارٍ التحميل...";
                var list = await ApiClient.Instance.GetAsync<List<InvoiceListRow>>("invoices") ?? new();
                _all.Clear();
                _all.AddRange(list);
                ApplyFilter();
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "خطأ: " + ex.Message;
                _lblStatus.ForeColor = Color.Firebrick;
            }
        }

        private void ApplyFilter()
        {
            var q = _txtSearch.Text.Trim();
            var st = _cmbStatus.SelectedItem as string ?? "الكل";

            IEnumerable<InvoiceListRow> result = _all;
            if (st != "الكل") result = result.Where(i => i.Status == st);
            if (!string.IsNullOrWhiteSpace(q))
            {
                result = result.Where(i =>
                    (i.InvoiceNo?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (i.SupplierName?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
            }

            _grid.DataSource = result.ToList();
            _lblStatus.Text = $"عدد الفواتير: {_grid.RowCount}";
            _lblStatus.ForeColor = Color.DimGray;
        }

        private void Confirm()
        {
            if (_grid.CurrentRow?.DataBoundItem is InvoiceListRow row)
            {
                SelectedId = row.Id;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show("الرجاء اختيار فاتورة.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}