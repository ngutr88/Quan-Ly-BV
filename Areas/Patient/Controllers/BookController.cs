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
using QuanLyBenhVien.Services;
using static QuanLyBenhVien.Helpers.DoctorDisplayHelper;

namespace QuanLyBenhVien.Areas.Patient.Controllers
{
    [Area("Patient")]
    [Authorize(Roles = "Patient")]
    public class BookController : Controller
    {
        private const int BookingHorizonDays = 14;

        private readonly ApplicationDbContext _context;
        private readonly DoctorDashboardNotifier _notifier;
        private readonly AppointmentSlotService _slotService;

        public BookController(ApplicationDbContext context, DoctorDashboardNotifier notifier, AppointmentSlotService slotService)
        {
            _context = context;
            _notifier = notifier;
            _slotService = slotService;
        }

        // GET: /Patient/Book
        [HttpGet]
        public async Task<IActionResult> Index(int? deptId, int? doctorId)
        {
            var patientUserId = GetCurrentUserId();
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.NguoiDungId == patientUserId);
            if (patient == null) return NotFound();

            ViewBag.Patient = patient;
            ViewBag.Departments = await _context.Departments.ToListAsync();

            // Carries the department (and, from the follow-up reminder card,
            // the same doctor too) chosen elsewhere through to this form so the
            // patient doesn't have to pick it a second time.
            ViewBag.PreselectedDeptId = deptId;
            ViewBag.PreselectedDoctorId = doctorId;
            ViewBag.Dependents = await _context.Dependents
                .Where(d => d.BenhNhanId == patient.Id)
                .ToListAsync();
            ViewBag.BookingMaxDate = DateTime.Today.AddDays(BookingHorizonDays);

            return View();
        }

        // GET: /Patient/Book/GetDoctors?deptId=5
        [HttpGet]
        public async Task<IActionResult> GetDoctors(int deptId)
        {
            var doctors = await _context.Doctors
                .AsNoTracking()
                .Include(d => d.User)
                .Where(d => d.KhoaId == deptId && !d.DaXoa && d.User.TrangThai == "Active")
                .ToListAsync();

            var result = doctors.Select(d => new { id = d.Id, name = $"{FormatDoctorName(d)} ({d.ChuyenKhoa})" });
            return Json(result);
        }

        // GET: /Patient/Book/GetSlots?doctorId=5&date=2026-06-22
        [HttpGet]
        public async Task<IActionResult> GetSlots(int doctorId, string date)
        {
            if (!DateTime.TryParse(date, out var parsedDate))
            {
                return BadRequest("Định dạng ngày không hợp lệ.");
            }

            var doctorExists = await _context.Doctors
                .AsNoTracking()
                .AnyAsync(d => d.Id == doctorId && !d.DaXoa && d.User.TrangThai == "Active");

            if (!doctorExists)
            {
                return Json(new { success = false, message = "Bác sĩ không tồn tại hoặc đang tạm ngưng nhận lịch." });
            }

            var dateValidation = ValidateBookingDate(parsedDate.Date);
            if (dateValidation != null)
            {
                return Json(new { success = false, message = dateValidation });
            }

            var availableSlots = await _slotService.GetAvailableSlotsAsync(doctorId, parsedDate.Date);
            if (!availableSlots.Any())
            {
                return Json(new { success = false, message = "Bác sĩ không có ca làm việc hoặc slot trống trong ngày đã chọn." });
            }

            return Json(new
            {
                success = true,
                slots = availableSlots.Select(s => new { time = s, label = s }).ToList()
            });
        }

        // POST: /Patient/Book/ConfirmBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(int deptId, int doctorId, DateTime bookingDate, string bookingTime, string lyDo, string bookingFor)
        {
            var patientUserId = GetCurrentUserId();
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.NguoiDungId == patientUserId);

