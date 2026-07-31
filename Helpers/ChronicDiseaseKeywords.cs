namespace QuanLyBenhVien.Helpers;

/// <summary>
/// Keyword list for the homepage follow-up-visit reminder heuristic - matched
/// diacritic/case-insensitively (VietnameseTextHelper.ContainsIgnoreCase)
/// against Patient.TienSuBenh free text. Approximate by nature (no structured
/// chronic-disease flag exists in the data model), same trade-off already
/// accepted for the "hệ ngoại" ChuyenKhoa heuristic on the Doctor sidebar.
/// </summary>
public static class ChronicDiseaseKeywords
{
    public static readonly string[] Keywords =
    {
        "tieu duong", "dai thao duong",
        "cao huyet ap", "tang huyet ap",
        "tim mach", "suy tim", "mach vanh",
        "hen suyen", "hen phe quan", "copd",
        "viem gan", "xo gan",
        "suy than",
        "gout", "gut"
    };
}
