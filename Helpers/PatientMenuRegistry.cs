namespace QuanLyBenhVien.Helpers;

/// <summary>
/// Ordered config for the Patient sidebar. Unlike DoctorMenuItem, this carries
/// its own Label rather than looking one up by ModuleKey in
/// ModulePermissionRegistry - several items here intentionally share the same
/// ModuleKey (Patient.Record): "Lịch sử khám & Đơn thuốc", "Sổ sức khỏe", and
/// "Hồ sơ người thân" are all actions on RecordController, so
/// ModulePermissionFilter (which keys purely on controller name) can only
/// toggle them together as one permission - an honest consequence of the
/// existing controller boundaries, not an oversight - but each still needs
/// its own distinct sidebar label.
/// </summary>
public record PatientMenuItem(
    string ModuleKey,
    string Label,
    string Icon,
    string Route,
    string Group,
    string? BadgeSourceKey = null
);

public static class PatientMenuRegistry
{
    public const string BadgeUnpaidInvoices = "UnpaidInvoices";
    public const string BadgeUnreadNotifications = "UnreadNotifications";
    public const string BadgeUnreadMessages = "UnreadMessages";

    public static readonly IReadOnlyList<PatientMenuItem> Items = new List<PatientMenuItem>
    {
        new("Patient.Dashboard", "Trang chủ", "dashboard", "/Patient/Dashboard", PatientMenuGroups.HayDung),
        new("Patient.Book", "Đặt lịch khám", "add_box", "/Patient/Book", PatientMenuGroups.HayDung),
        new("Patient.Appointments", "Lịch hẹn của tôi", "event_note", "/Patient/Appointments", PatientMenuGroups.HayDung),
        new("Patient.Chat", "Hỏi bác sĩ", "forum", "/Patient/Chat", PatientMenuGroups.HayDung, BadgeUnreadMessages),
        new("Patient.LabResults", "Kết quả khám & Xét nghiệm", "biotech", "/Patient/LabResults", PatientMenuGroups.HayDung),
        new("Patient.Record", "Lịch sử khám & Đơn thuốc", "medical_information", "/Patient/Record", PatientMenuGroups.HayDung),

        new("Patient.Record", "Sổ sức khỏe", "favorite", "/Patient/Record/Health", PatientMenuGroups.HoSo),
        new("Patient.Record", "Hồ sơ người thân", "group", "/Patient/Record/Dependents", PatientMenuGroups.HoSo),
        new("Patient.Documents", "Tài liệu của tôi", "folder_shared", "/Patient/Documents", PatientMenuGroups.HoSo),

        new("Patient.Payment", "Thanh toán hóa đơn", "receipt_long", "/Patient/Payment", PatientMenuGroups.Khac, BadgeUnpaidInvoices),
        new("Patient.Notification", "Thông báo", "notifications", "/Patient/Notification", PatientMenuGroups.Khac, BadgeUnreadNotifications),
        new("Patient.Account", "Tài khoản & Bảo mật", "manage_accounts", "/Patient/Account", PatientMenuGroups.Khac),
        new("Patient.Support", "Hỗ trợ", "support_agent", "/Patient/Support", PatientMenuGroups.Khac)
    };
}
