using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Models;
using QuanLyBenhVien.Services;

namespace QuanLyBenhVien.Areas.Patient.Controllers
{
    [Area("Patient")]
    [Authorize(Roles = "Patient")]
    public class AppointmentsController : Controller
    {
        // How close to the appointment time a patient can still self-serve
        // reschedule/cancel - inside this window they must call the hospital
        // instead, mirroring how the Admin-side Cancel has no such cutoff
        // (staff can always cancel; a patient acting alone this close to the
        // slot risks the doctor already being mid-preparation for it).
        private const int ChangeCutoffHours = 24;

        private static readonly string[] TerminalStatuses = { "HoanThanh", "DaHuy", "DangKham", "VangMat" };

        private readonly ApplicationDbContext _context;
        private readonly DoctorDashboardNotifier _notifier;
        private readonly AppointmentSlotService _slotService;

        public AppointmentsController(ApplicationDbContext context, DoctorDashboardNotifier notifier, AppointmentSlotService slotService)
        {
            _context = context;
            _notifier = notifier;
            _slotService = slotService;
        }

        // GET: /Patient/Appointments
        public async Task<IActionResult> Index()
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return NotFound();

            var all = await _context.Appointments
                .Include(a => a.Doctor.User)
                .Include(a => a.Doctor.Department)
                .Where(a => a.BenhNhanId == patient.Id)
                .ToListAsync();

            ViewBag.Upcoming = all.Where(a => a.ThoiGian >= DateTime.Now && a.TrangThai != "DaHuy")
                .OrderBy(a => a.ThoiGian).ToList();
            ViewBag.Past = all.Where(a => a.ThoiGian < DateTime.Now || a.TrangThai == "DaHuy")
                .OrderByDescending(a => a.ThoiGian).ToList();
            ViewBag.ChangeCutoffHours = ChangeCutoffHours;

            return View();
        }

        // GET: /Patient/Appointments/Reschedule/5
        [HttpGet]
        public async Task<IActionResult> Reschedule(int id)
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return NotFound();

            var app = await _context.Appointments
                .Include(a => a.Doctor.User)
                .Include(a => a.Doctor.Department)
                .FirstOrDefaultAsync(a => a.Id == id && a.BenhNhanId == patient.Id);

            if (app == null) return NotFound();

            var (allowed, reason) = CanChange(app);
            if (!allowed)
            {
                TempData["ErrorMessage"] = reason;
                return RedirectToAction(nameof(Index));
            }

