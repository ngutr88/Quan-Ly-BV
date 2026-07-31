namespace QuanLyBenhVien.Models.ViewModels;

/// <summary>
/// Backs the Doctor area dashboard (queue, action-required panel, week
/// schedule). Replaces the previous ViewBag/HashSet grab-bag so the page can
/// be split into partials that all render from the same shape of data.
/// </summary>
public class DoctorDashboardViewModel
{
    public Doctor Doctor { get; set; } = null!;

    public int TodayTotal { get; set; }
    public int TodayCompleted { get; set; }
    public int TodayWaiting { get; set; }
    public int? LongestWaitMinutes { get; set; }
    public int TomorrowTotal { get; set; }
    public int IncompleteRecordsCount { get; set; }

    public List<QueueRowViewModel> Queue { get; set; } = new();
    public List<ActionRequiredItemViewModel> ActionRequired { get; set; } = new();
    public List<WeekDayViewModel> Week { get; set; } = new();

    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}

/// <summary>One row of the "Hàng đợi khám hôm nay" table.</summary>
public class QueueRowViewModel
{
    public Appointment Appointment { get; set; } = null!;

    public int SttIndex { get; set; }

    /// <summary>True when this patient already has at least one prior
    /// completed encounter with this doctor ("Tái khám").</summary>
    public bool IsReturningPatient { get; set; }

    /// <summary>True when an ExaminationRecord already exists for this
    /// appointment (drives the "Xem bệnh án" vs. dead-end status display).</summary>
    public bool HasRecord { get; set; }

    public List<string> AllergyList { get; set; } = new();

    public string? ImportantHistoryNote { get; set; }
}

/// <summary>One row of the "Cần xử lý" panel — currently only the
/// "hồ sơ chưa hoàn thiện" group is backed by real data.</summary>
public class ActionRequiredItemViewModel
{
    public int AppointmentId { get; set; }

    public string PatientName { get; set; } = string.Empty;

    public DateTime ThoiGian { get; set; }

    /// <summary>Why this item needs attention, e.g. "Chưa lập hồ sơ khám" /
    /// "Thiếu chẩn đoán".</summary>
    public string Reason { get; set; } = string.Empty;
}

/// <summary>One day cell of the "Lịch tuần" block.</summary>
public class WeekDayViewModel
{
    public DateTime Date { get; set; }

    public bool IsToday { get; set; }

    public int AppointmentCount { get; set; }

    /// <summary>Recurring shift labels active on this weekday, e.g.
    /// "08:00 - 12:00 · Phòng 203".</summary>
    public List<string> ShiftLabels { get; set; } = new();
}
