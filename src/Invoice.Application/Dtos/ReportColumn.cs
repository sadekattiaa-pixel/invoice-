namespace Invoice.Application.Dtos;

/// <summary>
/// تعريف عمود واحد في جدول تقرير.
/// </summary>
public class ReportColumn
{
    /// <summary>عنوان العمود (يظهر في رأس الجدول).</summary>
    public string Header { get; set; } = "";

    /// <summary>هل تُحاذى قيم هذا العمود لليمين؟ (للأرقام عادة).</summary>
    public bool AlignRight { get; set; } = false;

    /// <summary>هل يُعرض العنوان والقيم بخط عريض؟</summary>
    public bool Bold { get; set; } = false;

    /// <summary>عرض نسبي للعمود (1 = عادي، 2 = ضعف، ...). يُستخدم في PDF.</summary>
    public int Weight { get; set; } = 1;
}