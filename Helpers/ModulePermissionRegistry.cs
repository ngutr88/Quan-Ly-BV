namespace QuanLyBenhVien.Helpers;

/// <summary>
/// Single source of truth for which modules exist per role area, used by both
/// the permission-matrix screen and the enforcement filter. Keys here must
/// match "{Area}.{Controller}" exactly, since that is how the filter derives
/// the module key for an incoming request from route data.
/// </summary>
public static class ModulePermissionRegistry
{
    public record ModuleInfo(string Key, string Label);

    public const string Admin = "Admin";
    public const string Doctor = "Doctor";
    public const string Patient = "Patient";

    // Modules that must always stay reachable for their own role, regardless
    // of matrix state, so an admin can never lock every admin out of the
    // screen that controls locking, and every role keeps its own dashboard.
    public static readonly IReadOnlySet<string> AlwaysAllowed = new HashSet<string>
    {
        "Admin.Staff",
        "Admin.Dashboard",
        "Doctor.Dashboard",
        "Patient.Dashboard"
    };

    public static readonly IReadOnlyList<ModuleInfo> AdminModules = new List<ModuleInfo>
    {
        new("Admin.Dashboard", "Dashboard"),
        new("Admin.Appointments", "Lịch khám"),
        new("Admin.Doctors", "Bác sĩ"),
        new("Admin.Patients", "Bệnh nhân"),
        new("Admin.Departments", "Khoa & Dịch vụ"),
        new("Admin.Medicines", "Thuốc & Kho"),
        new("Admin.Invoices", "Hóa đơn"),
        new("Admin.News", "Tin tức & Thông báo"),
        new("Admin.Staff", "Nhân sự & Phân quyền"),
        new("Admin.Reports", "Báo cáo"),
        new("Admin.Settings", "Cấu hình hệ thống"),
        new("Admin.Logs", "Nhật ký hệ thống")
    };

    public static readonly IReadOnlyList<ModuleInfo> DoctorModules = new List<ModuleInfo>
    {
        new("Doctor.Dashboard", "Bảng điều khiển"),
        new("Doctor.Queue", "Lịch khám hôm nay"),
        new("Doctor.Exam", "Phiên khám bệnh"),
        new("Doctor.History", "Lịch sử điều trị"),
        new("Doctor.Stats", "Thống kê cá nhân"),
        new("Doctor.Chat", "Tin nhắn tư vấn")
    };

    public static readonly IReadOnlyList<ModuleInfo> PatientModules = new List<ModuleInfo>
    {
        new("Patient.Dashboard", "Trang chủ"),
        new("Patient.Book", "Đặt lịch khám"),
        new("Patient.Record", "Đơn thuốc & Bệnh án"),
        new("Patient.Payment", "Thanh toán hóa đơn"),
        new("Patient.Notification", "Thông báo cá nhân")
    };

    public static readonly IReadOnlyDictionary<string, IReadOnlyList<ModuleInfo>> ModulesByRole =
        new Dictionary<string, IReadOnlyList<ModuleInfo>>
        {
            [Admin] = AdminModules,
            [Doctor] = DoctorModules,
            [Patient] = PatientModules
        };

    public static IEnumerable<(string Role, string RoleLabel, IReadOnlyList<ModuleInfo> Modules)> AllGroups()
    {
        yield return (Admin, "Quản trị viên", AdminModules);
        yield return (Doctor, "Bác sĩ", DoctorModules);
        yield return (Patient, "Bệnh nhân", PatientModules);
    }
}
