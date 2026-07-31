using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Helpers;
using QuanLyBenhVien.Models.ViewModels;
using static QuanLyBenhVien.Helpers.DoctorDisplayHelper;

namespace QuanLyBenhVien.Areas.Patient.Controllers
{
    [Area("Patient")]
    [Authorize(Roles = "Patient")]
    public class DashboardController : Controller
    {
        // How long since the last completed visit before the "tái khám" reminder
        // becomes eligible for a patient with a matching chronic-disease keyword.
        private const int FollowUpReminderWeeks = 12;

        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var patientUserId = GetCurrentUserId();
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.NguoiDungId == patientUserId);

            if (patient == null)
            {
                var email = User.FindFirstValue(ClaimTypes.Email);
                var identityName = User.Identity?.Name;
                patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.User.Email == email || p.User.Email == identityName || p.User.HoTen == identityName);
            }

            if (patient == null) return NotFound("Hồ sơ bệnh nhân không tồn tại.");

            // 1. Upcoming appointments - NextAppointment is drawn from this same
            // list; the view must skip index 0 when rendering the list below it
            // to avoid showing the same appointment twice (hero card + list row).
            var upcomingApps = await _context.Appointments
                .Include(a => a.Doctor.User)
                .Include(a => a.Doctor.Department)
                .Where(a => a.BenhNhanId == patient.Id && a.ThoiGian >= DateTime.Now && a.TrangThai != "DaHuy")
                .OrderBy(a => a.ThoiGian)
                .ToListAsync();

            // 2. All exam records drive total-visits/last-visit/vitals AND the
            // "Lượt khám gần đây" list (top 5) - one query instead of two.
            var examRecords = await _context.ExaminationRecords
                .Include(e => e.Appointment.Doctor.User)
                .Include(e => e.Appointment.Doctor.Department)
                .Where(e => e.Appointment.BenhNhanId == patient.Id)
                .OrderByDescending(e => e.NgayKham)
                .ToListAsync();

            var recentRecords = examRecords.Take(5).ToList();
            var recentRecordIds = recentRecords.Select(e => e.Id).ToList();
            var prescriptionsForRecent = await _context.Prescriptions
                .Include(p => p.PrescriptionDetails)
                .Where(p => recentRecordIds.Contains(p.PhieuKhamId))
                .ToListAsync();
            var medicineCountByRecord = prescriptionsForRecent.ToDictionary(p => p.PhieuKhamId, p => p.PrescriptionDetails.Count);

            var recentVisits = recentRecords.Select(e => new RecentVisitViewModel
            {
                Record = e,
                PrescriptionMedicineCount = medicineCountByRecord.TryGetValue(e.Id, out var count) ? count : 0
            }).ToList();

            // 3. Outstanding balance drives the payment call-to-action.
            var unpaidInvoices = await _context.Invoices
                .Include(i => i.ExaminationRecord.Appointment.Doctor.User)
                .Where(i => i.ExaminationRecord.Appointment.BenhNhanId == patient.Id
                            && i.TrangThaiThanhToan == "ChuaThanhToan")
                .OrderByDescending(i => i.NgayTao)
                .ToListAsync();

            // 4. "Thuốc đang sử dụng" - distinct medicines prescribed in the last
            // 30 days. Not a days-remaining countdown: PrescriptionDetail.LieuDung
            // is unconstrained free text, so a daily-dose estimate parsed out of it
            // would be fabricated precision, not a real number.
            var activePrescriptionCount = await _context.PrescriptionDetails
                .Where(pd => pd.Prescription.ExaminationRecord.Appointment.BenhNhanId == patient.Id
                             && pd.Prescription.NgayKe >= DateTime.Today.AddDays(-30))
                .Select(pd => pd.ThuocId)
                .Distinct()
                .CountAsync();

            // 5. Notifications
            var recentNotifications = await _context.Notifications
                .Where(n => n.NguoiDungId == patient.NguoiDungId)
                .OrderByDescending(n => n.NgayGui)
                .Take(4)
                .ToListAsync();

            var unreadNotificationCount = await _context.Notifications
                .CountAsync(n => n.NguoiDungId == patient.NguoiDungId && !n.DaDoc);

            // 6. Follow-up ("tái khám") reminder - heuristic only: a chronic-disease
            // keyword match in free-text TienSuBenh, no upcoming appointment, and
            // the last completed visit is old enough. Only shown when there is a
            // real doctor/department to pre-fill - a patient with the keyword but
            // no completed visit yet has nothing concrete to recommend a "return"
            // to, so that case is skipped rather than shown half-empty.
            FollowUpReminderViewModel? followUpReminder = null;
            if (upcomingApps.Count == 0 && !string.IsNullOrWhiteSpace(patient.TienSuBenh) &&
                ChronicDiseaseKeywords.Keywords.Any(k => VietnameseTextHelper.ContainsIgnoreCase(patient.TienSuBenh, k)))
            {
                var lastCompleted = await _context.Appointments
                    .Include(a => a.Doctor.User)
                    .Include(a => a.Doctor.Department)
                    .Where(a => a.BenhNhanId == patient.Id && a.TrangThai == "HoanThanh")
                    .OrderByDescending(a => a.ThoiGian)
                    .FirstOrDefaultAsync();

                if (lastCompleted?.Doctor != null && lastCompleted.ThoiGian < DateTime.Today.AddDays(-7 * FollowUpReminderWeeks))
                {
                    followUpReminder = new FollowUpReminderViewModel
                    {
                        DoctorId = lastCompleted.Doctor.Id,
                        DoctorName = FormatDoctorName(lastCompleted.Doctor),
                        DepartmentId = lastCompleted.Doctor.KhoaId,
                        DepartmentName = lastCompleted.Doctor.Department.TenKhoa
                    };
                }
            }

            var vm = new PatientDashboardViewModel
            {
                Patient = patient,
                UpcomingAppointments = upcomingApps,
                NextAppointment = upcomingApps.FirstOrDefault(),
                RecentVisits = recentVisits,
                UnpaidInvoiceCount = unpaidInvoices.Count,
                UnpaidInvoiceAmount = unpaidInvoices.Sum(i => i.TongTien),
                UnpaidInvoicePreview = unpaidInvoices.Take(3).ToList(),
                TotalVisits = examRecords.Count,
                LastVisit = examRecords.FirstOrDefault()?.NgayKham,
                LatestVitals = examRecords.FirstOrDefault(e => e.CanNang.HasValue || e.ChieuCao.HasValue
                                                                || e.NhietDo.HasValue || e.BMI.HasValue),
                RecentNotifications = recentNotifications,
                UnreadNotificationCount = unreadNotificationCount,
                ActivePrescriptionCount = activePrescriptionCount,
                FollowUpReminder = followUpReminder
            };

            return View(vm);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
