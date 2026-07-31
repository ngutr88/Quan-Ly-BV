namespace QuanLyBenhVien.Helpers;

/// <summary>
/// Ordered config for the Doctor sidebar. Deliberately does not repeat the
/// display label - that is looked up from ModulePermissionRegistry.DoctorModules
/// by ModuleKey at render time, so the sidebar and the permission matrix can
/// never show two different names for the same module.
/// </summary>
public record DoctorMenuItem(
    string ModuleKey,
    string Icon,
    string Route,
    string Group,
    string? ExtraVisibilityKey = null,
    string? BadgeSourceKey = null
);

public static class DoctorMenuRegistry
{
    public const string BadgeUnreadNotifications = "UnreadNotifications";

    public static readonly IReadOnlyList<DoctorMenuItem> Items = new List<DoctorMenuItem>
    {
        new("Doctor.Dashboard", "dashboard", "/Doctor/Dashboard", DoctorMenuGroups.CongViecHangNgay),
        new("Doctor.Queue", "patient_list", "/Doctor/Queue", DoctorMenuGroups.CongViecHangNgay),
        new("Doctor.Inpatient", "bed", "/Doctor/Inpatient", DoctorMenuGroups.CongViecHangNgay, DoctorMenuConditions.WardAssignment),
        new("Doctor.LabResults", "biotech", "/Doctor/LabResults", DoctorMenuGroups.CongViecHangNgay),

        new("Doctor.History", "history_edu", "/Doctor/History", DoctorMenuGroups.HoSoChuyenMon),
        new("Doctor.Prescriptions", "medication", "/Doctor/Prescriptions", DoctorMenuGroups.HoSoChuyenMon),
        new("Doctor.Consultation", "groups", "/Doctor/Consultation", DoctorMenuGroups.HoSoChuyenMon),
        new("Doctor.Surgery", "local_hospital", "/Doctor/Surgery", DoctorMenuGroups.HoSoChuyenMon, DoctorMenuConditions.SurgicalSpecialty),
        new("Doctor.PendingSignatures", "draw", "/Doctor/PendingSignatures", DoctorMenuGroups.HoSoChuyenMon),

        new("Doctor.Schedule", "calendar_month", "/Doctor/Schedule", DoctorMenuGroups.LichVaGiaoTiep),
        new("Doctor.Chat", "forum", "/Doctor/Chat", DoctorMenuGroups.LichVaGiaoTiep),
        new("Doctor.Notification", "notifications", "/Doctor/Notification", DoctorMenuGroups.LichVaGiaoTiep, BadgeSourceKey: BadgeUnreadNotifications),

        new("Doctor.Stats", "analytics", "/Doctor/Stats", DoctorMenuGroups.CaNhan),
        new("Doctor.Profile", "manage_accounts", "/Doctor/Profile", DoctorMenuGroups.CaNhan)
    };
}
