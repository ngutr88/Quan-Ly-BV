using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Models;
using QuanLyBenhVien.Models.ViewModels;
using QuanLyBenhVien.Services;

namespace QuanLyBenhVien.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize(Roles = "Doctor")]
    public class DashboardController : Controller
    {
        // Completed appointments older than this are no longer surfaced as
        // "hồ sơ chưa hoàn thiện" - keeps the query bounded and the panel
        // actionable instead of dredging up years-old edge cases.
        private const int IncompleteLookbackDays = 30;

        private readonly ApplicationDbContext _context;
        private readonly DoctorDashboardNotifier _notifier;

        public DashboardController(ApplicationDbContext context, DoctorDashboardNotifier notifier)
        {
            _context = context;
            _notifier = notifier;
        }

        public async Task<IActionResult> Index()
        {
            var doctor = await ResolveDoctorAsync();
            if (doctor == null) return NotFound("Bác sĩ không tồn tại trong hệ thống.");

            var vm = await BuildDashboardDataAsync(doctor);
            return View(vm);
        }

        // GET: /Doctor/Dashboard/QueueSection
        // Single source of truth for queue rendering - used by the initial
        // page load, the manual refresh button, and the SignalR handler.
        [HttpGet]
        public async Task<IActionResult> QueueSection()
        {
            var doctor = await ResolveDoctorAsync();
            if (doctor == null) return NotFound();

            var vm = await BuildDashboardDataAsync(doctor);
            return PartialView("_QueueSection", vm);
        }

        [HttpGet]
        public async Task<IActionResult> ActionRequiredSection()
        {
            var doctor = await ResolveDoctorAsync();
            if (doctor == null) return NotFound();

            var vm = await BuildDashboardDataAsync(doctor);
            return PartialView("_ActionRequiredSection", vm);
        }

        // GET: /Doctor/Dashboard/Incomplete - "Xem tất cả" target for the
        // "hồ sơ chưa hoàn thiện" group.
        [HttpGet]
        public async Task<IActionResult> Incomplete()
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (!doctorId.HasValue) return Forbid();

            var items = await BuildIncompleteRecordsAsync(doctorId.Value);
            return View(items);
        }

        // POST: /Doctor/Dashboard/CallNextPatient
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CallNextPatient()
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (!doctorId.HasValue) return Forbid();

            // Idempotent: if the doctor already has someone in the room,
            // re-point there instead of picking a second patient (handles a
            // double-click or a second browser tab hitting the same button).
            var inProgress = await _context.Appointments
                .Where(a => a.BacSiId == doctorId.Value && a.ThoiGian.Date == DateTime.Today && a.TrangThai == "DangKham")
                .OrderBy(a => a.ThoiGian)
                .FirstOrDefaultAsync();

            if (inProgress != null)
            {
                return Json(new { success = true, appointmentId = inProgress.Id, redirectUrl = $"/Doctor/Exam/Session/{inProgress.Id}" });
            }

            var candidate = await _context.Appointments
                .Where(a => a.BacSiId == doctorId.Value && a.ThoiGian.Date == DateTime.Today && a.TrangThai == "DaXacNhan")
                .OrderBy(a => a.ThoiGian)
                .FirstOrDefaultAsync();

            if (candidate == null)
            {
                return Json(new { success = false, message = "Không có bệnh nhân nào đang chờ khám." });
            }

            // Single atomic conditional UPDATE - the row lock this generates
            // serializes concurrent calls without needing a RowVersion column.
            var affected = await _context.Appointments
                .Where(a => a.Id == candidate.Id && a.BacSiId == doctorId.Value && a.TrangThai == "DaXacNhan")
                .ExecuteUpdateAsync(s => s.SetProperty(a => a.TrangThai, "DangKham"));

            if (affected == 0)
            {
                // Lost the race (another tab / concurrent click already claimed
                // this appointment) - point at whatever is now in progress
                // instead of erroring.
                var nowInProgress = await _context.Appointments
                    .Where(a => a.BacSiId == doctorId.Value && a.ThoiGian.Date == DateTime.Today && a.TrangThai == "DangKham")
                    .OrderBy(a => a.ThoiGian)
                    .FirstOrDefaultAsync();

                if (nowInProgress != null)
                {
                    return Json(new { success = true, appointmentId = nowInProgress.Id, redirectUrl = $"/Doctor/Exam/Session/{nowInProgress.Id}" });
                }

                return Json(new { success = false, message = "Danh sách chờ vừa thay đổi, vui lòng thử lại." });
            }

            // TODO: khi có hệ thống xếp hàng/màn hình chờ thật, phát số cho
            // bệnh nhân này tại đây (ví dụ gọi ra máy in số/loa gọi tên).

            await _notifier.NotifyQueueUpdatedAsync(doctorId);

            return Json(new { success = true, appointmentId = candidate.Id, redirectUrl = $"/Doctor/Exam/Session/{candidate.Id}" });
        }

        private async Task<DoctorDashboardViewModel> BuildDashboardDataAsync(QuanLyBenhVien.Models.Doctor doctor)
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var now = DateTime.Now;

            var todayAppointments = await _context.Appointments
                .Include(a => a.Patient.User)
                .Where(a => a.BacSiId == doctor.Id && a.ThoiGian.Date == today && a.TrangThai != "DaHuy")
                .ToListAsync();

            var tomorrowTotal = await _context.Appointments
                .CountAsync(a => a.BacSiId == doctor.Id && a.ThoiGian.Date == tomorrow && a.TrangThai != "DaHuy");

            var waitingApps = todayAppointments.Where(a => a.TrangThai == "DaXacNhan").ToList();
            int? longestWaitMinutes = null;
            if (waitingApps.Count > 0)
            {
                var maxWait = waitingApps
                    .Where(a => a.ThoiGian <= now)
                    .Select(a => (int)(now - a.ThoiGian).TotalMinutes)
                    .DefaultIfEmpty(0)
                    .Max();
                longestWaitMinutes = maxWait;
            }

            // Patients this doctor has seen before today - drives the "Tái
            // khám" label without needing a completed-record join per row.
            var previousPatientIds = (await _context.Appointments
                .Where(a => a.BacSiId == doctor.Id && a.ThoiGian.Date < today)
                .Select(a => a.BenhNhanId)
                .Distinct()
                .ToListAsync())
                .ToHashSet();

            var recordAppointmentIds = (await _context.ExaminationRecords
                .Where(e => e.Appointment.BacSiId == doctor.Id)
                .Select(e => e.LichKhamId)
                .ToListAsync())
                .ToHashSet();

            var orderedQueue = todayAppointments
                .OrderByDescending(a => a.TrangThai == "DangKham")
                .ThenBy(a => a.ThoiGian)
                .ToList();

            var queue = new List<QueueRowViewModel>();
            for (var i = 0; i < orderedQueue.Count; i++)
            {
                var app = orderedQueue[i];
                var patient = app.Patient;
                queue.Add(new QueueRowViewModel
                {
                    Appointment = app,
                    SttIndex = i + 1,
                    IsReturningPatient = previousPatientIds.Contains(app.BenhNhanId),
                    HasRecord = recordAppointmentIds.Contains(app.Id),
                    AllergyList = SplitFreeText(patient?.DiUng),
                    ImportantHistoryNote = string.IsNullOrWhiteSpace(patient?.TienSuBenh) ? null : patient.TienSuBenh.Trim()
                });
            }

            var incompleteAll = await BuildIncompleteRecordsAsync(doctor.Id);

            var week = await BuildWeekScheduleAsync(doctor.Id, today);

            return new DoctorDashboardViewModel
            {
                Doctor = doctor,
                TodayTotal = todayAppointments.Count,
                TodayCompleted = todayAppointments.Count(a => a.TrangThai == "HoanThanh"),
                TodayWaiting = waitingApps.Count,
                LongestWaitMinutes = longestWaitMinutes,
                TomorrowTotal = tomorrowTotal,
                IncompleteRecordsCount = incompleteAll.Count,
                Queue = queue,
                ActionRequired = incompleteAll.Take(5).ToList(),
                Week = week
            };
        }

        private async Task<List<ActionRequiredItemViewModel>> BuildIncompleteRecordsAsync(int doctorId)
        {
            var lookbackFrom = DateTime.Today.AddDays(-IncompleteLookbackDays);

            var completedAppointments = await _context.Appointments
                .Include(a => a.Patient.User)
                .Where(a => a.BacSiId == doctorId && a.TrangThai == "HoanThanh" && a.ThoiGian >= lookbackFrom)
                .ToListAsync();

            if (completedAppointments.Count == 0) return new List<ActionRequiredItemViewModel>();

            var appointmentIds = completedAppointments.Select(a => a.Id).ToList();
            var recordsByApptId = await _context.ExaminationRecords
                .Where(e => appointmentIds.Contains(e.LichKhamId))
                .Select(e => new { e.LichKhamId, e.ChanDoan })
                .ToDictionaryAsync(e => e.LichKhamId, e => e.ChanDoan);

            return completedAppointments
                .Select(a =>
                {
                    var hasRecord = recordsByApptId.TryGetValue(a.Id, out var chanDoan);
                    var missingDiagnosis = hasRecord && string.IsNullOrWhiteSpace(chanDoan);
                    return new { Appointment = a, hasRecord, missingDiagnosis };
                })
                .Where(x => !x.hasRecord || x.missingDiagnosis)
                .OrderByDescending(x => x.Appointment.ThoiGian)
                .Select(x => new ActionRequiredItemViewModel
                {
                    AppointmentId = x.Appointment.Id,
                    PatientName = x.Appointment.Patient?.User?.HoTen ?? string.Empty,
                    ThoiGian = x.Appointment.ThoiGian,
                    Reason = x.hasRecord ? "Thiếu chẩn đoán" : "Chưa lập hồ sơ khám"
                })
                .ToList();
        }

        private async Task<List<WeekDayViewModel>> BuildWeekScheduleAsync(int doctorId, DateTime weekStart)
        {
            var weekEnd = weekStart.AddDays(7);

            var weekAppointments = await _context.Appointments
                .Where(a => a.BacSiId == doctorId && a.ThoiGian >= weekStart && a.ThoiGian < weekEnd && a.TrangThai != "DaHuy")
                .Select(a => a.ThoiGian.Date)
                .ToListAsync();

            var shifts = await _context.DoctorWorkSchedules
                .Where(s => s.BacSiId == doctorId && s.DangHoatDong)
                .ToListAsync();

            var week = new List<WeekDayViewModel>();
            for (var i = 0; i < 7; i++)
            {
                var date = weekStart.AddDays(i);
                var dayOfWeek = (int)date.DayOfWeek; // 0=Sunday..6=Saturday, matches ThuTrongTuan

                var shiftLabels = shifts
                    .Where(s => s.ThuTrongTuan == dayOfWeek
                        && (!s.HieuLucTu.HasValue || s.HieuLucTu.Value.Date <= date)
                        && (!s.HieuLucDen.HasValue || s.HieuLucDen.Value.Date >= date))
                    .OrderBy(s => s.GioBatDau)
                    .Select(s => string.IsNullOrWhiteSpace(s.PhongKham)
                        ? $"{s.GioBatDau:hh\\:mm} - {s.GioKetThuc:hh\\:mm}"
                        : $"{s.GioBatDau:hh\\:mm} - {s.GioKetThuc:hh\\:mm} · {s.PhongKham}")
                    .ToList();

                week.Add(new WeekDayViewModel
                {
                    Date = date,
                    IsToday = date == weekStart,
                    AppointmentCount = weekAppointments.Count(d => d == date),
                    ShiftLabels = shiftLabels
                });
            }

            return week;
        }

        private static List<string> SplitFreeText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new List<string>();
            return text.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(s => s.Length > 0)
                .ToList();
        }

        private async Task<QuanLyBenhVien.Models.Doctor?> ResolveDoctorAsync()
        {
            var currentUserId = GetCurrentUserId();
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Department)
                .FirstOrDefaultAsync(d => d.NguoiDungId == currentUserId);

            if (doctor == null)
            {
                // Fallback for older cookies that may not contain a user ID.
                var identityValue = User.Identity?.Name;
                doctor = await _context.Doctors
                    .Include(d => d.User)
                    .Include(d => d.Department)
                    .FirstOrDefaultAsync(d => d.User.HoTen == identityValue || d.User.Email == identityValue);
            }

            return doctor;
        }

        private async Task<int?> GetCurrentDoctorIdAsync()
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return null;
            return await _context.Doctors
                .Where(d => d.NguoiDungId == userId)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
