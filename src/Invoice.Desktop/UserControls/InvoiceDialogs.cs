namespace Invoice.Desktop.UserControls;

// ============================================================
// نافذة اختيار صنف — تُستخدم عند إضافة بند في الفاتورة
// ============================================================
public class ProductPickerDialog : Form
{
    private DataGridView _grid = null!;
    private TextBox _txtSearch = null!;
    private ComboBox _cmbCategory = null!;
    private Button _btnOk = null!;
    private Button _btnCancel = null!;
    private Label _lblStatus = null!;

    private readonly List<ProductItem> _allProducts;
    private List<ProductItem> _filtered;

    public ProductItem? Selected { get; private set; }

    public ProductPickerDialog(IEnumerable<ProductItem> products)
    {
        _allProducts = products.ToList();
        _filtered = new List<ProductItem>(_allProducts);
        BuildUi();
        Load += (_, _) => ApplyFilter();
    }

    private void BuildUi()
    {
        Text = "اختيار صنف";
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.Sizable;
        MinimumSize = new Size(720, 500);
        ClientSize = new Size(850, 560);
        RightToLeft = RightToLeft.Yes;
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.WhiteSmoke;

        // شريط الأدوات
        var toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 55,
            BackColor = Color.WhiteSmoke,
            Padding = new Padding(10)
        };

        var lblSearch = new Label { Text = "بحث:", AutoSize = true, Location = new Point(toolbar.Width - 70, 20), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _txtSearch = new TextBox { Width = 220, Location = new Point(toolbar.Width - 300, 17), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _txtSearch.TextChanged += (_, _) => ApplyFilter();

        var lblCat = new Label { Text = "التصنيف:", AutoSize = true, Location = new Point(toolbar.Width - 390, 20), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _cmbCategory = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 140, Location = new Point(toolbar.Width - 530, 17), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _cmbCategory.Items.AddRange(new object[] { "الكل", "Batteries", "Car Tires", "Truck Tires" });
        _cmbCategory.SelectedIndex = 0;
        _cmbCategory.SelectedIndexChanged += (_, _) => ApplyFilter();

        toolbar.Controls.AddRange(new Control[] { lblSearch, _txtSearch, lblCat, _cmbCategory });
        toolbar.Resize += (_, _) =>
        {
            _txtSearch.Location = new Point(toolbar.Width - 300, 17);
            lblSearch.Location = new Point(toolbar.Width - 70, 20);
            _cmbCategory.Location = new Point(toolbar.Width - 530, 17);
            lblCat.Location = new Point(toolbar.Width - 390, 20);
        };

        // الجدول
        _grid = new DataGridView
        {
            Dock = DockStyle.Fill,
            AutoGenerateColumns = false,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
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

        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Code", HeaderText = "الكود", Width = 130 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "الاسم", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Category", HeaderText = "التصنيف", Width = 110 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Pattern", HeaderText = "النقشة", Width = 130 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Ah", HeaderText = "Ah", Width = 60 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "SupplierName", HeaderText = "المورد", Width = 150 });

        // شريط الحالة والأزرار
        _lblStatus = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 28,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 15, 0),
            ForeColor = Color.DimGray,
            Font = new Font("Segoe UI", 9F)
        };

