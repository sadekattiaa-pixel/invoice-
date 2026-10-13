using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace Invoice.Desktop.Shared;

/// <summary>
/// نافذة معاينة PDF — تعرض الملف داخل WebView2 مع أزرار "حفظ في..." و"إلغاء".
/// تُستخدم قبل حفظ ملف PDF المُصدَّر من الخادم (بند 16).
/// </summary>
public class PdfPreviewForm : Form
{
    private WebView2 _webView = null!;
    private Button _btnSaveAs = null!;
    private Button _btnCancel = null!;
    private Label _lblStatus = null!;

    private readonly byte[] _pdfBytes;
    private readonly string _suggestedFileName;
    private string? _tempFilePath;
    private bool _saved;

    private PdfPreviewForm(byte[] pdfBytes, string suggestedFileName)
    {
        _pdfBytes = pdfBytes;
        _suggestedFileName = suggestedFileName;
        BuildUi();
        Load += async (_, _) => await InitializeAsync();
    }

    /// <summary>
    /// يفتح نافذة المعاينة.
    /// يُعيد true إذا ضغط المستخدم "حفظ في..." (وتم الحفظ فعلًا).
    /// ويُعيد false إذا ألغى.
    /// </summary>
    public static bool ShowPreview(IWin32Window? owner, byte[] pdfBytes, string suggestedFileName)
    {
        using var dlg = new PdfPreviewForm(pdfBytes, suggestedFileName);
        var result = owner == null ? dlg.ShowDialog() : dlg.ShowDialog(owner);
        return result == DialogResult.OK && dlg._saved;
    }

    private void BuildUi()
    {
        Text = "معاينة PDF — قبل الحفظ";
        StartPosition = FormStartPosition.CenterParent;
        WindowState = FormWindowState.Maximized;
        MinimumSize = new Size(900, 650);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = false; // WebView2 لا يفضل RTL
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.WhiteSmoke;

        // ============ WebView2 (يملأ) ============
        _webView = new WebView2
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White
        };

        // ============ شريط الحالة السفلي ============
        _lblStatus = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 26,
            Text = "جارٍ تحضير المعاينة...",
            TextAlign = ContentAlignment.MiddleRight,
            Padding = new Padding(0, 0, 12, 0),
            ForeColor = Color.DimGray,
            Font = new Font("Segoe UI", 9F),
            BackColor = Color.FromArgb(245, 245, 250)
        };

        // ============ شريط الأزرار ============
        var btnPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 60,
            BackColor = Color.WhiteSmoke,
            Padding = new Padding(10)
        };

        _btnSaveAs = new Button
        {
            Text = "حفظ في...",
            Width = 150,
            Height = 40,
            Location = new Point(btnPanel.Width - 170, 10),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            BackColor = Color.FromArgb(46, 160, 67),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };
        _btnSaveAs.FlatAppearance.BorderSize = 0;
        _btnSaveAs.Click += async (_, _) => await OnSaveAsAsync();

        _btnCancel = new Button
        {
            Text = "إلغاء",
            Width = 130,
            Height = 40,
            Location = new Point(btnPanel.Width - 320, 10),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand,
            Font = new Font("Segoe UI", 10F)
        };
        _btnCancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        btnPanel.Resize += (_, _) =>
        {
            _btnSaveAs.Location = new Point(btnPanel.Width - 170, 10);
            _btnCancel.Location = new Point(btnPanel.Width - 320, 10);
        };

        btnPanel.Controls.Add(_btnSaveAs);
        btnPanel.Controls.Add(_btnCancel);

        Controls.Add(_webView);
        Controls.Add(_lblStatus);
        Controls.Add(btnPanel);

        CancelButton = _btnCancel;
    }

    private async Task InitializeAsync()
    {
        try
        {
            // مجلد بيانات WebView2 خاص بتطبيقنا (أكثر أمانًا من الافتراضي).
            var userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CostProfitApp", "Desktop", "WebView2");
            Directory.CreateDirectory(userDataFolder);

            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await _webView.EnsureCoreWebView2Async(env);

            // كتابة الـ PDF إلى ملف مؤقت ثم عرضه.
            _tempFilePath = Path.Combine(
                Path.GetTempPath(),
                $"invoice_preview_{Guid.NewGuid():N}.pdf");
            await File.WriteAllBytesAsync(_tempFilePath, _pdfBytes);

            _webView.CoreWebView2.Navigate(new Uri(_tempFilePath).AbsoluteUri);
            _lblStatus.Text = "معاينة PDF جاهزة. اضغط «حفظ في...» للحفظ أو «إلغاء» للإغلاق.";
            _lblStatus.ForeColor = Color.SeaGreen;
        }
        catch (Exception ex)
        {
            _lblStatus.Text = "فشل عرض المعاينة: " + ex.Message;
            _lblStatus.ForeColor = Color.Firebrick;
            _btnSaveAs.Enabled = false;
            MessageBox.Show(
                "تعذّر تشغيل عارض PDF المدمج (WebView2).\n" +
                "تأكد من تثبيت WebView2 Runtime على الجهاز.\n\n" +
                "يمكنك الحفظ مباشرة باستخدام الزر أدناه أو إلغاء العملية.\n\n" +
                "التفاصيل: " + ex.Message,
                "تعذّر تشغيل المعاينة",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
        }
    }

    private async Task OnSaveAsAsync()
    {
        using var sfd = new SaveFileDialog
        {
            Filter = "PDF Files (*.pdf)|*.pdf",
            FileName = _suggestedFileName,
            OverwritePrompt = true
        };
        if (sfd.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            await File.WriteAllBytesAsync(sfd.FileName, _pdfBytes);
            _saved = true;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show("فشل حفظ الملف: " + ex.Message,
                "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        // تنظيف الملف المؤقت
        try
        {
            if (!string.IsNullOrEmpty(_tempFilePath) && File.Exists(_tempFilePath))
                File.Delete(_tempFilePath);
        }
        catch
        {
            // نتجاهل — ملفات temp تُنظَّف دوريًا.
        }

        base.OnFormClosed(e);
    }
}