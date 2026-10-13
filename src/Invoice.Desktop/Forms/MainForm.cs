using Invoice.Desktop.Services;
using Invoice.Desktop.UserControls;

namespace Invoice.Desktop.Forms;

/// <summary>
/// النافذة الرئيسية — تحتوي على قائمة جانبية ومنطقة محتوى.
/// </summary>
public class MainForm : Form
{
    private Panel _contentPanel = null!;
    private FlowLayoutPanel _sidePanel = null!;
    private Label _lblPageTitle = null!;
    private Label _lblUserInfo = null!;
    private StatusStrip _statusStrip = null!;
    private ToolStripStatusLabel _statusLabel = null!;

    private readonly Dictionary<string, Button> _navButtons = new();
    private string? _currentPage = null;

    // مرجع لآخر InvoiceUserControl مُنشَأ (لتفعيل الفتح البرمجي من الأرشيف/لوحة التحكم)
    private InvoiceUserControl? _currentInvoiceControl;

    public MainForm()
    {
        BuildUi();
        HookSessionEvents();
        Navigate("Dashboard");
    }

    private void BuildUi()
    {
        Text = "نظام الفواتير — CostProfitApp";
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(1000, 650);
        StartPosition = FormStartPosition.CenterScreen;
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.WhiteSmoke;

        // ============ الشريط العلوي ============
        var topPanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.FromArgb(30, 60, 120)
        };

        var lblApp = new Label
        {
            Text = "نظام الفواتير",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 14F, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(20, 16)
        };

        _lblUserInfo = new Label
        {
            Text = "",
            ForeColor = Color.WhiteSmoke,
            Font = new Font("Segoe UI", 10F),
            AutoSize = true,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };

        var btnLogout = new Button
        {
            Text = "تسجيل الخروج",
            Width = 120,
            Height = 32,
            BackColor = Color.FromArgb(180, 50, 50),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Anchor = AnchorStyles.Top | AnchorStyles.Right
        };
        btnLogout.FlatAppearance.BorderSize = 0;
        btnLogout.Click += (_, _) => OnLogoutClicked();

        topPanel.Controls.Add(lblApp);
        topPanel.Controls.Add(_lblUserInfo);
        topPanel.Controls.Add(btnLogout);

        topPanel.Resize += (_, _) =>
        {
            btnLogout.Location = new Point(topPanel.Width - btnLogout.Width - 15, 14);
            _lblUserInfo.Location = new Point(
                btnLogout.Left - _lblUserInfo.Width - 20, 22);
        };

