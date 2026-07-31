namespace QuanLyBenhVien.Helpers;

/// <summary>
/// Fixed, ordered sidebar group keys for the Patient area menu - mirrors
/// DoctorMenuGroups.
/// </summary>
public static class PatientMenuGroups
{
    public const string HayDung = "HayDung";
    public const string HoSo = "HoSo";
    public const string Khac = "Khac";

    public static readonly IReadOnlyList<(string Key, string Label)> Ordered = new (string, string)[]
    {
        (HayDung, "Hay dùng"),
        (HoSo, "Hồ sơ"),
        (Khac, "Khác")
    };
}
