using System.Net.Http.Json;
using System.Text.Json;
using Invoice.Desktop.Services;

namespace Invoice.Desktop.UserControls;

/// <summary>
/// صفحة الموردين — قائمة + إضافة + تعديل + حذف + بحث.
/// </summary>
public class SuppliersUserControl : UserControl
{
    private DataGridView _grid = null!;
    private TextBox _txtSearch = null!;
    private Button _btnRefresh = null!;
    private Button _btnAdd = null!;
    private Button _btnEdit = null!;
    private Button _btnDelete = null!;
    private Label _lblStatus = null!;
    private List<SupplierRow> _allData = new();

    public SuppliersUserControl()
    {
        BuildUi();
        Load += async (_, _) => await LoadDataAsync();
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
        _btnRefresh.Click += async (_, _) => await LoadDataAsync();

        _btnAdd = MakeButton("إضافة جديد", Color.FromArgb(46, 160, 67), 115);
        _btnAdd.Click += async (_, _) => await OnAddAsync();

        _btnEdit = MakeButton("تعديل", Color.FromArgb(230, 160, 30), 235);
        _btnEdit.Click += async (_, _) => await OnEditAsync();

        _btnDelete = MakeButton("حذف", Color.FromArgb(200, 50, 50), 320);
        _btnDelete.Click += async (_, _) => await OnDeleteAsync();

        var lblSearch = new Label
        {
            Text = "بحث:",
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(toolbar.Width - 65, 20)
        };

        _txtSearch = new TextBox
        {
            Width = 220,
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(toolbar.Width - 290, 17)
        };
        _txtSearch.TextChanged += (_, _) => ApplyFilter();

        toolbar.Controls.AddRange(new Control[]
        {
            _btnRefresh, _btnAdd, _btnEdit, _btnDelete,
            lblSearch, _txtSearch
        });
        toolbar.Resize += (_, _) =>
        {
            _txtSearch.Location = new Point(toolbar.Width - 290, 17);
            lblSearch.Location = new Point(toolbar.Width - 65, 20);
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
            BorderStyle = BorderStyle.FixedSingle,
            RightToLeft = RightToLeft.Yes,
            Font = new Font("Segoe UI", 10F),
            ColumnHeadersHeight = 36,
            EnableHeadersVisualStyles = false
        };
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(30, 60, 120);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
        _grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(200, 220, 250);
        _grid.DefaultCellStyle.SelectionForeColor = Color.Black;
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 245, 250);
        _grid.RowTemplate.Height = 30;
        _grid.CellDoubleClick += async (_, _) => await OnEditAsync();

        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Id", HeaderText = "المعرّف", Width = 70, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleCenter } });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Code", HeaderText = "الكود", Width = 100 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "الاسم", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Phone", HeaderText = "الهاتف", Width = 130 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Email", HeaderText = "الإيميل", Width = 180 });
        _grid.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Address", HeaderText = "العنوان", Width = 250 });

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

    // ============ Data ============

    private async Task LoadDataAsync()
    {
        SetStatus("جارٍ التحميل...", Color.DimGray);
        try
        {
            var list = await ApiClient.Instance.GetAsync<List<SupplierRow>>("suppliers") ?? new();
            _allData = list;
            ApplyFilter();
            SetStatus($"عدد الموردين: {_allData.Count}", Color.SeaGreen);
        }
        catch (Exception ex)
        {
            SetStatus("خطأ: " + ex.Message, Color.Firebrick);
        }
    }

    private void ApplyFilter()
    {
        var q = _txtSearch.Text.Trim();
        IEnumerable<SupplierRow> filtered = _allData;

        if (!string.IsNullOrWhiteSpace(q))
        {
            filtered = filtered.Where(s =>
                (s.Code?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.Name?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.Phone?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (s.Email?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false));
        }

        _grid.DataSource = filtered.ToList();
    }

    private SupplierRow? GetSelected()
    {
        if (_grid.CurrentRow?.DataBoundItem is SupplierRow row) return row;
        return null;
    }

    // ============ Actions ============

    private async Task OnAddAsync()
    {
        if (!ApiClient.Instance.HasPermission("suppliers.create"))
        {
            MessageBox.Show("لا تملك صلاحية إضافة مورد.", "مرفوض", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dlg = new SupplierEditDialog(null);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            await ApiClient.Instance.PostAsync("suppliers", dlg.Result);
            await LoadDataAsync();
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
            MessageBox.Show("الرجاء اختيار مورد أولًا.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!ApiClient.Instance.HasPermission("suppliers.update"))
        {
            MessageBox.Show("لا تملك صلاحية تعديل الموردين.", "مرفوض", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using var dlg = new SupplierEditDialog(selected);
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            await ApiClient.Instance.PutAsync($"suppliers/{selected.Id}", dlg.Result);
            await LoadDataAsync();
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
            MessageBox.Show("الرجاء اختيار مورد أولًا.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (!ApiClient.Instance.HasPermission("suppliers.delete"))
        {
            MessageBox.Show("لا تملك صلاحية حذف الموردين.", "مرفوض", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var confirm = MessageBox.Show(
            $"هل تريد حذف المورد «{selected.Name}»؟",
            "تأكيد الحذف",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes) return;

        try
        {
            await ApiClient.Instance.DeleteAsync($"suppliers/{selected.Id}");
            await LoadDataAsync();
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

    // ============ Row Model ============

    public class SupplierRow
    {
        public int Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
    }

    // ============ Edit Dialog ============

    private class SupplierEditDialog : Form
    {
        private TextBox _txtCode = null!;
        private TextBox _txtName = null!;
        private TextBox _txtPhone = null!;
        private TextBox _txtEmail = null!;
        private TextBox _txtAddress = null!;
        private Label _lblStatus = null!;

        public object Result { get; private set; } = new { };

        public SupplierEditDialog(SupplierRow? existing)
        {
            BuildUi(existing);
        }

        private void BuildUi(SupplierRow? existing)
        {
            Text = existing == null ? "إضافة مورد جديد" : $"تعديل المورد — {existing.Name}";
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(480, 400);
            RightToLeft = RightToLeft.Yes;
            RightToLeftLayout = true;
            Font = new Font("Segoe UI", 10F);
            BackColor = Color.WhiteSmoke;

            int y = 20;
            const int labelX = 30;
            const int fieldX = 150;
            const int fieldW = 300;

            _txtCode = AddField("الكود:", ref y, labelX, fieldX, fieldW, existing?.Code ?? "");
            _txtName = AddField("الاسم:", ref y, labelX, fieldX, fieldW, existing?.Name ?? "");
            _txtPhone = AddField("الهاتف:", ref y, labelX, fieldX, fieldW, existing?.Phone ?? "");
            _txtEmail = AddField("الإيميل:", ref y, labelX, fieldX, fieldW, existing?.Email ?? "");
            _txtAddress = AddMultilineField("العنوان:", ref y, labelX, fieldX, fieldW, existing?.Address ?? "", 80);

            _lblStatus = new Label
            {
                Location = new Point(30, y + 15),
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
                Location = new Point(300, y + 50),
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
                Location = new Point(160, y + 50),
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            btnCancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

            AcceptButton = btnSave;
            CancelButton = btnCancel;

            Controls.AddRange(new Control[]
            {
                _txtCode, _txtName, _txtPhone, _txtEmail, _txtAddress,
                _lblStatus, btnSave, btnCancel
            });
        }

        private TextBox AddField(string label, ref int y, int labelX, int fieldX, int fieldW, string value)
        {
            Controls.Add(new Label { Text = label, Location = new Point(labelX, y + 5), AutoSize = true });
            var tb = new TextBox
            {
                Location = new Point(fieldX, y),
                Width = fieldW,
                Text = value
            };
            y += 42;
            return tb;
        }

        private TextBox AddMultilineField(string label, ref int y, int labelX, int fieldX, int fieldW, string value, int height)
        {
            Controls.Add(new Label { Text = label, Location = new Point(labelX, y + 5), AutoSize = true });
            var tb = new TextBox
            {
                Location = new Point(fieldX, y),
                Width = fieldW,
                Height = height,
                Multiline = true,
                Text = value,
                ScrollBars = ScrollBars.Vertical
            };
            y += height + 12;
            return tb;
        }

        private void OnSave(SupplierRow? existing)
        {
            var code = _txtCode.Text.Trim();
            var name = _txtName.Text.Trim();

            if (string.IsNullOrWhiteSpace(code)) { _lblStatus.Text = "الكود مطلوب."; return; }
            if (string.IsNullOrWhiteSpace(name)) { _lblStatus.Text = "الاسم مطلوب."; return; }

            if (existing == null)
            {
                Result = new
                {
                    code,
                    name,
                    phone = NullIfEmpty(_txtPhone.Text),
                    email = NullIfEmpty(_txtEmail.Text),
                    address = NullIfEmpty(_txtAddress.Text)
                };
            }
            else
            {
                Result = new
                {
                    id = existing.Id,
                    code,
                    name,
                    phone = NullIfEmpty(_txtPhone.Text),
                    email = NullIfEmpty(_txtEmail.Text),
                    address = NullIfEmpty(_txtAddress.Text)
                };
            }

            DialogResult = DialogResult.OK;
            Close();
        }

        private static string? NullIfEmpty(string s)
            => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    }
}