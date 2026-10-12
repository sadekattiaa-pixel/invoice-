using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Invoice.Desktop.Services;

/// <summary>
/// عميل HTTP مركزي لكل الاتصالات بالـ API.
/// - يخزّن Access + Refresh Tokens في الذاكرة.
/// - يضيف ترويسة Bearer تلقائيًا.
/// - يُجدّد التوكن تلقائيًا عند 401 (مرة واحدة).
/// - يُطلق حدث SessionExpired عند فشل التجديد.
/// Singleton — استخدم ApiClient.Instance.
/// </summary>
public sealed class ApiClient
{
    private static readonly Lazy<ApiClient> _instance = new(() => new ApiClient());
    public static ApiClient Instance => _instance.Value;

    private readonly HttpClient _http;
    private readonly object _lock = new();

    /// <summary>عنوان الـ API الأساسي (بدون / في النهاية).</summary>
    public string BaseUrl { get; set; } = "http://localhost:5124/api";

    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public UserInfoDto? CurrentUser { get; private set; }
    public DateTime AccessTokenExpiresAt { get; private set; } = DateTime.MinValue;

    public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken) && CurrentUser != null;

    /// <summary>يُطلق عندما ينتهي التوكن ويفشل التجديد (يجب إعادة تسجيل الدخول).</summary>
    public event Action? SessionExpired;

    private ApiClient()
    {
        _http = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(120)
        };
    }

    // ============================================================
    // Authentication
    // ============================================================

    public async Task LoginAsync(string username, string password)
    {
        var body = new { username, password };
        using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl.TrimEnd('/')}/auth/login")
        {
            Content = JsonContent.Create(body)
        };
        using var resp = await _http.SendAsync(req);
        if (!resp.IsSuccessStatusCode)
        {
            var err = await ReadErrorAsync(resp);
            throw new ApiException(err, resp.StatusCode);
        }
        var login = await resp.Content.ReadFromJsonAsync<LoginResponse>()
            ?? throw new ApiException("استجابة تسجيل دخول فارغة.", resp.StatusCode);
        SetSession(login);
    }

    public void Logout()
    {
        lock (_lock)
        {
            AccessToken = null;
            RefreshToken = null;
            CurrentUser = null;
            AccessTokenExpiresAt = DateTime.MinValue;
        }
    }

    public bool HasPermission(string permission)
        => CurrentUser?.Permissions.Contains(permission, StringComparer.OrdinalIgnoreCase) == true;

    private void SetSession(LoginResponse login)
    {
        lock (_lock)
        {
            AccessToken = login.AccessToken;
            RefreshToken = login.RefreshToken;
            CurrentUser = login.User;
            AccessTokenExpiresAt = login.AccessTokenExpiresAt.ToUniversalTime();
        }
    }

    private async Task<bool> TryRefreshAsync()
    {
        string? refresh;
        lock (_lock) { refresh = RefreshToken; }
        if (string.IsNullOrEmpty(refresh)) return false;

        try
        {
            var body = new { refreshToken = refresh };
            using var req = new HttpRequestMessage(HttpMethod.Post, $"{BaseUrl.TrimEnd('/')}/auth/refresh")
            {
                Content = JsonContent.Create(body)
            };
            using var resp = await _http.SendAsync(req);
            if (!resp.IsSuccessStatusCode) return false;

            var login = await resp.Content.ReadFromJsonAsync<LoginResponse>();
            if (login == null) return false;
            SetSession(login);
            return true;
        }
        catch
        {
            return false;
        }
    }

    // ============================================================
    // Generic helpers
    // ============================================================

    public async Task<T?> GetAsync<T>(string endpoint)
    {
        using var resp = await SendWithRefreshAsync(() => BuildRequest(HttpMethod.Get, endpoint));
        await EnsureSuccessAsync(resp);
        return await resp.Content.ReadFromJsonAsync<T>();
    }

    public async Task<HttpResponseMessage> PostAsync<T>(string endpoint, T body)
    {
        var resp = await SendWithRefreshAsync(() =>
        {
            var req = BuildRequest(HttpMethod.Post, endpoint);
            req.Content = JsonContent.Create(body);
            return req;
        });
        await EnsureSuccessAsync(resp);
        return resp;
    }

    public async Task<HttpResponseMessage> PutAsync<T>(string endpoint, T body)
    {
        var resp = await SendWithRefreshAsync(() =>
        {
            var req = BuildRequest(HttpMethod.Put, endpoint);
            req.Content = JsonContent.Create(body);
            return req;
        });
        await EnsureSuccessAsync(resp);
        return resp;
    }

    public async Task DeleteAsync(string endpoint)
    {
        using var resp = await SendWithRefreshAsync(() => BuildRequest(HttpMethod.Delete, endpoint));
        await EnsureSuccessAsync(resp);
    }

    /// <summary>
    /// تنزيل ملف (PDF/Excel) كبايتات.
    /// hiddenKeys: قائمة مفاتيح الأعمدة/الأقسام المخفية — تُرسل كـ ?hidden=a,b,c (اختياري).
    /// </summary>
    public async Task<byte[]> DownloadAsync(string endpoint, IEnumerable<string>? hiddenKeys = null)
    {
        var url = AppendHiddenQuery(endpoint, hiddenKeys);
        using var resp = await SendWithRefreshAsync(() => BuildRequest(HttpMethod.Get, url));
        await EnsureSuccessAsync(resp);
        return await resp.Content.ReadAsByteArrayAsync();
    }

    /// <summary>رفع ملف عبر multipart/form-data (يُستخدم لرفع الشعار).</summary>
    public async Task<HttpResponseMessage> PostMultipartAsync(
        string endpoint,
        string fieldName,
        string fileName,
        byte[] bytes,
        string contentType)
    {
        var resp = await SendWithRefreshAsync(() =>
        {
            var req = BuildRequest(HttpMethod.Post, endpoint);
            var content = new MultipartFormDataContent();
            var fileContent = new ByteArrayContent(bytes);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
            content.Add(fileContent, fieldName, fileName);
            req.Content = content;
            return req;
        });
        await EnsureSuccessAsync(resp);
        return resp;
    }

    // ============================================================
    // Internals
    // ============================================================

    /// <summary>يُضيف ?hidden=a,b,c إلى endpoint إن كانت هناك قائمة.</summary>
    private static string AppendHiddenQuery(string endpoint, IEnumerable<string>? hiddenKeys)
    {
        if (hiddenKeys == null) return endpoint;
        var list = hiddenKeys
            .Where(k => !string.IsNullOrWhiteSpace(k))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (list.Count == 0) return endpoint;

        var joined = Uri.EscapeDataString(string.Join(",", list));
        var sep = endpoint.Contains('?') ? "&" : "?";
        return $"{endpoint}{sep}hidden={joined}";
    }

    private HttpRequestMessage BuildRequest(HttpMethod method, string endpoint)
    {
        var url = endpoint.StartsWith("http", StringComparison.OrdinalIgnoreCase)
            ? endpoint
            : $"{BaseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";

        var req = new HttpRequestMessage(method, url);

        string? token;
        lock (_lock) { token = AccessToken; }

        if (!string.IsNullOrEmpty(token))
            req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return req;
    }

    private async Task<HttpResponseMessage> SendWithRefreshAsync(Func<HttpRequestMessage> requestFactory)
    {
        // المحاولة الأولى
        using (var req = requestFactory())
        {
            var resp = await _http.SendAsync(req);
            if (resp.StatusCode != HttpStatusCode.Unauthorized)
                return resp;
            resp.Dispose();
        }

        // 401 → نحاول تجديد التوكن مرة واحدة
        if (!await TryRefreshAsync())
        {
            Logout();
            SessionExpired?.Invoke();
            throw new ApiException("انتهت الجلسة. الرجاء تسجيل الدخول مرة أخرى.", HttpStatusCode.Unauthorized);
        }

        // إعادة المحاولة بالتوكن الجديد
        using var retryReq = requestFactory();
        return await _http.SendAsync(retryReq);
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage resp)
    {
        if (resp.IsSuccessStatusCode) return;
        var msg = await ReadErrorAsync(resp);
        throw new ApiException(msg, resp.StatusCode);
    }

    private static async Task<string> ReadErrorAsync(HttpResponseMessage resp)
    {
        try
        {
            var text = await resp.Content.ReadAsStringAsync();
            if (string.IsNullOrWhiteSpace(text)) return $"HTTP {(int)resp.StatusCode}";
            try
            {
                using var doc = JsonDocument.Parse(text);
                if (doc.RootElement.TryGetProperty("message", out var m))
                    return m.GetString() ?? $"HTTP {(int)resp.StatusCode}";
            }
            catch { /* ليست JSON */ }
            return text;
        }
        catch
        {
            return $"HTTP {(int)resp.StatusCode}";
        }
    }
}

// ============================================================
// Supporting types
// ============================================================

public sealed class ApiException : Exception
{
    public HttpStatusCode StatusCode { get; }

    public ApiException(string message, HttpStatusCode status) : base(message)
    {
        StatusCode = status;
    }
}

public class LoginResponse
{
    public string AccessToken { get; set; } = "";
    public string RefreshToken { get; set; } = "";
    public DateTime AccessTokenExpiresAt { get; set; }
    public UserInfoDto User { get; set; } = new();
}

public class UserInfoDto
{
    public int Id { get; set; }
    public string Username { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public List<string> Roles { get; set; } = new();
    public List<string> Permissions { get; set; } = new();
}