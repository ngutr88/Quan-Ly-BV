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

namespace QuanLyBenhVien.Areas.Patient.Controllers
{
    [Area("Patient")]
    [Authorize(Roles = "Patient")]
    public class BookController : Controller
    {
        // Until the system configuration module is available, keep these booking
        // rules centralized so GET slot lookup and POST confirmation stay aligned.
        private const int BookingHorizonDays = 14;
        private static readonly HashSet<string> BookableSlots = new(StringComparer.Ordinal)
        {
            "08:00", "08:30", "09:00", "09:30", "10:00", "10:30", "11:00", "11:30",
            "13:30", "14:00", "14:30", "15:00", "15:30", "16:00", "16:30", "17:00"
        };

        private readonly ApplicationDbContext _context;

        public BookController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Patient/Book
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var patientUserId = GetCurrentUserId();
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.NguoiDungId == patientUserId);
            if (patient == null) return NotFound();

            var depts = await _context.Departments.ToListAsync();
            var dependents = await _context.Dependents
                .Where(d => d.BenhNhanId == patient.Id)
                .ToListAsync();

            ViewBag.Patient = patient;
            ViewBag.Departments = depts;
            ViewBag.Dependents = dependents;
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
                .Where(d => d.KhoaId == deptId && d.User.TrangThai == "Active")
                .Select(d => new { id = d.Id, name = $"{d.HocVi} {d.User.HoTen} ({d.ChuyenKhoa})" })
                .ToListAsync();

            return Json(doctors);
        }

        // GET: /Patient/Book/GetSlots?doctorId=5&date=2026-06-22
        [HttpGet]
        public async Task<IActionResult> GetSlots(int doctorId, string date)
        {
            if (!DateTime.TryParse(date, out DateTime parsedDate))
            {
                return BadRequest("Định dạng ngày không hợp lệ.");
            }

            var doctorExists = await _context.Doctors
                .AsNoTracking()
                .AnyAsync(d => d.Id == doctorId && d.User.TrangThai == "Active");

            if (!doctorExists)
            {
                return Json(new { success = false, message = "Bác sĩ không tồn tại hoặc đang tạm ngưng nhận lịch." });
            }

            var dateValidation = ValidateBookingDate(parsedDate.Date);
            if (dateValidation != null)
            {
                return Json(new { success = false, message = dateValidation });
            }

            var bookedAppointments = await _context.Appointments
                .Where(a => a.BacSiId == doctorId && a.ThoiGian.Date == parsedDate.Date && a.TrangThai != "DaHuy")
                .Select(a => a.ThoiGian)
                .ToListAsync();

            var bookedTimes = bookedAppointments.Select(t => t.ToString("HH:mm")).ToList();
            var now = DateTime.Now;
            var availableSlots = BookableSlots
                .Where(s => !bookedTimes.Contains(s))
                .Where(s =>
                {
                    if (parsedDate.Date != DateTime.Today) return true;

                    var parts = s.Split(':');
                    var slotTime = DateTime.Today.AddHours(int.Parse(parts[0])).AddMinutes(int.Parse(parts[1]));
                    return slotTime > now;
                })
                .Select(s => new { time = s, label = s })
                .ToList();

            return Json(new { success = true, slots = availableSlots });
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
                .FirstOrDefaultAsync(d => d.Id == doctorId && d.KhoaId == deptId && d.User.TrangThai == "Active");

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
                if (!int.TryParse(bookingFor, out int depId))
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

            if (string.IsNullOrWhiteSpace(bookingTime) || !BookableSlots.Contains(bookingTime))
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

            var doctorSlotTaken = await _context.Appointments
                .AnyAsync(a => a.BacSiId == doctorId && a.ThoiGian == appointmentTime && a.TrangThai != "DaHuy");

            if (doctorSlotTaken)
            {
                TempData["ErrorMessage"] = "Khung giờ này đã có người đặt trước. Vui lòng chọn khung giờ khác.";
                await transaction.RollbackAsync();
                return RedirectToAction(nameof(Index));
            }

            var patientSlotTaken = await _context.Appointments
                .AnyAsync(a => a.BenhNhanId == patient.Id && a.ThoiGian == appointmentTime && a.TrangThai != "DaHuy");

            if (patientSlotTaken)
            {
                TempData["ErrorMessage"] = "Tài khoản của bạn đã có lịch khám trong cùng khung giờ này.";
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

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

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

            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
            {
                return "Bệnh viện chưa nhận đặt lịch vào thứ 7 và Chủ nhật.";
            }

            return null;
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 3;
        }
    }
}
