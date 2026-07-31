namespace QuanLyBenhVien.Models.ViewModels;

/// <summary>
/// Backs the Patient area homepage. Replaces the previous ViewBag/Model grab-bag
/// (Areas/Patient/Controllers/DashboardController.cs) so bug fixes (dedup,
/// diagnosis-on-homepage, BHYT masking) have one clear, typed source instead of
/// loosely-typed ViewBag reads scattered across the view.
/// </summary>
public class PatientDashboardViewModel
{
    public Patient Patient { get; set; } = null!;

    public List<Appointment> UpcomingAppointments { get; set; } = new();
    public Appointment? NextAppointment { get; set; }

    public List<RecentVisitViewModel> RecentVisits { get; set; } = new();

    public int UnpaidInvoiceCount { get; set; }
    public decimal UnpaidInvoiceAmount { get; set; }
    public List<Invoice> UnpaidInvoicePreview { get; set; } = new();

    public int TotalVisits { get; set; }
    public DateTime? LastVisit { get; set; }
    public ExaminationRecord? LatestVitals { get; set; }

    public List<Notification> RecentNotifications { get; set; } = new();
    public int UnreadNotificationCount { get; set; }

    /// <summary>Distinct medicines prescribed in the last 30 days - see
    /// DashboardController for why this replaces a fabricated "days
    /// remaining" countdown.</summary>
    public int ActivePrescriptionCount { get; set; }

    public FollowUpReminderViewModel? FollowUpReminder { get; set; }
}

public class RecentVisitViewModel
{
    public ExaminationRecord Record { get; set; } = null!;
    public int PrescriptionMedicineCount { get; set; }
}

public class FollowUpReminderViewModel
{
    public int DoctorId { get; set; }
    public string DoctorName { get; set; } = string.Empty;
    public int DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
}
