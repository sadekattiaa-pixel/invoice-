using Invoice.Desktop.Services;

namespace Invoice.Desktop.UserControls;

/// <summary>
/// صفحة الأصناف — قائمة + بحث + فلترة + إضافة + تعديل + حذف + اختيار الأعمدة.
/// </summary>
public class ProductsUserControl : UserControl
{
    private const string GridKey = "Products";

    private DataGridView _grid = null!;
    private TextBox _txtSearch = null!;
    private ComboBox _cmbCategory = null!;
    private ComboBox _cmbSupplier = null!;
    private Button _btnRefresh = null!;
    private Button _btnAdd = null!;
    private Button _btnEdit = null!;
    private Button _btnDelete = null!;
    private Button _btnColumns = null!;
    private Label _lblStatus = null!;

    private List<ProductRow> _allData = new();
    private List<SupplierItem> _suppliers = new();

    private bool _suppressWidthSave = false;

    public ProductsUserControl()
    {
        BuildUi();
        ApplyColumnVisibility();
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

        // ============ شريط الأدوات ============
        var toolbar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 55,
            BackColor = Color.WhiteSmoke,
            Padding = new Padding(10)
        };

        _btnRefresh = MakeButton("تحديث", Color.FromArgb(30, 100, 200), 10);
        _btnRefresh.Click += async (_, _) => await LoadAllAsync();

        _btnAdd = MakeButton("إضافة جديد", Color.FromArgb(46, 160, 67), 115);
        _btnAdd.Click += async (_, _) => await OnAddAsync();

        _btnEdit = MakeButton("تعديل", Color.FromArgb(230, 160, 30), 235);
        _btnEdit.Click += async (_, _) => await OnEditAsync();

        _btnDelete = MakeButton("حذف", Color.FromArgb(200, 50, 50), 320);
        _btnDelete.Click += async (_, _) => await OnDeleteAsync();

        _btnColumns = MakeButton("الأعمدة", Color.FromArgb(90, 90, 120), 425);
        _btnColumns.Click += (_, _) => OnChooseColumns();

        var lblSearch = new Label { Text = "بحث:", AutoSize = true, Location = new Point(toolbar.Width - 70, 20), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _txtSearch = new TextBox { Width = 180, Location = new Point(toolbar.Width - 260, 17), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _txtSearch.TextChanged += (_, _) => ApplyFilter();

        var lblCat = new Label { Text = "التصنيف:", AutoSize = true, Location = new Point(toolbar.Width - 340, 20), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _cmbCategory = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 160, Location = new Point(toolbar.Width - 510, 17), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _cmbCategory.Items.AddRange(new object[] { "الكل", "Batteries", "Car Tires", "Truck Tires" });
        _cmbCategory.SelectedIndex = 0;
        _cmbCategory.SelectedIndexChanged += (_, _) => ApplyFilter();

        var lblSup = new Label { Text = "المورد:", AutoSize = true, Location = new Point(toolbar.Width - 590, 20), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _cmbSupplier = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200, Location = new Point(toolbar.Width - 800, 17), Anchor = AnchorStyles.Top | AnchorStyles.Right };
        _cmbSupplier.SelectedIndexChanged += (_, _) => ApplyFilter();

        toolbar.Controls.AddRange(new Control[]
        {
            _btnRefresh, _btnAdd, _btnEdit, _btnDelete, _btnColumns,
            lblSearch, _txtSearch,
            lblCat, _cmbCategory,
            lblSup, _cmbSupplier
        });

        toolbar.Resize += (_, _) =>
        {
            _txtSearch.Location = new Point(toolbar.Width - 260, 17);
            lblSearch.Location = new Point(toolbar.Width - 70, 20);
            _cmbCategory.Location = new Point(toolbar.Width - 510, 17);
            lblCat.Location = new Point(toolbar.Width - 340, 20);
            _cmbSupplier.Location = new Point(toolbar.Width - 800, 17);
            lblSup.Location = new Point(toolbar.Width - 590, 20);
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
        _grid.RowTemplate.Height = 60;
        _grid.CellDoubleClick += async (_, _) => await OnEditAsync();
        _grid.CellFormatting += OnCellFormatting;

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Id", DataPropertyName = "Id", HeaderText = "المعرّف", Width = 70 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Code", DataPropertyName = "Code", HeaderText = "الكود", Width = 140 });

        _grid.Columns.Add(new DataGridViewImageColumn
        {
            Name = "LayoutImage",
            DataPropertyName = "LayoutImageFilename",
            HeaderText = "الصورة",
            Width = 80,
            ImageLayout = DataGridViewImageCellLayout.Zoom,
            DefaultCellStyle = new DataGridViewCellStyle
            {
                NullValue = null,
                Alignment = DataGridViewContentAlignment.MiddleCenter
            }
        });

        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Name", DataPropertyName = "Name", HeaderText = "الاسم", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category", DataPropertyName = "Category", HeaderText = "التصنيف", Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Pattern", DataPropertyName = "Pattern", HeaderText = "النقشة", Width = 120 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Ah", DataPropertyName = "Ah", HeaderText = "Ah", Width = 60,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "L", DataPropertyName = "L", HeaderText = "L", Width = 55,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "W", DataPropertyName = "W", HeaderText = "W", Width = 55,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "H", DataPropertyName = "H", HeaderText = "H", Width = 55,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Oh", DataPropertyName = "Oh", HeaderText = "Oh", Width = 55,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Layout", DataPropertyName = "Layout", HeaderText = "Layout", Width = 70 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "Pallet", DataPropertyName = "Pallet", HeaderText = "Pallet", Width = 70,
            DefaultCellStyle = new DataGridViewCellStyle { Format = "N0", Alignment = DataGridViewContentAlignment.MiddleCenter }
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { Name = "SupplierName", DataPropertyName = "SupplierName", HeaderText = "المورد", Width = 160 });

        // ============ شريط الحالة ============
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
        Controls.Add(toolbar);
    }

