using Invoice.Desktop.Forms;

namespace Invoice.Desktop;

internal static class Program
{
    /// <summary>
    /// نقطة الدخول — تُشغّل حلقة: LoginForm → MainForm.
    /// عند تسجيل الخروج أو انتهاء الجلسة تُعاد الحلقة إلى LoginForm.
    /// عند إلغاء تسجيل الدخول يخرج التطبيق.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        while (true)
        {
            using var login = new LoginForm();
            if (login.ShowDialog() != DialogResult.OK)
                return; // إلغاء تسجيل الدخول → خروج

            using var main = new MainForm();
            var result = main.ShowDialog();

            // Retry = تسجيل خروج أو انتهاء جلسة → نُعيد الحلقة
            if (result != DialogResult.Retry)
                return;
        }
    }
}