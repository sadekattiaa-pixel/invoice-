using Invoice.Desktop.Models;

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

        // ============ شريط الحالة (فوق الفوتر بقليل) ============
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
        _grid.RowTemplate.Height = 28;
        _grid.CellDoubleClick += (_, _) => OnEdit();

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "LineNo", DataPropertyName = "LineNo", HeaderText = "#", Width = 40 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductCode", DataPropertyName = "ProductCode", HeaderText = "الكود", Width = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", DataPropertyName = "ProductName", HeaderText = "الصنف", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
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
            Name = "Pallet", DataPropertyName = "Pallet", HeaderText = "Pallet", Width = 70,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleCenter }
        });

        Controls.Add(_grid);
        Controls.Add(_lblStatus);
        Controls.Add(footerPanel);
        Controls.Add(headerPanel);

        RefreshGrid();
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

    /// <summary>تحميل بنود من فاتورة موجودة.</summary>
    public void LoadItems(IEnumerable<InvoiceItemDto> items)
    {
        _items.Clear();
        foreach (var i in items)
        {
            _items.Add(new ItemEntry
            {
                ProductId = i.ProductId,
                ProductCode = i.ProductCode ?? "",
                ProductName = i.ProductName ?? "",
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

        // هل الصنف مضاف مسبقًا؟
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
                          Category = ""
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
        // ترقيم تسلسلي
        for (int i = 0; i < _items.Count; i++)
            _items[i].LineNo = i + 1;

        _grid.DataSource = null;
        _grid.DataSource = _items.ToList();

        _lblTotals.Text = $"إجمالي البيع: {LocalTotalSale:N2} د.ل    |    " +
                          $"إجمالي الشراء: $ {LocalTotalUsd:N2}    |    " +
                          $"عدد البنود: {_items.Count}";
        _lblStatus.Text = "الحسابات النهائية (الربح، سعر الصرف، صافي التكلفة) تُحسب عند الضغط على «حساب» أو «حفظ».";
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
        public decimal Quantity { get; set; }
        public decimal PurchaseUsd { get; set; }
        public decimal SaleDzd { get; set; }
        public int? Pallet { get; set; }

        /// <summary>للعرض فقط — كمية × سعر البيع.</summary>
        public decimal TotalSale => Quantity * SaleDzd;
    }

    // ============================================================
    // Edit Dialog (كمية + شراء$ + بيع د.ل + Pallet)
    // ============================================================

    private class ItemEditDialog : Form
    {
        private NumericUpDown _numQty = null!;
        private NumericUpDown _numPurchase = null!;
        private NumericUpDown _numSale = null!;
        private NumericUpDown _numPallet = null!;
        private CheckBox _chkPallet = null!;
        private Label _lblStatus = null!;

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

            // اسم الصنف (للقراءة فقط)
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

            // الكمية
            var lblQty = new Label { Text = "الكمية:", Location = new Point(labelX, y + 5), AutoSize = true };
            _numQty = MakeNumeric(fieldX, y, fieldW, decimalPlaces: 0, max: 1_000_000m, value: existing?.Quantity ?? 1m);
            y += 42;

            // سعر الشراء بالدولار
            var lblP = new Label { Text = "الشراء $:", Location = new Point(labelX, y + 5), AutoSize = true };
            _numPurchase = MakeNumeric(fieldX, y, fieldW, decimalPlaces: 2, max: 1_000_000m, value: existing?.PurchaseUsd ?? 0m);
            y += 42;

            // سعر البيع بالدينار
            var lblS = new Label { Text = "البيع د.ل:", Location = new Point(labelX, y + 5), AutoSize = true };
            _numSale = MakeNumeric(fieldX, y, fieldW, decimalPlaces: 2, max: 1_000_000_000m, value: existing?.SaleDzd ?? 0m);
            y += 42;

            // Pallet (اختياري)
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

            var btnSave = new Button
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
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (_, _) => Confirm(product, existing);

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

            AcceptButton = btnSave;
            CancelButton = btnCancel;

            Controls.AddRange(new Control[]
            {
                lblProductLbl, lblProduct,
                lblQty, _numQty,
                lblP, _numPurchase,
                lblS, _numSale,
                _chkPallet, _numPallet,
                _lblStatus,
                btnSave, btnCancel
            });
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
            if (_numQty.Value <= 0m) { _lblStatus.Text = "الكمية يجب أن تكون أكبر من صفر."; return; }
            if (_numPurchase.Value <= 0m) { _lblStatus.Text = "سعر الشراء يجب أن يكون أكبر من صفر."; return; }
            if (_numSale.Value <= 0m) { _lblStatus.Text = "سعر البيع يجب أن يكون أكبر من صفر."; return; }

            Result = new ItemEntry
            {
                ProductId = product.Id,
                ProductCode = product.Code,
                ProductName = product.Name,
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