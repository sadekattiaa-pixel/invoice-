using Invoice.Desktop.Models;
using Invoice.Desktop.Services;

namespace Invoice.Desktop.UserControls;

/// <summary>
/// لوحة بنود الفاتورة — إضافة/تعديل/حذف أصناف مع كميات وأسعار.
/// الحساب النهائي (التكلفة، الربح...) يأتي من الـ API عبر Calculate
/// ويُطبق على الجدول بـ ApplyCalculation.
/// </summary>
public class InvoiceItemsPanel : UserControl
{
    private DataGridView _grid = null!;
    private Button _btnAdd = null!;
    private Button _btnEdit = null!;
    private Button _btnDelete = null!;
    private Button _btnColumns = null!;
    private Label _lblTotals = null!;
    private Label _lblStatus = null!;

    private readonly List<ItemEntry> _items = new();
    private List<ProductPickerDialog.ProductItem> _allProducts = new();

    private const string GridKey = "InvoiceItems";

    private bool _suppressWidthSave = false;

    /// <summary>يُطلق عند تغيّر القائمة.</summary>
    public event Action? Changed;

    public IReadOnlyList<ItemEntry> Items => _items;

    /// <summary>مجموع البيع (كمية × سعر البيع).</summary>
    public decimal LocalTotalSale => _items.Sum(i => i.Quantity * i.SaleDzd);

    /// <summary>مجموع الشراء بالدولار (كمية × شراء $).</summary>
    public decimal LocalTotalUsd => _items.Sum(i => i.Quantity * i.PurchaseUsd);

    public InvoiceItemsPanel()
    {
        BuildUi();
        Shared.ColumnsDialog.Apply(_grid, GridKey);
        ApplyColumnWidths();
        HookColumnWidthTracking();
    }

    /// <summary>تحميل قائمة الأصناف المتاحة (تُملأ عند الفتح).</summary>
    public void SetAvailableProducts(IEnumerable<ProductPickerDialog.ProductItem> products)
    {
        _allProducts = products.ToList();
    }

    // ============================================================
    // UI
    // ============================================================

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

