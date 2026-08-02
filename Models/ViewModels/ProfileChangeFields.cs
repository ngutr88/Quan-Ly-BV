using System;

namespace QuanLyBenhVien.Models.ViewModels;

/// <summary>
/// The exact set of "hồ sơ hành nghề" fields that go through the
/// đề xuất→duyệt (propose→approve) workflow instead of being saved directly.
/// Serialized to JSON for both <c>ProfileChangeRequest.DuLieuCuJson</c> (snapshot
/// at proposal time) and <c>DuLieuMoiJson</c> (proposed values), so the same
/// shape drives the diff view on both the Doctor and Admin sides.
/// </summary>
public class ProfileChangeFields
{
    public string HoTen { get; set; } = string.Empty;

    public DateTime? NgaySinh { get; set; }

    public string HocVi { get; set; } = string.Empty;

    public string ChuyenKhoa { get; set; } = string.Empty;

    public int KhoaId { get; set; }

    public string ChucVu { get; set; } = string.Empty;

    public string? SoCCHN { get; set; }

    public DateTime? NgayCapCCHN { get; set; }

    public string? NoiCapCCHN { get; set; }

    public string? PhamViHanhNghe { get; set; }
}