            ViewBag.BookingMaxDate = DateTime.Today.AddDays(14);
            return View(app);
        }

        // POST: /Patient/Appointments/Reschedule/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reschedule(int id, DateTime bookingDate, string bookingTime)
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return NotFound();

            var app = await _context.Appointments
                .Include(a => a.Patient.User)
                .Include(a => a.Doctor.User)
                .FirstOrDefaultAsync(a => a.Id == id && a.BenhNhanId == patient.Id);

            if (app == null) return NotFound();

            var (allowed, reason) = CanChange(app);
            if (!allowed)
            {
                TempData["ErrorMessage"] = reason;
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(bookingTime))
            {
                TempData["ErrorMessage"] = "Vui lòng chọn khung giờ mới.";
                return RedirectToAction(nameof(Reschedule), new { id });
            }

            var timeParts = bookingTime.Split(':');
            var newTime = bookingDate.Date.AddHours(int.Parse(timeParts[0])).AddMinutes(int.Parse(timeParts[1]));

            if (newTime <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Không thể đổi sang thời điểm đã qua.";
                return RedirectToAction(nameof(Reschedule), new { id });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            var validation = await _slotService.ValidateSlotAsync(app.BacSiId!.Value, newTime, patient.Id, excludingAppointmentId: app.Id);
            if (!validation.Success)
            {
                await transaction.RollbackAsync();
                TempData["ErrorMessage"] = validation.ErrorMessage;
                return RedirectToAction(nameof(Reschedule), new { id });
            }

            var oldTime = app.ThoiGian;
            app.ThoiGian = newTime;
            app.TrangThai = "ChoXacNhan"; // Re-confirmation required after any change.
            _context.Entry(app).State = EntityState.Modified;

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Đổi lịch khám",
                ChiTiet = $"Bệnh nhân {patient.User.HoTen} đổi lịch hẹn #{app.Id} từ {oldTime:HH:mm dd/MM/yyyy} sang {newTime:HH:mm dd/MM/yyyy}."
            });
            _context.Notifications.Add(new Notification
            {
                NguoiDungId = GetCurrentUserId(),
                NoiDung = $"[LichKham] Đổi lịch khám|Lịch hẹn của bạn đã được đổi sang {newTime:HH:mm dd/MM/yyyy} và đang chờ xác nhận lại.",
                NgayGui = DateTime.Now,
                DaDoc = false
            });
            if (app.Doctor != null)
            {
                _context.Notifications.Add(new Notification
                {
                    NguoiDungId = app.Doctor.NguoiDungId,
                    NoiDung = $"[LichKham] Bệnh nhân đổi lịch|Bệnh nhân {patient.User.HoTen} đã đổi lịch hẹn từ {oldTime:HH:mm dd/MM/yyyy} sang {newTime:HH:mm dd/MM/yyyy}.",
                    NgayGui = DateTime.Now,
                    DaDoc = false
                });
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _notifier.NotifyQueueUpdatedAsync(app.BacSiId);
            await _notifier.NotifyNotificationCountChangedAsync(app.BacSiId);

            TempData["SuccessMessage"] = "Đã đổi lịch hẹn thành công. Vui lòng chờ xác nhận lại.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Patient/Appointments/Cancel/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, string lyDoHuy)
        {
            var patient = await GetCurrentPatientAsync();
            if (patient == null) return NotFound();

            var app = await _context.Appointments
                .Include(a => a.Patient.User)
                .Include(a => a.Doctor.User)
                .FirstOrDefaultAsync(a => a.Id == id && a.BenhNhanId == patient.Id);

            if (app == null) return NotFound();

            if (string.IsNullOrWhiteSpace(lyDoHuy))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do hủy lịch.";
                return RedirectToAction(nameof(Index));
            }

            var (allowed, reason) = CanChange(app);
            if (!allowed)
            {
                TempData["ErrorMessage"] = reason;
                return RedirectToAction(nameof(Index));
            }

            app.TrangThai = "DaHuy";
            app.LyDoKham = $"[ĐÃ HỦY - Lý do: {lyDoHuy}] " + app.LyDoKham;
            _context.Entry(app).State = EntityState.Modified;

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Hủy lịch khám",
                ChiTiet = $"Bệnh nhân {patient.User.HoTen} tự hủy lịch hẹn #{id}. Lý do: {lyDoHuy}."
            });
            if (app.Doctor != null)
            {
                _context.Notifications.Add(new Notification
                {
                    NguoiDungId = app.Doctor.NguoiDungId,
                    NoiDung = $"[LichKham] Bệnh nhân hủy lịch|Bệnh nhân {patient.User.HoTen} đã hủy lịch hẹn lúc {app.ThoiGian:HH:mm dd/MM/yyyy}. Lý do: {lyDoHuy}.",
                    NgayGui = DateTime.Now,
                    DaDoc = false
                });
            }

            await _context.SaveChangesAsync();

            await _notifier.NotifyQueueUpdatedAsync(app.BacSiId);
            await _notifier.NotifyNotificationCountChangedAsync(app.BacSiId);

            TempData["SuccessMessage"] = "Đã hủy lịch hẹn.";
            return RedirectToAction(nameof(Index));
        }

        private (bool Allowed, string? Reason) CanChange(Appointment app)
        {
            if (TerminalStatuses.Contains(app.TrangThai))
            {
                return (false, "Lịch hẹn này không còn ở trạng thái có thể đổi/hủy.");
            }

            if (app.ThoiGian - DateTime.Now < TimeSpan.FromHours(ChangeCutoffHours))
            {
                return (false, $"Chỉ có thể tự đổi/hủy lịch trước giờ hẹn tối thiểu {ChangeCutoffHours} giờ. Vui lòng gọi hotline để được hỗ trợ.");
            }

            return (true, null);
        }

        private async Task<QuanLyBenhVien.Models.Patient?> GetCurrentPatientAsync()
        {
            var userId = GetCurrentUserId();
            return await _context.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.NguoiDungId == userId);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
