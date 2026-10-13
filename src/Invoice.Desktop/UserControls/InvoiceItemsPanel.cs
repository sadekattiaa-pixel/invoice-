using Invoice.Desktop.Models;
using Invoice.Desktop.Services;

namespace Invoice.Desktop.UserControls;

/// <summary>
/// لوحة بنود الفاتورة — إضافة/تعديل/حذف أصناف مع كميات وأسعار.
/// الحساب النهائي (الربح، سعر الصرف...) يأتي من الـ API عبر Calculate.
/// هذا الـ Panel يحسب فقط معاينة محلية لإجمالي البيع.
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
            EnableHeadersVisualStyles = false
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

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LineNo", DataPropertyName = "LineNo", HeaderText = "#", Width = 40 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", DataPropertyName = "ProductCode", HeaderText = "الكود", Width = 120 });

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
                Alignment = DataGridViewContentAlignment.MiddleCenter
            }
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", DataPropertyName = "ProductName", HeaderText = "الصنف", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCategory", DataPropertyName = "ProductCategory", HeaderText = "التصنيف", Width = 90 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductPattern", DataPropertyName = "ProductPattern", HeaderText = "النقشة", Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ProductAh", DataPropertyName = "ProductAh", HeaderText = "Ah", Width = 55,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ProductL", DataPropertyName = "ProductL", HeaderText = "L", Width = 50,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ProductW", DataPropertyName = "ProductW", HeaderText = "W", Width = 50,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ProductH", DataPropertyName = "ProductH", HeaderText = "H", Width = 50,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ProductOh", DataPropertyName = "ProductOh", HeaderText = "Oh", Width = 50,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ProductLayout", DataPropertyName = "ProductLayout", HeaderText = "Layout", Width = 65,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "ProductPallet", DataPropertyName = "ProductPallet", HeaderText = "Pallet", Width = 65,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleCenter }
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Quantity", DataPropertyName = "Quantity", HeaderText = "الكمية", Width = 80,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "PurchaseUsd", DataPropertyName = "PurchaseUsd", HeaderText = "الشراء $", Width = 90,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "SaleDzd", DataPropertyName = "SaleDzd", HeaderText = "البيع د.ل", Width = 100,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "TotalSale", DataPropertyName = "TotalSale", HeaderText = "إجمالي البيع", Width = 120,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N2", Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Pallet", DataPropertyName = "Pallet", HeaderText = "Pallet (سطر)", Width = 90,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleCenter }
        });

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
                Pallet = i.Pallet
            });
        }
        RefreshGrid();
    }

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
            $"هل تريد حذف البند «{row.ProductName}»؟",
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

        _lblTotals.Text = $"إجمالي البيع: {LocalTotalSale:N2} د.ل    |    " +
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

        public decimal Quantity { get; set; }
        public decimal PurchaseUsd { get; set; }
        public decimal SaleDzd { get; set; }
        public int? Pallet { get; set; }

        public decimal TotalSale => Quantity * SaleDzd;
    }

    // ============================================================
    // Edit Dialog
    // ============================================================

    private class ItemEditDialog : Form
    {
        private NumericUpDown _numQty = null!;
        private NumericUpDown _numPurchase = null!;
        private NumericUpDown _numSale = null!;
        private NumericUpDown _numPallet = null!;
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
            Text = existing == null ? "إضافة بند" : "تعديل بند";
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
            _numQty = MakeNumeric(fieldX, y, fieldW, decimalPlaces: 0, max: 1_000_000m, value: existing?.Quantity ?? 1m);
            y += 42;

            var lblP = new Label { Text = "الشراء $:", Location = new Point(labelX, y + 5), AutoSize = true };
            _numPurchase = MakeNumeric(fieldX, y, fieldW, decimalPlaces: 2, max: 1_000_000m, value: existing?.PurchaseUsd ?? 0m);
            y += 42;

            var lblS = new Label { Text = "البيع د.ل:", Location = new Point(labelX, y + 5), AutoSize = true };
            _numSale = MakeNumeric(fieldX, y, fieldW, decimalPlaces: 2, max: 1_000_000_000m, value: existing?.SaleDzd ?? 0m);
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
                _numPallet.Enabled = _chkPallet.Checked;
            };

            _numPallet = MakeNumeric(fieldX, y - 5, 150, decimalPlaces: 0, max: 10_000m, value: existing?.Pallet ?? 0m);
            _numPallet.Enabled = _chkPallet.Checked;
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
                lblQty, _numQty,
                lblP, _numPurchase,
                lblS, _numSale,
                _chkPallet, _numPallet,
                _lblStatus,
                _btnSave, btnCancel
            });

            _numQty.TabIndex = 0;
            _numPurchase.TabIndex = 1;
            _numSale.TabIndex = 2;
            _chkPallet.TabStop = false;
            _numPallet.TabIndex = 3;
            _btnSave.TabIndex = 4;
            btnCancel.TabIndex = 5;

            _numQty.KeyDown += (_, e) => HandleEnterNavigation(e, _numPurchase);
            _numPurchase.KeyDown += (_, e) => HandleEnterNavigation(e, _numSale);
            _numSale.KeyDown += (_, e) => HandleEnterNavigation(e, _btnSave);

            Shown += (_, _) =>
            {
                _numQty.Focus();
                _numQty.Select(0, _numQty.Text.Length);
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

        private static NumericUpDown MakeNumeric(int x, int y, int w, int decimalPlaces, decimal max, decimal value)
        {
            return new NumericUpDown
            {
                Location = new Point(x, y),
                Width = w,
                DecimalPlaces = decimalPlaces,
                Maximum = max,
                Minimum = 0m,
                ThousandsSeparator = true,
                Value = Math.Max(0m, Math.Min(max, value))
            };
        }

        private void Confirm(ProductPickerDialog.ProductItem product, ItemEntry? existing)
        {
            if (_numQty.Value <= 0m) { _lblStatus.Text = "الكمية يجب أن تكون أكبر من صفر."; _numQty.Focus(); return; }
            if (_numPurchase.Value <= 0m) { _lblStatus.Text = "سعر الشراء يجب أن يكون أكبر من صفر."; _numPurchase.Focus(); return; }
            if (_numSale.Value <= 0m) { _lblStatus.Text = "سعر البيع يجب أن يكون أكبر من صفر."; _numSale.Focus(); return; }

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
                Quantity = _numQty.Value,
                PurchaseUsd = _numPurchase.Value,
                SaleDzd = _numSale.Value,
                Pallet = _chkPallet.Checked ? (int?)_numPallet.Value : null
            };

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}