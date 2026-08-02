using System.Collections.Generic;

namespace QuanLyBenhVien.Helpers;

/// <summary>
/// Single source of truth for the "Học hàm / Học vị" dropdown values - the
/// 5 values already in real use at Admin/Doctors/Edit.cshtml and stored in
/// <c>BacSi.HocVi</c>. Centralized so the Doctor self-service "đề xuất thay
/// đổi" form and the Admin direct-edit form never drift apart.
/// </summary>
public static class DoctorTitleCatalog
{
    public static readonly IReadOnlyList<(string Value, string Label)> Options = new List<(string, string)>
    {
        ("BS", "BS"),
        ("ThS.BS", "ThS.BS (Thạc sĩ Bác sĩ)"),
        ("TS.BS", "TS.BS (Tiến sĩ Bác sĩ)"),
        ("PGS.TS.BS", "PGS.TS.BS"),
        ("GS.TS.BS", "GS.TS.BS")
    };
}
