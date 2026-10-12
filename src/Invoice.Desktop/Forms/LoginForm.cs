using Invoice.Desktop.Services;

namespace Invoice.Desktop.Forms;

/// <summary>
/// نافذة تسجيل الدخول — تتصل بـ /api/auth/login عبر ApiClient.
/// عند النجاح: DialogResult = OK، ثم يُفتح MainForm.
/// </summary>
public class LoginForm : Form
{
    private TextBox _txtUsername = null!;
    private TextBox _txtPassword = null!;
    private Button _btnLogin = null!;
    private Button _btnCancel = null!;
    private Label _lblStatus = null!;
    private Label _lblServer = null!;
    private TextBox _txtServer = null!;
    private CheckBox _chkShowPassword = null!;

    public LoginForm()
    {
        BuildUi();
    }

    private void BuildUi()
    {
        Text = "تسجيل الدخول — نظام الفواتير";
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        ClientSize = new Size(420, 360);
        RightToLeft = RightToLeft.Yes;
        RightToLeftLayout = true;
        Font = new Font("Segoe UI", 10F);
        BackColor = Color.WhiteSmoke;

        // العنوان
        var lblTitle = new Label
        {
            Text = "نظام الفواتير",
            Font = new Font("Segoe UI", 16F, FontStyle.Bold),
            ForeColor = Color.FromArgb(30, 60, 120),
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 60,
            Padding = new Padding(0, 15, 0, 0)
        };

        var lblSubtitle = new Label
        {
            Text = "الرجاء تسجيل الدخول للمتابعة",
            ForeColor = Color.Gray,
            TextAlign = ContentAlignment.MiddleCenter,
            Dock = DockStyle.Top,
            Height = 25
        };

        // الحقول
        var lblUser = new Label { Text = "اسم المستخدم:", Location = new Point(30, 110), AutoSize = true };
        _txtUsername = new TextBox
        {
            Location = new Point(140, 107),
            Width = 240,
            Text = "admin"
        };

        var lblPass = new Label { Text = "كلمة المرور:", Location = new Point(30, 150), AutoSize = true };
        _txtPassword = new TextBox
        {
            Location = new Point(140, 147),
            Width = 240,
            UseSystemPasswordChar = true,
            Text = "admin123"
        };

        _chkShowPassword = new CheckBox
        {
            Text = "إظهار كلمة المرور",
            Location = new Point(140, 175),
            AutoSize = true,
            ForeColor = Color.DimGray
        };
        _chkShowPassword.CheckedChanged += (_, _) =>
        {
            _txtPassword.UseSystemPasswordChar = !_chkShowPassword.Checked;
        };

        // عنوان الخادم
        var lblSrv = new Label { Text = "عنوان الخادم:", Location = new Point(30, 210), AutoSize = true };
        _txtServer = new TextBox
        {
            Location = new Point(140, 207),
            Width = 240,
            Text = ApiClient.Instance.BaseUrl
        };
        _lblServer = new Label
        {
            Text = "مثال: http://localhost:5124/api",
            Location = new Point(140, 230),
            AutoSize = true,
            ForeColor = Color.Gray,
            Font = new Font("Segoe UI", 8F)
        };

        // رسالة الحالة
        _lblStatus = new Label
        {
            Location = new Point(30, 260),
            Width = 360,
            Height = 30,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Color.Firebrick,
            Font = new Font("Segoe UI", 9F)
        };

        // الأزرار
        _btnLogin = new Button
        {
            Text = "تسجيل الدخول",
            Location = new Point(200, 300),
            Width = 180,
            Height = 38,
            BackColor = Color.FromArgb(30, 100, 200),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 10F, FontStyle.Bold),
            Cursor = Cursors.Hand
        };
        _btnLogin.FlatAppearance.BorderSize = 0;
        _btnLogin.Click += async (_, _) => await OnLoginAsync();

        _btnCancel = new Button
        {
            Text = "إلغاء",
            Location = new Point(30, 300),
            Width = 120,
            Height = 38,
            FlatStyle = FlatStyle.Flat,
            Cursor = Cursors.Hand
        };
        _btnCancel.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        AcceptButton = _btnLogin;
        CancelButton = _btnCancel;

        Controls.AddRange(new Control[]
        {
            lblTitle, lblSubtitle,
            lblUser, _txtUsername,
            lblPass, _txtPassword, _chkShowPassword,
            lblSrv, _txtServer, _lblServer,
            _lblStatus,
            _btnLogin, _btnCancel
        });
    }

    private async Task OnLoginAsync()
    {
        var username = _txtUsername.Text.Trim();
        var password = _txtPassword.Text;
        var server = _txtServer.Text.Trim();

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            _lblStatus.Text = "الرجاء إدخال اسم المستخدم وكلمة المرور.";
            return;
        }
        if (string.IsNullOrWhiteSpace(server))
        {
            _lblStatus.Text = "الرجاء إدخال عنوان الخادم.";
            return;
        }

        SetBusy(true);
        _lblStatus.ForeColor = Color.DimGray;
        _lblStatus.Text = "جارٍ الاتصال بالخادم...";

        try
        {
            ApiClient.Instance.BaseUrl = server;
            await ApiClient.Instance.LoginAsync(username, password);

            _lblStatus.ForeColor = Color.SeaGreen;
            _lblStatus.Text = "تم تسجيل الدخول بنجاح.";
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (ApiException ex)
        {
            _lblStatus.ForeColor = Color.Firebrick;
            _lblStatus.Text = ex.Message;
        }
        catch (HttpRequestException)
        {
            _lblStatus.ForeColor = Color.Firebrick;
            _lblStatus.Text = "تعذّر الاتصال بالخادم. تأكد من تشغيله ومن عنوان الخادم.";
        }
        catch (Exception ex)
        {
            _lblStatus.ForeColor = Color.Firebrick;
            _lblStatus.Text = "خطأ غير متوقع: " + ex.Message;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void SetBusy(bool busy)
    {
        _btnLogin.Enabled = !busy;
        _btnCancel.Enabled = !busy;
        _txtUsername.Enabled = !busy;
        _txtPassword.Enabled = !busy;
        _txtServer.Enabled = !busy;
        _chkShowPassword.Enabled = !busy;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }
}