        var btnPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 55,
            BackColor = Color.WhiteSmoke
        };
        _btnOk = new Button
        {
            Text = "اختيار",
            Width = 130,
            Height = 36,
            Location = new Point(btnPanel.Width - 150, 10),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = Color.FromArgb(46, 160, 67),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnOk.FlatAppearance.BorderSize = 0;
        _btnOk.Click += (_, _) => Confirm();

        _btnCancel = new Button
        {
            Text = "إلغاء",
            Width = 110,
            Height = 36,
            Location = new Point(btnPanel.Width - 280, 10),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        btnPanel.Resize += (_, _) =>
        {
            _btnOk.Location = new Point(btnPanel.Width - 150, 10);
            _btnCancel.Location = new Point(btnPanel.Width - 280, 10);
        };
        btnPanel.Controls.Add(_btnOk);
        btnPanel.Controls.Add(_btnCancel);

        Controls.Add(_grid);
        Controls.Add(_lblStatus);
        Controls.Add(btnPanel);
        Controls.Add(toolbar);

        AcceptButton = _btnOk;
        CancelButton = _btnCancel;

        // ============ التركيز على مربع البحث عند الفتح (بند 10) ============
        Shown += (_, _) =>
        {
            _txtSearch.Focus();
            _txtSearch.SelectAll();
        };
    }

    private void ApplyFilter()
    {
        var q = _txtSearch.Text.Trim();
        var cat = _cmbCategory.SelectedItem as string ?? "الكل";

        IEnumerable<ProductItem> result = _allProducts;
        if (cat != "الكل")
            result = result.Where(p => p.Category == cat);
        if (!string.IsNullOrWhiteSpace(q))
        {
            result = result.Where(p =>
                (p.Code?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Pattern?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        _filtered = result.ToList();
        _grid.DataSource = _filtered;
        _lblStatus.Text = $"عدد الأصناف: {_filtered.Count}";
    }

    private void Confirm()
    {
        if (_grid.CurrentRow?.DataBoundItem is ProductItem p)
        {
            Selected = p;
            DialogResult = DialogResult.OK;
            Close();
        }
        else
        {
            MessageBox.Show("الرجاء اختيار صنف.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }

    // ============ Model ============
    public class ProductItem
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string? Pattern { get; set; }
        public string? SupplierName { get; set; }

        // حقول إضافية (للعرض في جدول البنود)
        public decimal? Ah { get; set; }
        public decimal? L { get; set; }
        public decimal? W { get; set; }
        public decimal? H { get; set; }
        public decimal? Oh { get; set; }
        public int? Layout { get; set; }
        public string? LayoutImageFilename { get; set; }
        public int? Pallet { get; set; }
    }
}

// ============================================================
// نافذة إدخال مبلغ (دفعة/خصم/مصروف)
// ============================================================
public class AmountEntryDialog : Form
{
    private TextBox _txtLabel = null!;
    private NumericUpDown _numAmount = null!;
    private Label _lblStatus = null!;

    public string Label { get; private set; } = "";
    public decimal Amount { get; private set; }

    public AmountEntryDialog(string title, string label, decimal amount = 0m)
    {
        Text = title;
        Label = label;
        Amount = amount;
        BuildUi();
    }

    private void BuildUi()
    {
        StartPosition = FormStartPosition.CenterParent;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(420, 200);
        RightToLeft = RightToLeft.Yes;
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.WhiteSmoke;

        var lblLbl = new Label { Text = "الوصف:", Location = new Point(30, 25), AutoSize = true };
        _txtLabel = new TextBox { Location = new Point(140, 22), Width = 250, Text = Label };

        var lblAmt = new Label { Text = "المبلغ:", Location = new Point(30, 65), AutoSize = true };
        _numAmount = new NumericUpDown
        {
            Location = new Point(140, 62),
            Width = 250,
            DecimalPlaces = 2,
            Maximum = 1_000_000_000m,
            Minimum = 0m,
            ThousandsSeparator = true,
            Value = Amount
        };

        _lblStatus = new Label
        {
            Location = new Point(30, 105),
            Width = 360,
            Height = 25,
            ForeColor = Color.Firebrick,
            TextAlign = ContentAlignment.MiddleRight
        };

        var btnOk = new Button
        {
            Text = "حفظ",
            Width = 120,
            Height = 36,
            Location = new Point(270, 140),
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
            Width = 120,
            Height = 36,
            Location = new Point(140, 140),
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        AcceptButton = btnOk;
        CancelButton = btnCancel;

        Controls.AddRange(new Control[]
        {
            lblLbl, _txtLabel,
            lblAmt, _numAmount,
            _lblStatus,
            btnOk, btnCancel
        });
    }

    private void Confirm()
    {
        var lbl = _txtLabel.Text.Trim();
        if (string.IsNullOrWhiteSpace(lbl))
        {
            _lblStatus.Text = "الوصف مطلوب.";
            return;
        }
        if (_numAmount.Value <= 0m)
        {
            _lblStatus.Text = "المبلغ يجب أن يكون أكبر من صفر.";
            return;
        }

        Label = lbl;
        Amount = _numAmount.Value;
        DialogResult = DialogResult.OK;
        Close();
    }
}