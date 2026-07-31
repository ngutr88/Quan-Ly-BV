using QuanLyBenhVien.Models;

namespace QuanLyBenhVien.Helpers;

/// <summary>
/// Single source of truth for the two account-level (not role-level) sidebar
/// visibility conditions. Used by BOTH the sidebar menu (DoctorSidebarViewComponent)
/// and the matching route guard (DoctorMenuConditionFilter) via the same key,
/// so a menu item can never be visible while its URL is blocked or vice versa.
/// </summary>
public static class DoctorMenuConditions
{
    public const string WardAssignment = "WardAssignment";
    public const string SurgicalSpecialty = "SurgicalSpecialty";

    /// <summary>
    /// No ward/bed/room-assignment entity exists anywhere in the data model
    /// yet (confirmed by repo-wide search) - always false until that data
    /// model is introduced. This is honest current behavior, not a bug: no
    /// doctor account can satisfy this condition today.
    /// </summary>
    public static bool HasWardAssignment(Doctor doctor) => false;

    /// <summary>
    /// Doctor.ChuyenKhoa is free text with no controlled vocabulary, so this
    /// is a heuristic substring match pending a real permission flag.
    /// </summary>
    public static bool IsSurgicalSpecialty(Doctor doctor) =>
        VietnameseTextHelper.ContainsIgnoreCase(doctor.ChuyenKhoa, "ngoai");

    public static readonly IReadOnlyDictionary<string, Func<Doctor, bool>> ByKey =
        new Dictionary<string, Func<Doctor, bool>>
        {
            [WardAssignment] = HasWardAssignment,
            [SurgicalSpecialty] = IsSurgicalSpecialty
        };
}