            if (patient == null) return NotFound();

            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.Id == doctorId && d.KhoaId == deptId && !d.DaXoa && d.User.TrangThai == "Active");

            if (doctor == null)
            {
                TempData["ErrorMessage"] = "Bác sĩ hoặc khoa khám không hợp lệ. Vui lòng chọn lại.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(lyDo) || lyDo.Trim().Length < 5)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do khám hoặc triệu chứng đủ rõ ràng.";
                return RedirectToAction(nameof(Index));
            }

            var appointmentReason = lyDo.Trim();
            if (bookingFor != "Self")
            {
                if (!int.TryParse(bookingFor, out var depId))
                {
                    TempData["ErrorMessage"] = "Thông tin người bệnh đăng ký không hợp lệ.";
                    return RedirectToAction(nameof(Index));
                }

                var dependent = await _context.Dependents
                    .FirstOrDefaultAsync(d => d.Id == depId && d.BenhNhanId == patient.Id);

                if (dependent == null)
                {
                    TempData["ErrorMessage"] = "Hồ sơ người thân không hợp lệ hoặc không thuộc tài khoản của bạn.";
                    return RedirectToAction(nameof(Index));
                }

                appointmentReason = $"[Đặt lịch hộ: {dependent.HoTen} ({dependent.QuanHe})] {appointmentReason}";
            }

            if (string.IsNullOrWhiteSpace(bookingTime))
            {
                TempData["ErrorMessage"] = "Khung giờ khám không hợp lệ. Vui lòng chọn một slot còn trống.";
                return RedirectToAction(nameof(Index));
            }

            var dateValidation = ValidateBookingDate(bookingDate.Date);
            if (dateValidation != null)
            {
                TempData["ErrorMessage"] = dateValidation;
                return RedirectToAction(nameof(Index));
            }

            var availableSlots = await _slotService.GetAvailableSlotsAsync(doctorId, bookingDate.Date);
            if (!availableSlots.Contains(bookingTime))
            {
                TempData["ErrorMessage"] = "Khung giờ này không nằm trong lịch làm việc còn trống của bác sĩ.";
                return RedirectToAction(nameof(Index));
            }

            var timeParts = bookingTime.Split(':');
            var appointmentTime = bookingDate.Date
                .AddHours(int.Parse(timeParts[0]))
                .AddMinutes(int.Parse(timeParts[1]));

            if (appointmentTime <= DateTime.Now)
            {
                TempData["ErrorMessage"] = "Không thể đặt lịch hẹn ở thời điểm đã qua.";
                return RedirectToAction(nameof(Index));
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();

            // Đối chiếu lại slot ngay bên trong transaction để giảm rủi ro
            // race-condition giữa hai lần đặt lịch gần nhau (đã kiểm tra sơ bộ ở
            // GetAvailableSlotsAsync phía trên, đây là lần kiểm tra "chốt" cuối cùng).
            var slotValidation = await _slotService.ValidateSlotAsync(doctorId, appointmentTime, patient.Id);
            if (!slotValidation.Success)
            {
                TempData["ErrorMessage"] = slotValidation.ErrorMessage;
                await transaction.RollbackAsync();
                return RedirectToAction(nameof(Index));
            }

            var app = new Appointment
            {
                BenhNhanId = patient.Id,
                BacSiId = doctorId,
                ThoiGian = appointmentTime,
                TrangThai = "ChoXacNhan",
                LyDoKham = appointmentReason,
                NgayTao = DateTime.Now
            };

            _context.Appointments.Add(app);
            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = patientUserId,
                HanhDong = "Đăng ký khám",
                ChiTiet = $"Bệnh nhân {patient.User.HoTen} đặt lịch hẹn trực tuyến với {doctor.User.HoTen} vào lúc {appointmentTime:HH:mm dd/MM/yyyy}. Lý do: {appointmentReason}"
            });
            _context.Notifications.Add(new Notification
            {
                NguoiDungId = patientUserId,
                NoiDung = $"[LichKham] Đăng ký lịch khám|Yêu cầu đặt lịch khám vào lúc {appointmentTime:HH:mm dd/MM/yyyy} đã được gửi thành công và đang chờ xác nhận.",
                NgayGui = DateTime.Now,
                DaDoc = false
            });
            _context.Notifications.Add(new Notification
            {
                NguoiDungId = doctor.NguoiDungId,
                NoiDung = $"[LichKham] Lịch hẹn mới|Bệnh nhân {patient.User.HoTen} vừa đặt lịch khám vào lúc {appointmentTime:HH:mm dd/MM/yyyy}.",
                NgayGui = DateTime.Now,
                DaDoc = false
            });

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            await _notifier.NotifyQueueUpdatedAsync(doctorId);
            await _notifier.NotifyNotificationCountChangedAsync(doctorId);

            TempData["SuccessMessage"] = $"Đặt lịch thành công! Mã số hẹn của bạn là #LK-{app.Id.ToString("D4")}. Vui lòng chờ nhân viên xác nhận.";
            return RedirectToAction("Index", "Dashboard");
        }

        private static string? ValidateBookingDate(DateTime date)
        {
            if (date < DateTime.Today)
            {
                return "Ngày đặt khám phải lớn hơn hoặc bằng ngày hiện tại.";
            }

            if (date > DateTime.Today.AddDays(BookingHorizonDays))
            {
                return $"Chỉ có thể đặt lịch trong vòng {BookingHorizonDays} ngày tới.";
            }

            return null;
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