        // ============ القائمة الجانبية ============
        _sidePanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Right,
            Width = 200,
            BackColor = Color.FromArgb(45, 55, 75),
            Padding = new Padding(0, 15, 0, 0),
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoScroll = true
        };

        AddNavButton("Dashboard", "لوحة التحكم", "reports");
        AddNavButton("Suppliers", "الموردون", "suppliers");
        AddNavButton("Products", "الأصناف", "products");
        AddNavButton("Invoice", "فاتورة جديدة", "invoices");
        AddNavButton("Archive", "أرشيف الفواتير", "invoices");
        AddNavButton("Reports", "التقارير", "reports");
        AddNavButton("Settings", "الإعدادات", "settings");

        // ============ منطقة المحتوى ============
        _contentPanel = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(15)
        };

        var titlePanel = new Panel
        {
            Dock = DockStyle.Top,
            Height = 50,
            BackColor = Color.White
        };
        _lblPageTitle = new Label
        {
            Text = "",
            Font = new Font("Segoe UI", 15F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 60, 120),
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 10, 0)
        };
        titlePanel.Controls.Add(_lblPageTitle);

        var contentHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Name = "ContentHost"
        };

        _contentPanel.Controls.Add(contentHost);
        _contentPanel.Controls.Add(titlePanel);

        // ============ شريط الحالة ============
        _statusStrip = new StatusStrip
        {
            BackColor = Color.FromArgb(240, 240, 240),
            SizingGrip = false
        };
        _statusLabel = new ToolStripStatusLabel("جاهز")
        {
            Spring = true,
            TextAlign = ContentAlignment.MiddleRight
        };
        _statusStrip.Items.Add(_statusLabel);

        // ============ التركيب ============
        Controls.Add(_contentPanel);
        Controls.Add(_sidePanel);
        Controls.Add(topPanel);
        Controls.Add(_statusStrip);

        UpdateUserInfo();
    }

    private void AddNavButton(string key, string text, string permissionPrefix)
    {
        var btn = new Button
        {
            Text = text,
            Width = 200,
            Height = 48,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(45, 55, 75),
            ForeColor = Color.WhiteSmoke,
            Font = new Font("Segoe UI", 10F),
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 15, 0),
            Cursor = Cursors.Hand,
            Margin = new Padding(0),
            Tag = permissionPrefix
        };
        btn.FlatAppearance.BorderSize = 0;
        btn.FlatAppearance.MouseOverBackColor = Color.FromArgb(60, 75, 100);
        btn.Click += (_, _) => Navigate(key);
        _navButtons[key] = btn;
        _sidePanel.Controls.Add(btn);
    }

    private void UpdateUserInfo()
    {
        var user = ApiClient.Instance.CurrentUser;
        if (user == null)
        {
            _lblUserInfo.Text = "(غير مُسجَّل)";
            return;
        }

        var role = user.Roles.FirstOrDefault() ?? "—";
        _lblUserInfo.Text = $"{user.FullName}  |  {role}";
        _lblUserInfo.AutoSize = true;

        foreach (var (_, btn) in _navButtons)
        {
            var prefix = btn.Tag as string;
            if (string.IsNullOrEmpty(prefix)) { btn.Visible = true; continue; }

            btn.Visible =
                ApiClient.Instance.HasPermission(prefix + ".read") ||
                ApiClient.Instance.HasPermission(prefix + ".create") ||
                ApiClient.Instance.HasPermission(prefix + ".update") ||
                ApiClient.Instance.HasPermission(prefix + ".archive");
        }
    }

    private void Navigate(string pageKey)
    {
        _currentPage = pageKey;

        foreach (var (key, btn) in _navButtons)
        {
            btn.BackColor = key == pageKey
                ? Color.FromArgb(30, 60, 120)
                : Color.FromArgb(45, 55, 75);
        }

        ShowPage(pageKey);
    }

    private void ShowPage(string pageKey)
    {
        var titles = new Dictionary<string, string>
        {
            ["Dashboard"] = "لوحة التحكم",
            ["Suppliers"] = "الموردون",
            ["Products"] = "الأصناف",
            ["Invoice"] = "فاتورة جديدة",
            ["Archive"] = "أرشيف الفواتير",
            ["Reports"] = "التقارير",
            ["Settings"] = "الإعدادات"
        };

        _lblPageTitle.Text = titles.TryGetValue(pageKey, out var t) ? t : pageKey;

        var host = _contentPanel.Controls["ContentHost"];
        if (host == null) return;

        host.SuspendLayout();
        host.Controls.Clear();

        Control? page = pageKey switch
        {
            "Dashboard" => CreateDashboardPage(),
            "Suppliers" => new SuppliersUserControl(),
            "Products" => new ProductsUserControl(),
            "Invoice" => CreateInvoicePage(),
            "Archive" => CreateArchivePage(),
            _ => BuildPlaceholder($"صفحة «{_lblPageTitle.Text}» قيد الإنشاء.\nستُضاف في الخطوات القادمة.")
        };

        if (page != null)
        {
            page.Dock = DockStyle.Fill;
            host.Controls.Add(page);
        }

        host.ResumeLayout();

        SetStatus($"الصفحة الحالية: {_lblPageTitle.Text}");
    }

    /// <summary>ينشئ صفحة الفاتورة ويحفظ مرجعًا لها (للتفعيل البرمجي).</summary>
    private Control CreateInvoicePage()
    {
        _currentInvoiceControl = new InvoiceUserControl();
        return _currentInvoiceControl;
    }

    /// <summary>ينشئ صفحة الأرشيف ويشترك في حدث فتح فاتورة.</summary>
    private Control CreateArchivePage()
    {
        var archive = new ArchiveUserControl();
        archive.InvoiceOpenRequested += OnInvoiceOpenRequested;
        return archive;
    }

    /// <summary>ينشئ لوحة التحكم ويشترك في حدث فتح فاتورة (بند 2).</summary>
    private Control CreateDashboardPage()
    {
        var dashboard = new DashboardUserControl();
        dashboard.InvoiceOpenRequested += OnInvoiceOpenRequested;
        return dashboard;
    }

    /// <summary>عند النقر المزدوج على فاتورة (في الأرشيف أو لوحة التحكم): انتقل لشاشة الفاتورة وافتحها.</summary>
    private void OnInvoiceOpenRequested(long id)
    {
        Navigate("Invoice");
        _currentInvoiceControl?.RequestOpenInvoice(id);
    }

    private static Control BuildPlaceholder(string text)
    {
        return new Label
        {
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 12F),
            ForeColor = Color.Gray
        };
    }

    private void SetStatus(string text) => _statusLabel.Text = text;

    private void HookSessionEvents()
    {
        ApiClient.Instance.SessionExpired += OnSessionExpired;
    }

    private void OnSessionExpired()
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action(OnSessionExpired));
            return;
        }

        MessageBox.Show(
            "انتهت الجلسة. الرجاء تسجيل الدخول مرة أخرى.",
            "انتهت الجلسة",
            MessageBoxButtons.OK,
            MessageBoxIcon.Warning);

        DialogResult = DialogResult.Retry;
        Close();
    }

    private void OnLogoutClicked()
    {
        var result = MessageBox.Show(
            "هل تريد تسجيل الخروج؟",
            "تأكيد",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (result != DialogResult.Yes) return;

        ApiClient.Instance.Logout();
        DialogResult = DialogResult.Retry;
        Close();
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        ApiClient.Instance.SessionExpired -= OnSessionExpired;
        base.OnFormClosed(e);
    }
}