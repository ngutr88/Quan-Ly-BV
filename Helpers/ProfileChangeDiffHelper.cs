using System;
using System.Collections.Generic;
using QuanLyBenhVien.Models.ViewModels;

namespace QuanLyBenhVien.Helpers;

/// <summary>
/// Turns a pair of <see cref="ProfileChangeFields"/> snapshots into a
/// human-readable field-by-field diff, shared by the Doctor's own "Chờ duyệt"
/// badges (Areas/Doctor/Views/Profile/Index.cshtml) and the Admin diff table
/// (Areas/Admin/Views/ProfileApprovals/Details.cshtml) so the two never
/// disagree on labels or formatting.
/// </summary>
public static class ProfileChangeDiffHelper
{
    public record FieldDiff(string Key, string Label, string OldDisplay, string NewDisplay, bool Changed);

    public static List<FieldDiff> Compute(ProfileChangeFields oldData, ProfileChangeFields newData, IReadOnlyDictionary<int, string> departmentNames)
    {
        string Khoa(int id) => departmentNames.TryGetValue(id, out var name) ? name : $"#{id}";
        string Ngay(DateTime? d) => d.HasValue ? d.Value.ToString("dd/MM/yyyy") : "—";
        string Text(string? s) => string.IsNullOrWhiteSpace(s) ? "—" : s;

        var fields = new List<FieldDiff>
        {
            new("HoTen", "Họ và tên", Text(oldData.HoTen), Text(newData.HoTen), oldData.HoTen != newData.HoTen),
            new("NgaySinh", "Ngày sinh", Ngay(oldData.NgaySinh), Ngay(newData.NgaySinh), oldData.NgaySinh != newData.NgaySinh),
            new("HocVi", "Học hàm / Học vị", Text(oldData.HocVi), Text(newData.HocVi), oldData.HocVi != newData.HocVi),
            new("ChuyenKhoa", "Chuyên khoa", Text(oldData.ChuyenKhoa), Text(newData.ChuyenKhoa), oldData.ChuyenKhoa != newData.ChuyenKhoa),
            new("KhoaId", "Khoa/phòng công tác", Khoa(oldData.KhoaId), Khoa(newData.KhoaId), oldData.KhoaId != newData.KhoaId),
            new("ChucVu", "Chức vụ", Text(oldData.ChucVu), Text(newData.ChucVu), oldData.ChucVu != newData.ChucVu),
            new("SoCCHN", "Số CCHN", Text(oldData.SoCCHN), Text(newData.SoCCHN), oldData.SoCCHN != newData.SoCCHN),
            new("NgayCapCCHN", "Ngày cấp CCHN", Ngay(oldData.NgayCapCCHN), Ngay(newData.NgayCapCCHN), oldData.NgayCapCCHN != newData.NgayCapCCHN),
            new("NoiCapCCHN", "Nơi cấp CCHN", Text(oldData.NoiCapCCHN), Text(newData.NoiCapCCHN), oldData.NoiCapCCHN != newData.NoiCapCCHN),
            new("PhamViHanhNghe", "Phạm vi hành nghề", Text(oldData.PhamViHanhNghe), Text(newData.PhamViHanhNghe), oldData.PhamViHanhNghe != newData.PhamViHanhNghe)
        };

        return fields;
    }
}
