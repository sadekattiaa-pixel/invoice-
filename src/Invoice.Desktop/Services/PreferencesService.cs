using System.Text.Json;

namespace Invoice.Desktop.Services;

/// <summary>
/// خدمة تفضيلات المستخدم — تُحفظ محليًا في ملف JSON.
/// المسار: %LOCALAPPDATA%\CostProfitApp\Desktop\preferences.json
/// Singleton — استخدم PreferencesService.Instance.
/// </summary>
public sealed class PreferencesService
{
    private static readonly Lazy<PreferencesService> _instance = new(() => new PreferencesService());
    public static PreferencesService Instance => _instance.Value;

    private readonly string _filePath;
    private readonly object _lock = new();
    private PreferencesData _data;

    private PreferencesService()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "CostProfitApp", "Desktop");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "preferences.json");
        _data = Load();
    }

    /// <summary>البيانات الحالية (قراءة فقط).</summary>
    public PreferencesData Data
    {
        get { lock (_lock) { return _data; } }
    }

    // ============================================================
    // Hidden Columns
    // ============================================================

    /// <summary>هل العمود مخفي في الجدول المحدَّد؟</summary>
    public bool IsColumnHidden(string gridKey, string columnName)
    {
        lock (_lock)
        {
            if (!_data.HiddenColumns.TryGetValue(gridKey, out var set)) return false;
            return set.Contains(columnName);
        }
    }

    /// <summary>إخفاء عمود.</summary>
    public void HideColumn(string gridKey, string columnName)
    {
        lock (_lock)
        {
            if (!_data.HiddenColumns.TryGetValue(gridKey, out var set))
            {
                set = new HashSet<string>();
                _data.HiddenColumns[gridKey] = set;
            }
            set.Add(columnName);
        }
        Save();
    }

    /// <summary>إظهار عمود.</summary>
    public void ShowColumn(string gridKey, string columnName)
    {
        lock (_lock)
        {
            if (_data.HiddenColumns.TryGetValue(gridKey, out var set))
            {
                set.Remove(columnName);
                if (set.Count == 0) _data.HiddenColumns.Remove(gridKey);
            }
        }
        Save();
    }

    /// <summary>تعيين حالة عمود (إظهار/إخفاء) حسب قيمة bool.</summary>
    public void SetColumnVisible(string gridKey, string columnName, bool visible)
    {
        if (visible) ShowColumn(gridKey, columnName);
        else HideColumn(gridKey, columnName);
    }

    // ============================================================
    // Column Widths (بند 12)
    // ============================================================

    /// <summary>
    /// يُرجِع العرض المحفوظ للعمود، أو null إن لم يُحفظ عرض من قبل.
    /// </summary>
    public int? GetColumnWidth(string gridKey, string columnName)
    {
        lock (_lock)
        {
            if (!_data.ColumnWidths.TryGetValue(gridKey, out var map)) return null;
            if (!map.TryGetValue(columnName, out var width)) return null;
            return width > 0 ? width : null;
        }
    }

    /// <summary>يحفظ عرض العمود.</summary>
    public void SetColumnWidth(string gridKey, string columnName, int width)
    {
        if (width <= 0) return;
        lock (_lock)
        {
            if (!_data.ColumnWidths.TryGetValue(gridKey, out var map))
            {
                map = new Dictionary<string, int>();
                _data.ColumnWidths[gridKey] = map;
            }
            map[columnName] = width;
        }
        Save();
    }

    // ============================================================
    // Font Size
    // ============================================================

    /// <summary>حجم الخط الحالي للتطبيق (بالبوينت).</summary>
    public float FontSize
    {
        get { lock (_lock) { return _data.FontSize; } }
        set
        {
            lock (_lock) { _data.FontSize = value; }
            Save();
        }
    }

    // ============================================================
    // Dark Mode
    // ============================================================

    /// <summary>هل الوضع الداكن مُفعَّل؟</summary>
    public bool DarkMode
    {
        get { lock (_lock) { return _data.DarkMode; } }
        set
        {
            lock (_lock) { _data.DarkMode = value; }
            Save();
        }
    }

    // ============================================================
    // Internals
    // ============================================================

    private PreferencesData Load()
    {
        try
        {
            if (!File.Exists(_filePath)) return new PreferencesData();
            var json = File.ReadAllText(_filePath);
            var obj = JsonSerializer.Deserialize<PreferencesData>(json);
            return obj ?? new PreferencesData();
        }
        catch
        {
            return new PreferencesData();
        }
    }

    private void Save()
    {
        try
        {
            PreferencesData snapshot;
            lock (_lock) { snapshot = _data; }
            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // نتجاهل أخطاء الحفظ — لن نُعطّل الواجهة.
        }
    }
}

/// <summary>بنية بيانات التفضيلات.</summary>
public class PreferencesData
{
    /// <summary>حجم الخط الحالي (افتراضي 10).</summary>
    public float FontSize { get; set; } = 10f;

    /// <summary>هل الوضع الداكن مُفعَّل؟</summary>
    public bool DarkMode { get; set; } = false;

    /// <summary>الأعمدة المخفية — مفتاح: معرّف الجدول، قيمة: أسماء الأعمدة.</summary>
    public Dictionary<string, HashSet<string>> HiddenColumns { get; set; } = new();

    /// <summary>
    /// عروض الأعمدة المحفوظة — مفتاح: معرّف الجدول، قيمة: قاموس (اسم العمود → العرض).
    /// </summary>
    public Dictionary<string, Dictionary<string, int>> ColumnWidths { get; set; } = new();
}