        var lblTitle = new Label
        {
            Text = "بنود الفاتورة (الأصناف)",
            Font = new Font("Segoe UI", 11F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 60, 120),
            Dock = DockStyle.Right,
            Width = 260,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 10, 0)
        };

        _btnAdd = MakeSmallButton("إضافة صنف", Color.FromArgb(46, 160, 67));
        _btnAdd.Click += (_, _) => OnAdd();

        _btnEdit = MakeSmallButton("تعديل", Color.FromArgb(230, 160, 30));
        _btnEdit.Click += (_, _) => OnEdit();

        _btnDelete = MakeSmallButton("حذف", Color.FromArgb(200, 50, 50));
        _btnDelete.Click += (_, _) => OnDelete();

        _btnColumns = MakeSmallButton("الأعمدة", Color.FromArgb(90, 90, 120));
        _btnColumns.Click += (_, _) =>
        {
            Shared.ColumnsDialog.ShowFor(_grid, GridKey, this);
        };

        headerPanel.Controls.Add(_btnAdd);
        headerPanel.Controls.Add(_btnEdit);
        headerPanel.Controls.Add(_btnDelete);
        headerPanel.Controls.Add(_btnColumns);
        headerPanel.Controls.Add(lblTitle);

        // ============ الإجمالي ============
        var footerPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 34,
            BackColor = Color.FromArgb(245, 245, 250),
            Padding = new Padding(8, 0, 8, 0)
        };
        _lblTotals = new Label
        {
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 60, 120),
            Padding = new Padding(0, 0, 10, 0)
        };
        footerPanel.Controls.Add(_lblTotals);

        // ============ شريط الحالة ============
        _lblStatus = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 22,
            TextAlign = ContentAlignment.MiddleRight,
            ForeColor = Color.DimGray,
            Font = new Font("Segoe UI", 8.5F),
            Padding = new Padding(0, 0, 10, 0)
        };

        // ============ الجدول ============
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
            BorderStyle = BorderStyle.None,
            RightToLeft = RightToLeft.Yes,
            Font = new Font("Segoe UI", 10F),
            ColumnHeadersHeight = 30,
            EnableHeadersVisualStyles = false,
            ScrollBars = ScrollBars.Both   // بند 4: تمرير أفقي ورأسي
        };
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 60, 120);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        _grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 220, 250);
        _grid.DefaultCellStyle.SelectionForeColor = Color.Black;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 250);
        _grid.RowTemplate.Height = 60;
        _grid.CellDoubleClick += (_, _) => OnEdit();
        _grid.CellFormatting += OnCellFormatting;

        _grid.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
                OnAdd();
            }
        };

        // ============ ترتيب الأعمدة (بند 1) ============
        var center = DataGridViewContentAlignment.MiddleCenter;
        var money = new DataGridViewCellStyle { Format = "N2", Alignment = center };
        var moneyNullable = new DataGridViewCellStyle { Format = "N2", Alignment = center, NullValue = "—" };
        var intCenter = new DataGridViewCellStyle { Format = "N0", Alignment = center };

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LineNo", DataPropertyName = "LineNo", HeaderText = "#", Width = 40 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", DataPropertyName = "ProductCode", HeaderText = "الكود", Width = 130 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductAh", DataPropertyName = "ProductAh", HeaderText = "AH", Width = 55, DefaultCellStyle = intCenter });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductL", DataPropertyName = "ProductL", HeaderText = "L", Width = 50, DefaultCellStyle = intCenter });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductW", DataPropertyName = "ProductW", HeaderText = "W", Width = 50, DefaultCellStyle = intCenter });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductH", DataPropertyName = "ProductH", HeaderText = "H", Width = 50, DefaultCellStyle = intCenter });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductOh", DataPropertyName = "ProductOh", HeaderText = "OH", Width = 55, DefaultCellStyle = intCenter });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductLayout", DataPropertyName = "ProductLayout", HeaderText = "LAYOUT", Width = 70, DefaultCellStyle = intCenter });
        _grid.Columns.Add(new DataGridViewImageColumn
        {
            Name = "ProductImage",
            DataPropertyName = "ProductLayoutImageFilename",
            HeaderText = "الصورة",
            Width = 70,
            ImageLayout = DataGridViewImageCellLayout.Zoom,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                NullValue = null,
                Alignment = center
            }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductPallet", DataPropertyName = "ProductPallet", HeaderText = "PALLET", Width = 65, DefaultCellStyle = intCenter });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCategory", DataPropertyName = "ProductCategory", HeaderText = "التصنيف", Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductPattern", DataPropertyName = "ProductPattern", HeaderText = "النقشة", Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", DataPropertyName = "Quantity", HeaderText = "الكمية", Width = 80, DefaultCellStyle = intCenter });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchaseUsd", DataPropertyName = "PurchaseUsd", HeaderText = "الشراء $", Width = 95, DefaultCellStyle = money });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalPurchaseUsd", DataPropertyName = "TotalPurchaseUsd", HeaderText = "اجمالي الشراء $", Width = 130, DefaultCellStyle = money });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "PurchaseDzd", DataPropertyName = "PurchaseDzd", HeaderText = "التكلفة", Width = 110, DefaultCellStyle = moneyNullable });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalDzd", DataPropertyName = "TotalDzd", HeaderText = "اجمالي التكلفة", Width = 130, DefaultCellStyle = moneyNullable });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SaleDzd", DataPropertyName = "SaleDzd", HeaderText = "البيع", Width = 110, DefaultCellStyle = money });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalSale", DataPropertyName = "TotalSale", HeaderText = "اجمالي البيع", Width = 130, DefaultCellStyle = money });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "UnitProfit", DataPropertyName = "UnitProfit", HeaderText = "الربح", Width = 110, DefaultCellStyle = moneyNullable });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "TotalProfit", DataPropertyName = "TotalProfit", HeaderText = "اجمالي الربح", Width = 130, DefaultCellStyle = moneyNullable });

        Controls.Add(_grid);
        Controls.Add(_lblStatus);
        Controls.Add(footerPanel);
        Controls.Add(headerPanel);

        RefreshGrid();
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

    private void OnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.ColumnIndex < 0 || e.ColumnIndex >= _grid.Columns.Count) return;
        if (_grid.Columns[e.ColumnIndex].Name != "ProductImage") return;

        var filename = e.Value as string;
        if (string.IsNullOrWhiteSpace(filename))
        {
            e.Value = null;
            e.FormattingApplied = true;
            return;
        }

        e.Value = LoadLocalImage(filename);
        e.FormattingApplied = true;
    }

    private static Image? LoadLocalImage(string filename)
    {
        try
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CostProfitApp", "Desktop", "Layouts");
            var path = Path.Combine(folder, filename);
            if (!File.Exists(path)) return null;
            var bytes = File.ReadAllBytes(path);
            return Image.FromStream(new MemoryStream(bytes));
        }
        catch
        {
            return null;
        }
    }

    private static Button MakeSmallButton(string text, Color back)
    {
        var b = new Button
        {
            Text = text,
            Width = 90,
            Dock = DockStyle.Left,
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

    public void LoadItems(IEnumerable<InvoiceItemDto> items)
    {
        _items.Clear();
        foreach (var i in items)
        {
            var prod = _allProducts.FirstOrDefault(p => p.Id == i.ProductId);

            _items.Add(new ItemEntry
            {
                ProductId = i.ProductId,
                ProductCode = i.ProductCode ?? "",
                ProductName = i.ProductName ?? "",
                ProductCategory = prod?.Category ?? i.ProductCategory ?? "",
                ProductPattern = i.ProductPattern ?? prod?.Pattern,
                ProductAh = prod?.Ah,
                ProductL = prod?.L,
                ProductW = prod?.W,
                ProductH = prod?.H,
                ProductOh = prod?.Oh,
                ProductLayout = prod?.Layout,
                ProductLayoutImageFilename = i.ProductImage ?? prod?.LayoutImageFilename,
                ProductPallet = prod?.Pallet,
                Quantity = i.Quantity,
                PurchaseUsd = i.PurchaseUsd,
                SaleDzd = i.SaleDzd,
                Pallet = i.Pallet,
                PurchaseDzd = i.PurchaseDzd,
                TotalDzd = i.TotalDzd,
                UnitProfit = i.UnitProfit,
                TotalProfit = i.TotalProfit
            });
        }
        RefreshGrid();
    }

    public void ClearAll()
    {
        _items.Clear();
        RefreshGrid();
    }

    /// <summary>
    /// يُطبّق نتائج الحساب من الخادم على البنود (بند 1: تحديث الأعمدة 16, 17, 20, 21).
    /// يُطابق البنود بالترتيب (الخادم يحافظ على ترتيب البنود المُرسَلة).
    /// </summary>
    public void ApplyCalculation(IEnumerable<CalculatedItem> calcItems)
    {
        var list = calcItems.ToList();
        for (int i = 0; i < _items.Count && i < list.Count; i++)
        {
            var c = list[i];
            _items[i].PurchaseDzd = c.PurchaseDzd;
            _items[i].TotalDzd = c.TotalDzd;
            _items[i].UnitProfit = c.UnitProfit;
            _items[i].TotalProfit = c.TotalProfit;
        }
        RefreshGrid();
    }

    // ============================================================
    // Actions
    // ============================================================

    private void OnAdd()
    {
        if (_allProducts.Count == 0)
        {
            MessageBox.Show("لا توجد أصناف متاحة. أضف أصنافًا أولًا من صفحة الأصناف.",
                "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var picker = new ProductPickerDialog(_allProducts);
        if (picker.ShowDialog(this) != DialogResult.OK) return;
        if (picker.Selected == null) return;

        if (_items.Any(x => x.ProductId == picker.Selected.Id))
        {
            var confirm = MessageBox.Show(
                "هذا الصنف مضاف مسبقًا. هل تريد إضافته مرة أخرى كبند منفصل؟",
                "تنبيه", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm != DialogResult.Yes) return;
        }

        using var dlg = new ItemEditDialog(picker.Selected, null);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        _items.Add(dlg.Result!);
        RefreshGrid();
        Changed?.Invoke();
    }

    private void OnEdit()
    {
        if (_grid.CurrentRow?.DataBoundItem is not ItemEntry row) return;
        var idx = _items.IndexOf(row);
        if (idx < 0) return;

        var product = _allProducts.FirstOrDefault(p => p.Id == row.ProductId)
                      ?? new ProductPickerDialog.ProductItem
                      {
                          Id = row.ProductId,
                          Code = row.ProductCode,
                          Name = row.ProductName,
                          Category = row.ProductCategory,
                          Pattern = row.ProductPattern,
                          Ah = row.ProductAh,
                          L = row.ProductL,
                          W = row.ProductW,
                          H = row.ProductH,
                          Oh = row.ProductOh,
                          Layout = row.ProductLayout,
                          LayoutImageFilename = row.ProductLayoutImageFilename,
                          Pallet = row.ProductPallet
                      };

        using var dlg = new ItemEditDialog(product, row);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        _items[idx] = dlg.Result!;
        RefreshGrid();
        Changed?.Invoke();
    }

    private void OnDelete()
    {
        if (_grid.CurrentRow?.DataBoundItem is not ItemEntry row) return;
        var idx = _items.IndexOf(row);
        if (idx < 0) return;

        var confirm = MessageBox.Show(
            $"هل تريد حذف البند «{row.ProductCode}»؟",
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
        for (int i = 0; i < _items.Count; i++)
            _items[i].LineNo = i + 1;

        _grid.DataSource = null;
        _grid.DataSource = _items.ToList();

        _lblTotals.Text = $"إجمالي البيع: {LocalTotalSale:N2}    |    " +
                          $"إجمالي الشراء: $ {LocalTotalUsd:N2}    |    " +
                          $"عدد البنود: {_items.Count}";
        _lblStatus.Text = "اضغط Enter لفتح نافذة اختيار صنف — أو استخدم زر «إضافة صنف».";
    }

    // ============================================================
    // Models
    // ============================================================

    public class ItemEntry
    {
        public int LineNo { get; set; }
        public int ProductId { get; set; }
        public string ProductCode { get; set; } = "";
        public string ProductName { get; set; } = "";
        public string ProductCategory { get; set; } = "";
        public string? ProductPattern { get; set; }
        public decimal? ProductAh { get; set; }
        public decimal? ProductL { get; set; }
        public decimal? ProductW { get; set; }
        public decimal? ProductH { get; set; }
        public decimal? ProductOh { get; set; }
        public int? ProductLayout { get; set; }
        public string? ProductLayoutImageFilename { get; set; }
        public int? ProductPallet { get; set; }

        // يحرّرها المستخدم
        public decimal Quantity { get; set; }
        public decimal PurchaseUsd { get; set; }
        public decimal SaleDzd { get; set; }
        public int? Pallet { get; set; }

        // محسوبة محليًا
        public decimal TotalPurchaseUsd => Quantity * PurchaseUsd;
        public decimal TotalSale => Quantity * SaleDzd;

        // تأتي من الخادم (بعد Calculate أو تحميل فاتورة محفوظة)
        public decimal? PurchaseDzd { get; set; }
        public decimal? TotalDzd { get; set; }
        public decimal? UnitProfit { get; set; }
        public decimal? TotalProfit { get; set; }
    }

    // ============================================================
    // Edit Dialog
    // ============================================================

    private class ItemEditDialog : Form
    {
        private TextBox _txtQty = null!;
        private TextBox _txtPurchase = null!;
        private TextBox _txtSale = null!;
        private TextBox _txtPallet = null!;
        private CheckBox _chkPallet = null!;
        private Label _lblStatus = null!;
        private Button _btnSave = null!;

        public ItemEntry? Result { get; private set; }

        public ItemEditDialog(ProductPickerDialog.ProductItem product, ItemEntry? existing)
        {
            BuildUi(product, existing);
        }

        private void BuildUi(ProductPickerDialog.ProductItem product, ItemEntry? existing)
        {
            bool isNew = existing == null;

            Text = isNew ? "إضافة بند" : "تعديل بند";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(480, 400);
            RightToLeft = RightToLeft.Yes;
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.WhiteSmoke;

            const int labelX = 30;
            const int fieldX = 160;
            const int fieldW = 280;
            int y = 20;

            var lblProductLbl = new Label { Text = "الصنف:", Location = new Point(labelX, y + 5), AutoSize = true };
            var lblProduct = new Label
            {
                Text = $"{product.Name}  ({product.Code})",
                Location = new Point(fieldX, y),
                Width = fieldW,
                Height = 25,
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                ForeColor = Color.FromArgb(30, 60, 120),
                TextAlign = ContentAlignment.MiddleRight
            };
            y += 40;

            var lblQty = new Label { Text = "الكمية:", Location = new Point(labelX, y + 5), AutoSize = true };
            _txtQty = MakeNumericTextBox(fieldX, y, fieldW, isNew ? null : existing!.Quantity);
            y += 42;

            var lblP = new Label { Text = "الشراء $:", Location = new Point(labelX, y + 5), AutoSize = true };
            _txtPurchase = MakeNumericTextBox(fieldX, y, fieldW, isNew ? null : existing!.PurchaseUsd);
            y += 42;

            var lblS = new Label { Text = "البيع:", Location = new Point(labelX, y + 5), AutoSize = true };
            _txtSale = MakeNumericTextBox(fieldX, y, fieldW, isNew ? null : existing!.SaleDzd);
            y += 42;

            _chkPallet = new CheckBox
            {
                Text = "تحديد Pallet",
                Location = new Point(labelX, y),
                AutoSize = true,
                Checked = existing?.Pallet.HasValue ?? false
            };
            _chkPallet.CheckedChanged += (_, _) =>
            {
                _txtPallet.Enabled = _chkPallet.Checked;
            };

            _txtPallet = MakeNumericTextBox(fieldX, y - 3, 150, existing?.Pallet);
            _txtPallet.Enabled = _chkPallet.Checked;
            y += 42;

            _lblStatus = new Label
            {
                Location = new Point(30, y + 5),
                Width = 420,
                Height = 25,
                ForeColor = Color.Firebrick,
                TextAlign = ContentAlignment.MiddleRight
            };

            _btnSave = new Button
            {
                Text = "حفظ",
                Width = 130,
                Height = 38,
                Location = new Point(320, y + 40),
                BackColor = Color.FromArgb(46, 160, 67),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnSave.FlatAppearance.BorderSize = 0;
            _btnSave.Click += (_, _) => Confirm(product, existing);

            var btnCancel = new Button
            {
                Text = "إلغاء",
                Width = 130,
                Height = 38,
                Location = new Point(180, y + 40),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

            AcceptButton = _btnSave;
            CancelButton = btnCancel;

            Controls.AddRange(new Control[]
            {
                lblProductLbl, lblProduct,
                lblQty, _txtQty,
                lblP, _txtPurchase,
                lblS, _txtSale,
                _chkPallet, _txtPallet,
                _lblStatus,
                _btnSave, btnCancel
            });

            _txtQty.TabIndex = 0;
            _txtPurchase.TabIndex = 1;
            _txtSale.TabIndex = 2;
            _chkPallet.TabStop = false;
            _txtPallet.TabIndex = 3;
            _btnSave.TabIndex = 4;
            btnCancel.TabIndex = 5;

            _txtQty.KeyDown += (_, e) => HandleEnterNavigation(e, _txtPurchase);
            _txtPurchase.KeyDown += (_, e) => HandleEnterNavigation(e, _txtSale);
            _txtSale.KeyDown += (_, e) => HandleEnterNavigation(e, _btnSave);

            Shown += (_, _) =>
            {
                _txtQty.Focus();
                _txtQty.SelectAll();
            };
        }

        private static TextBox MakeNumericTextBox(int x, int y, int w, decimal? value)
        {
            return new TextBox
            {
                Location = new Point(x, y),
                Width = w,
                TextAlign = HorizontalAlignment.Center,
                Text = value.HasValue && value.Value > 0m
                    ? value.Value.ToString("0.##")
                    : ""
            };
        }

        private static void HandleEnterNavigation(KeyEventArgs e, Control next)
        {
            if (e.KeyCode == Keys.Enter && !e.Shift && !e.Control)
            {
                e.SuppressKeyPress = true;
                e.Handled = true;
                if (next.Enabled && next.CanFocus) next.Focus();
            }
        }

        private void Confirm(ProductPickerDialog.ProductItem product, ItemEntry? existing)
        {
            if (!TryParsePositiveDecimal(_txtQty.Text, out var qty))
            {
                _lblStatus.Text = "الكمية مطلوبة ويجب أن تكون أكبر من صفر.";
                _txtQty.Focus();
                return;
            }
            if (!TryParsePositiveDecimal(_txtPurchase.Text, out var purchase))
            {
                _lblStatus.Text = "سعر الشراء مطلوب ويجب أن يكون أكبر من صفر.";
                _txtPurchase.Focus();
                return;
            }
            if (!TryParsePositiveDecimal(_txtSale.Text, out var sale))
            {
                _lblStatus.Text = "سعر البيع مطلوب ويجب أن يكون أكبر من صفر.";
                _txtSale.Focus();
                return;
            }

            int? pallet = null;
            if (_chkPallet.Checked)
            {
                if (!int.TryParse(_txtPallet.Text.Trim(), out var p) || p <= 0)
                {
                    _lblStatus.Text = "قيمة Pallet غير صالحة.";
                    _txtPallet.Focus();
                    return;
                }
                pallet = p;
            }

            Result = new ItemEntry
            {
                ProductId = product.Id,
                ProductCode = product.Code,
                ProductName = product.Name,
                ProductCategory = product.Category,
                ProductPattern = product.Pattern,
                ProductAh = product.Ah,
                ProductL = product.L,
                ProductW = product.W,
                ProductH = product.H,
                ProductOh = product.Oh,
                ProductLayout = product.Layout,
                ProductLayoutImageFilename = product.LayoutImageFilename,
                ProductPallet = product.Pallet,
                Quantity = qty,
                PurchaseUsd = purchase,
                SaleDzd = sale,
                Pallet = pallet
            };

            DialogResult = DialogResult.OK;
            Close();
        }

        private static bool TryParsePositiveDecimal(string text, out decimal value)
        {
            value = 0m;
            if (string.IsNullOrWhiteSpace(text)) return false;
            if (!decimal.TryParse(text.Trim(), out var v)) return false;
            if (v <= 0m) return false;
            value = v;
            return true;
        }
    }
}