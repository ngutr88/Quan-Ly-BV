namespace QuanLyBenhVien.Helpers;

/// <summary>
/// Fixed, ordered sidebar group keys for the Doctor area menu, so render
/// order is guaranteed regardless of the order items are declared in
/// <see cref="DoctorMenuRegistry"/>.
/// </summary>
public static class DoctorMenuGroups
{
    public const string CongViecHangNgay = "CongViecHangNgay";
    public const string HoSoChuyenMon = "HoSoChuyenMon";
    public const string LichVaGiaoTiep = "LichVaGiaoTiep";
    public const string CaNhan = "CaNhan";

    public static readonly IReadOnlyList<(string Key, string Label)> Ordered = new (string, string)[]
    {
        (CongViecHangNgay, "Công việc hàng ngày"),
        (HoSoChuyenMon, "Hồ sơ & chuyên môn"),
        (LichVaGiaoTiep, "Lịch & giao tiếp"),
        (CaNhan, "Cá nhân")
    };
}