    private void OnCellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
    {
        if (e.ColumnIndex < 0 || e.ColumnIndex >= _grid.Columns.Count) return;
        if (_grid.Columns[e.ColumnIndex].Name != "LayoutImage") return;

        var filename = e.Value as string;
        if (string.IsNullOrWhiteSpace(filename))
        {
            e.Value = null;
            e.FormattingApplied = true;
            return;
        }

        var img = LayoutStorage.TryLoadImage(filename);
        e.Value = img;
        e.FormattingApplied = true;
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

    // ============ Column Visibility + Widths ============

    private void ApplyColumnVisibility()
    {
        var prefs = PreferencesService.Instance;
        foreach (DataGridViewColumn col in _grid.Columns)
        {
            col.Visible = !prefs.IsColumnHidden(GridKey, col.Name);
        }
    }

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

    private void OnChooseColumns()
    {
        using var dlg = new ColumnsDialog(_grid, GridKey);
        dlg.ShowDialog(this);
        ApplyColumnVisibility();
    }

    // ============ Data ============

    private async Task LoadAllAsync()
    {
        SetStatus("جارٍ التحميل...", Color.DimGray);
        try
        {
            _suppliers = await ApiClient.Instance.GetAsync<List<SupplierItem>>("suppliers") ?? new();
            _allData = await ApiClient.Instance.GetAsync<List<ProductRow>>("products") ?? new();

            var supItems = new List<SupplierItem> { new() { Id = 0, Name = "الكل" } };
            supItems.AddRange(_suppliers);
            _cmbSupplier.DataSource = null;
            _cmbSupplier.DataSource = supItems;
            _cmbSupplier.DisplayMember = "Name";
            _cmbSupplier.ValueMember = "Id";
            _cmbSupplier.SelectedIndex = 0;

            ApplyFilter();
            SetStatus($"عدد الأصناف: {_allData.Count}", Color.SeaGreen);
        }
        catch (Exception ex)
        {
            SetStatus("خطأ: " + ex.Message, Color.Firebrick);
        }
    }

    private void ApplyFilter()
    {
        var q = _txtSearch.Text.Trim();
        var cat = _cmbCategory.SelectedItem as string ?? "الكل";
        var supId = (_cmbSupplier.SelectedItem as SupplierItem)?.Id ?? 0;

        IEnumerable<ProductRow> filtered = _allData;

        if (cat != "الكل")
            filtered = filtered.Where(p => p.Category == cat);

        if (supId != 0)
            filtered = filtered.Where(p => p.SupplierId == supId);

        if (!string.IsNullOrWhiteSpace(q))
        {
            filtered = filtered.Where(p =>
                (p.Code?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (p.Pattern?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        _grid.DataSource = filtered.ToList();
    }

    private ProductRow? GetSelected()
        => _grid.CurrentRow?.DataBoundItem as ProductRow;

    // ============ Actions ============

    private async Task OnAddAsync()
    {
        if (!ApiClient.Instance.HasPermission("products.create"))
        {
            MessageBox.Show("لا تملك صلاحية إضافة صنف.", "مرفوض", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (_suppliers.Count == 0)
        {
            MessageBox.Show("يجب إضافة مورد واحد على الأقل أولًا.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        using var dlg = new ProductEditDialog(null, _suppliers);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            await ApiClient.Instance.PostAsync("products", dlg.Result);
            await LoadAllAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show("فشل الحفظ: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task OnEditAsync()
    {
        var selected = GetSelected();
        if (selected == null)
        {
            MessageBox.Show("الرجاء اختيار صنف أولًا.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!ApiClient.Instance.HasPermission("products.update"))
        {
            MessageBox.Show("لا تملك صلاحية تعديل الأصناف.", "مرفوض", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dlg = new ProductEditDialog(selected, _suppliers);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            await ApiClient.Instance.PutAsync($"products/{selected.Id}", dlg.Result);
            await LoadAllAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show("فشل التعديل: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async Task OnDeleteAsync()
    {
        var selected = GetSelected();
        if (selected == null)
        {
            MessageBox.Show("الرجاء اختيار صنف أولًا.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        if (!ApiClient.Instance.HasPermission("products.delete"))
        {
            MessageBox.Show("لا تملك صلاحية حذف الأصناف.", "مرفوض", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"هل تريد حذف الصنف «{selected.Name}»؟",
            "تأكيد الحذف",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        try
        {
            await ApiClient.Instance.DeleteAsync($"products/{selected.Id}");
            await LoadAllAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show("فشل الحذف: " + ex.Message, "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void SetStatus(string text, Color color)
    {
        _lblStatus.Text = text;
        _lblStatus.ForeColor = color;
    }

    // ============ Models ============

    public class ProductRow
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string Category { get; set; } = "";
        public string? Pattern { get; set; }
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }

        public decimal? Ah { get; set; }
        public decimal? L { get; set; }
        public decimal? W { get; set; }
        public decimal? H { get; set; }
        public decimal? Oh { get; set; }
        public int? Layout { get; set; }
        public string? LayoutImageFilename { get; set; }
        public int? Pallet { get; set; }
    }

    public class SupplierItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    // ============ Layout Image Storage (local) ============

    internal static class LayoutStorage
    {
        public static string Folder
        {
            get
            {
                var folder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "CostProfitApp", "Desktop", "Layouts");
                Directory.CreateDirectory(folder);
                return folder;
            }
        }

        public static string CopyToStorage(string sourcePath)
        {
            var ext = Path.GetExtension(sourcePath).ToLowerInvariant();
            if (string.IsNullOrEmpty(ext)) ext = ".png";
            var filename = $"layout_{Guid.NewGuid():N}{ext}";
            File.Copy(sourcePath, Path.Combine(Folder, filename), overwrite: true);
            return filename;
        }

        public static string? GetFullPath(string? filename)
        {
            if (string.IsNullOrWhiteSpace(filename)) return null;
            var path = Path.Combine(Folder, filename);
            return File.Exists(path) ? path : null;
        }

        public static Image? TryLoadImage(string? filename)
        {
            var path = GetFullPath(filename);
            if (path == null) return null;
            try
            {
                var bytes = File.ReadAllBytes(path);
                var ms = new MemoryStream(bytes);
                return Image.FromStream(ms);
            }
            catch
            {
                return null;
            }
        }
    }

    // ============ Columns Dialog ============

    private class ColumnsDialog : Form
    {
        private readonly DataGridView _grid;
        private readonly string _gridKey;
        private readonly List<CheckBox> _checkBoxes = new();

        public ColumnsDialog(DataGridView grid, string gridKey)
        {
            _grid = grid;
            _gridKey = gridKey;
            BuildUi();
        }

        private void BuildUi()
        {
            Text = "إظهار / إخفاء الأعمدة";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(320, 460);
            RightToLeft = RightToLeft.Yes;
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.WhiteSmoke;

            var title = new Label
            {
                Text = "اختر الأعمدة التي تريد عرضها:",
                Dock = DockStyle.Top,
                Height = 32,
                TextAlign = ContentAlignment.MiddleRight,
                Padding = new Padding(0, 8, 10, 0),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold)
            };

            var panel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(15, 5, 15, 5),
                BackColor = Color.White
            };

            var prefs = PreferencesService.Instance;
            foreach (DataGridViewColumn col in _grid.Columns)
            {
                var cb = new CheckBox
                {
                    Text = string.IsNullOrEmpty(col.HeaderText) ? col.Name : col.HeaderText,
                    Checked = !prefs.IsColumnHidden(_gridKey, col.Name),
                    Tag = col.Name,
                    Dock = DockStyle.Top,
                    Height = 30,
                    Padding = new Padding(0, 5, 0, 0)
                };
                _checkBoxes.Add(cb);
                panel.Controls.Add(cb);
            }

            var btnPanel = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 55,
                BackColor = Color.WhiteSmoke
            };

            var btnSave = new Button
            {
                Text = "حفظ",
                Width = 120,
                Height = 36,
                Location = new Point(180, 10),
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
                Width = 120,
                Height = 36,
                Location = new Point(50, 10),
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
                Width = 120,
                Height = 36,
                Location = new Point(-80, 10),
                Visible = false
            };

            btnPanel.Controls.Add(btnSave);
            btnPanel.Controls.Add(btnShowAll);
            btnPanel.Controls.Add(btnCancel);

            Controls.Add(panel);
            Controls.Add(btnPanel);
            Controls.Add(title);

            AcceptButton = btnSave;
        }

        private void SaveAndClose()
        {
            var prefs = PreferencesService.Instance;
            foreach (var cb in _checkBoxes)
            {
                var colName = cb.Tag as string ?? "";
                if (string.IsNullOrEmpty(colName)) continue;
                prefs.SetColumnVisible(_gridKey, colName, cb.Checked);
            }
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    // ============ Edit Dialog ============

    private class ProductEditDialog : Form
    {
        private ComboBox _cmbCategory = null!;
        private ComboBox _cmbSupplier = null!;

        private Label _lblCode = null!;
        private TextBox _txtCode = null!;

        private Label _lblAh = null!; private TextBox _txtAh = null!;
        private Label _lblL = null!;  private TextBox _txtL = null!;
        private Label _lblW = null!;  private TextBox _txtW = null!;
        private Label _lblH = null!;  private TextBox _txtH = null!;
        private Label _lblOh = null!; private TextBox _txtOh = null!;

        private Label _lblLayout = null!;
        private TextBox _txtLayout = null!;

        private Label _lblPicture = null!;
        private Button _btnPickImage = null!;
        private Button _btnClearImage = null!;
        private PictureBox _picLayout = null!;

        private Label _lblPallet = null!;
        private TextBox _txtPallet = null!;

        private Label _lblName = null!;  private TextBox _txtName = null!;
        private Label _lblPattern = null!; private TextBox _txtPattern = null!;

        private Label _lblStatus = null!;
        private string? _layoutImageFilename;

        public object Result { get; private set; } = new { };

        public ProductEditDialog(ProductRow? existing, List<SupplierItem> suppliers)
        {
            BuildUi(existing, suppliers);
        }

        private void BuildUi(ProductRow? existing, List<SupplierItem> suppliers)
        {
            Text = existing == null ? "إضافة صنف جديد" : $"تعديل الصنف — {existing.Name}";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(520, 820);
            RightToLeft = RightToLeft.Yes;
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.WhiteSmoke;

            const int labelX = 30;
            const int fieldX = 160;
            const int fieldW = 330;
            int y = 20;

            var lblCategory = new Label { Text = "التصنيف:", Location = new Point(labelX, y + 5), AutoSize = true };
            _cmbCategory = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(fieldX, y),
                Width = fieldW
            };
            _cmbCategory.Items.AddRange(new object[] { "Batteries", "Car Tires", "Truck Tires" });
            _cmbCategory.SelectedItem = existing?.Category ?? "Batteries";
            _cmbCategory.SelectedIndexChanged += (_, _) => UpdateFieldsByCategory();
            y += 42;

            _lblCode = new Label { Text = "الكود *:", Location = new Point(labelX, y + 5), AutoSize = true };
            _txtCode = new TextBox { Location = new Point(fieldX, y), Width = fieldW, Text = existing?.Code ?? "" };
            y += 42;

            _lblName = new Label { Text = "الاسم:", Location = new Point(labelX, y + 5), AutoSize = true };
            _txtName = new TextBox { Location = new Point(fieldX, y), Width = fieldW, Text = existing?.Name ?? "" };
            y += 42;

            _lblAh = new Label { Text = "Ah (أمبير):", Location = new Point(labelX, y + 5), AutoSize = true };
            _txtAh = new TextBox { Location = new Point(fieldX, y), Width = fieldW, Text = FormatInt(existing?.Ah) };
            y += 42;

            _lblL = new Label { Text = "L (الطول):", Location = new Point(labelX, y + 5), AutoSize = true };
            _txtL = new TextBox { Location = new Point(fieldX, y), Width = fieldW, Text = FormatInt(existing?.L) };
            y += 42;

            _lblW = new Label { Text = "W (العرض):", Location = new Point(labelX, y + 5), AutoSize = true };
            _txtW = new TextBox { Location = new Point(fieldX, y), Width = fieldW, Text = FormatInt(existing?.W) };
            y += 42;

            _lblH = new Label { Text = "H (الارتفاع):", Location = new Point(labelX, y + 5), AutoSize = true };
            _txtH = new TextBox { Location = new Point(fieldX, y), Width = fieldW, Text = FormatInt(existing?.H) };
            y += 42;

            _lblOh = new Label { Text = "Oh:", Location = new Point(labelX, y + 5), AutoSize = true };
            _txtOh = new TextBox { Location = new Point(fieldX, y), Width = fieldW, Text = FormatInt(existing?.Oh) };
            y += 42;

            _lblLayout = new Label { Text = "Layout (LAY NUM):", Location = new Point(labelX, y + 5), AutoSize = true };
            _txtLayout = new TextBox { Location = new Point(fieldX, y), Width = fieldW, Text = existing?.Layout?.ToString() ?? "" };
            y += 42;

            _lblPicture = new Label { Text = "صورة الرسمة:", Location = new Point(labelX, y + 5), AutoSize = true };
            _btnPickImage = new Button
            {
                Text = "اختيار صورة...",
                Location = new Point(fieldX, y),
                Width = 150,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(230, 230, 240)
            };
            _btnPickImage.Click += (_, _) => PickImage();

            _btnClearImage = new Button
            {
                Text = "إزالة",
                Location = new Point(fieldX + 160, y),
                Width = 80,
                Height = 30,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand,
                BackColor = Color.FromArgb(240, 200, 200)
            };
            _btnClearImage.Click += (_, _) => ClearImage();
            y += 36;

            _picLayout = new PictureBox
            {
                Location = new Point(fieldX, y),
                Width = 150,
                Height = 100,
                BorderStyle = BorderStyle.FixedSingle,
                SizeMode = PictureBoxSizeMode.Zoom,
                BackColor = Color.White
            };
            y += 108;

            _lblPallet = new Label { Text = "Pallet:", Location = new Point(labelX, y + 5), AutoSize = true };
            _txtPallet = new TextBox { Location = new Point(fieldX, y), Width = fieldW, Text = existing?.Pallet?.ToString() ?? "" };
            y += 42;

            _lblPattern = new Label { Text = "النقشة *:", Location = new Point(labelX, y + 5), AutoSize = true };
            _txtPattern = new TextBox { Location = new Point(fieldX, y), Width = fieldW, Text = existing?.Pattern ?? "" };
            y += 42;

            var lblSupplier = new Label { Text = "المورد:", Location = new Point(labelX, y + 5), AutoSize = true };
            _cmbSupplier = new ComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(fieldX, y),
                Width = fieldW,
                DataSource = suppliers.ToList(),
                DisplayMember = "Name",
                ValueMember = "Id"
            };
            if (existing != null)
            {
                var found = suppliers.FirstOrDefault(s => s.Id == existing.SupplierId);
                if (found != null) _cmbSupplier.SelectedItem = found;
            }
            y += 42;

            _lblStatus = new Label
            {
                Location = new Point(30, y + 5),
                Width = 460,
                Height = 25,
                ForeColor = Color.Firebrick,
                TextAlign = ContentAlignment.MiddleRight
            };

            var btnSave = new Button
            {
                Text = "حفظ",
                Width = 130,
                Height = 38,
                Location = new Point(340, y + 40),
                BackColor = Color.FromArgb(46, 160, 67),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnSave.FlatAppearance.BorderSize = 0;
            btnSave.Click += (_, _) => OnSave(existing);

            var btnCancel = new Button
            {
                Text = "إلغاء",
                Width = 130,
                Height = 38,
                Location = new Point(200, y + 40),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

            AcceptButton = btnSave;
            CancelButton = btnCancel;

            Controls.Add(lblCategory); Controls.Add(_cmbCategory);
            Controls.Add(_lblCode); Controls.Add(_txtCode);
            Controls.Add(_lblName); Controls.Add(_txtName);
            Controls.Add(_lblAh); Controls.Add(_txtAh);
            Controls.Add(_lblL); Controls.Add(_txtL);
            Controls.Add(_lblW); Controls.Add(_txtW);
            Controls.Add(_lblH); Controls.Add(_txtH);
            Controls.Add(_lblOh); Controls.Add(_txtOh);
            Controls.Add(_lblLayout); Controls.Add(_txtLayout);
            Controls.Add(_lblPicture);
            Controls.Add(_btnPickImage);
            Controls.Add(_btnClearImage);
            Controls.Add(_picLayout);
            Controls.Add(_lblPallet); Controls.Add(_txtPallet);
            Controls.Add(_lblPattern); Controls.Add(_txtPattern);
            Controls.Add(lblSupplier); Controls.Add(_cmbSupplier);
            Controls.Add(_lblStatus);
            Controls.Add(btnSave);
            Controls.Add(btnCancel);

            _cmbCategory.TabIndex = 0;
            _txtCode.TabIndex = 1;
            _txtName.TabIndex = 2;
            _txtAh.TabIndex = 3;
            _txtL.TabIndex = 4;
            _txtW.TabIndex = 5;
            _txtH.TabIndex = 6;
            _txtOh.TabIndex = 7;
            _txtLayout.TabIndex = 8;
            _btnPickImage.TabStop = false;
            _btnClearImage.TabStop = false;
            _txtPallet.TabIndex = 9;
            _txtPattern.TabIndex = 10;
            _cmbSupplier.TabIndex = 11;
            btnSave.TabIndex = 12;
            btnCancel.TabIndex = 13;

            TextBox[] enterFields =
            {
                _txtCode, _txtAh, _txtL, _txtW, _txtH, _txtOh,
                _txtLayout, _txtPallet, _txtPattern
            };
            foreach (var tb in enterFields)
            {
                tb.KeyDown += (s, e) =>
                {
                    if (e.KeyCode == Keys.Enter && !e.Shift && !e.Control)
                    {
                        e.SuppressKeyPress = true;
                        e.Handled = true;
                        SelectNextControl((Control)s!, forward: true, tabStopOnly: true, nested: true, wrap: true);
                    }
                };
            }

            Shown += (_, _) =>
            {
                if (_txtCode.Visible && _txtCode.CanFocus) _txtCode.Focus();
                else if (_cmbCategory.CanFocus) _cmbCategory.Focus();
            };

            if (!string.IsNullOrWhiteSpace(existing?.LayoutImageFilename))
            {
                _layoutImageFilename = existing!.LayoutImageFilename;
                _picLayout.Image = LayoutStorage.TryLoadImage(_layoutImageFilename);
            }

            UpdateFieldsByCategory();
        }

        private static string FormatInt(decimal? value)
            => value.HasValue ? ((long)value.Value).ToString() : "";

        private void UpdateFieldsByCategory()
        {
            bool isBattery = (_cmbCategory.SelectedItem as string) == "Batteries";

            _lblAh.Visible = isBattery; _txtAh.Visible = isBattery;
            _lblL.Visible = isBattery; _txtL.Visible = isBattery;
            _lblW.Visible = isBattery; _txtW.Visible = isBattery;
            _lblH.Visible = isBattery; _txtH.Visible = isBattery;
            _lblOh.Visible = isBattery; _txtOh.Visible = isBattery;
            _lblLayout.Visible = isBattery; _txtLayout.Visible = isBattery;
            _lblPicture.Visible = isBattery;
            _btnPickImage.Visible = isBattery;
            _btnClearImage.Visible = isBattery;
            _picLayout.Visible = isBattery;
            _lblPallet.Visible = isBattery; _txtPallet.Visible = isBattery;

            _lblPattern.Visible = !isBattery; _txtPattern.Visible = !isBattery;

            _lblCode.Visible = true; _txtCode.Visible = true;
            _lblName.Visible = false; _txtName.Visible = false;
        }

        private void PickImage()
        {
            using var ofd = new OpenFileDialog
            {
                Title = "اختر صورة الرسمة",
                Filter = "Images|*.png;*.jpg;*.jpeg;*.gif;*.bmp;*.webp"
            };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            try
            {
                _layoutImageFilename = LayoutStorage.CopyToStorage(ofd.FileName);
                _picLayout.Image?.Dispose();
                _picLayout.Image = LayoutStorage.TryLoadImage(_layoutImageFilename);
            }
            catch (Exception ex)
            {
                _lblStatus.Text = "فشل تحميل الصورة: " + ex.Message;
            }
        }

        private void ClearImage()
        {
            _layoutImageFilename = null;
            _picLayout.Image?.Dispose();
            _picLayout.Image = null;
        }

        private void OnSave(ProductRow? existing)
        {
            var category = _cmbCategory.SelectedItem as string ?? "Batteries";
            var supplier = _cmbSupplier.SelectedItem as SupplierItem;

            if (supplier == null) { _lblStatus.Text = "الرجاء اختيار مورد."; return; }

            var code = _txtCode.Text.Trim();
            if (string.IsNullOrWhiteSpace(code)) { _lblStatus.Text = "كود الصنف مطلوب."; return; }

            if (category == "Batteries")
            {
                if (!TryParseNullableDecimalFromInt(_txtAh.Text, out var ah)) { _lblStatus.Text = "Ah يجب أن يكون عددًا صحيحًا."; return; }
                if (!TryParseNullableDecimalFromInt(_txtL.Text, out var l))   { _lblStatus.Text = "L يجب أن يكون عددًا صحيحًا."; return; }
                if (!TryParseNullableDecimalFromInt(_txtW.Text, out var w))   { _lblStatus.Text = "W يجب أن يكون عددًا صحيحًا."; return; }
                if (!TryParseNullableDecimalFromInt(_txtH.Text, out var h))   { _lblStatus.Text = "H يجب أن يكون عددًا صحيحًا."; return; }
                if (!TryParseNullableDecimalFromInt(_txtOh.Text, out var oh)) { _lblStatus.Text = "Oh يجب أن يكون عددًا صحيحًا."; return; }

                if (!TryParseNullableInt(_txtLayout.Text, out var layout)) { _lblStatus.Text = "Layout يجب أن يكون عددًا صحيحًا."; return; }
                if (!TryParseNullableInt(_txtPallet.Text, out var pallet)) { _lblStatus.Text = "Pallet يجب أن يكون عددًا صحيحًا."; return; }

                if (existing == null)
                {
                    Result = new
                    {
                        category,
                        code,
                        name = code,
                        supplierId = supplier.Id,
                        ah, l, w, h, oh,
                        layout,
                        layoutImageFilename = _layoutImageFilename,
                        pallet
                    };
                }
                else
                {
                    Result = new
                    {
                        id = existing.Id,
                        category,
                        code,
                        name = code,
                        supplierId = supplier.Id,
                        ah, l, w, h, oh,
                        layout,
                        layoutImageFilename = _layoutImageFilename,
                        pallet
                    };
                }
            }
            else
            {
                var pattern = _txtPattern.Text.Trim();
                if (string.IsNullOrWhiteSpace(pattern)) { _lblStatus.Text = "نقشة الإطار مطلوبة."; return; }

                if (existing == null)
                {
                    Result = new
                    {
                        category,
                        code,
                        name = code,
                        pattern,
                        supplierId = supplier.Id
                    };
                }
                else
                {
                    Result = new
                    {
                        id = existing.Id,
                        category,
                        code,
                        name = code,
                        pattern,
                        supplierId = supplier.Id
                    };
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private static bool TryParseNullableInt(string text, out int? value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(text)) return true;
            if (int.TryParse(text.Trim(), out var v)) { value = v; return true; }
            return false;
        }

        private static bool TryParseNullableDecimalFromInt(string text, out decimal? value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(text)) return true;
            if (int.TryParse(text.Trim(), out var v)) { value = v; return true; }
            return false;
        }
    }
}