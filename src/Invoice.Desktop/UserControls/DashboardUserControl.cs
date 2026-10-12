using System.Text.Json;
using Invoice.Desktop.Services;

namespace Invoice.Desktop.UserControls;

/// <summary>
/// لوحة التحكم — تعرض KPIs من /api/reports/dashboard.
/// </summary>
public class DashboardUserControl : UserControl
{
    private ComboBox _cmbPeriod = null!;
    private Button _btnRefresh = null!;
    private Label _lblStatus = null!;
    private FlowLayoutPanel _cardsPanel = null!;

    public DashboardUserControl()
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
        _cmbPeriod.SelectedIndex = 4; // السنة
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

        Controls.Add(_cardsPanel);
        Controls.Add(_lblStatus);
        Controls.Add(toolbar);
    }

    private async Task LoadDataAsync()
    {
        _cardsPanel.Controls.Clear();
        _lblStatus.Text = "جارٍ التحميل...";
        _lblStatus.ForeColor = Color.DimGray;

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

            _lblStatus.Text = $"آخر تحديث: {DateTime.Now:HH:mm:ss}";
            _lblStatus.ForeColor = Color.SeaGreen;
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "خطأ: " + ex.Message;
            _lblStatus.ForeColor = Color.Firebrick;
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

    // ============ استخراج القيم من JsonElement ============

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