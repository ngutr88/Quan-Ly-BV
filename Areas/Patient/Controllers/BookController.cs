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
        private const int BookingHorizonDays = 14;

        private readonly ApplicationDbContext _context;

        public BookController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Patient/Book
        [HttpGet]
        public async Task<IActionResult> Index(int? deptId)
        {
            var patientUserId = GetCurrentUserId();
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.NguoiDungId == patientUserId);
            if (patient == null) return NotFound();

            ViewBag.Patient = patient;
            ViewBag.Departments = await _context.Departments.ToListAsync();

            // Carries the department chosen on the public site through the login
            // redirect, so the visitor does not have to pick it a second time.
            ViewBag.PreselectedDeptId = deptId;
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
                .Where(d => d.KhoaId == deptId && d.User.TrangThai == "Active")
                .Select(d => new { id = d.Id, name = $"{d.HocVi} {d.User.HoTen} ({d.ChuyenKhoa})" })
                .ToListAsync();

            return Json(doctors);
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

            var availableSlots = await GetAvailableSlotsAsync(doctorId, parsedDate.Date);
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

            var availableSlots = await GetAvailableSlotsAsync(doctorId, bookingDate.Date);
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

        private async Task<IReadOnlyList<string>> GetAvailableSlotsAsync(int doctorId, DateTime date)
        {
            var dayOfWeek = (int)date.DayOfWeek;
            // SQLite cannot reliably translate ordering by TimeSpan. Materialize the
            // filtered rows first, then sort in memory to keep this endpoint responsive.
            var schedules = (await _context.DoctorWorkSchedules
                .AsNoTracking()
                .Where(s => s.BacSiId == doctorId &&
                            s.ThuTrongTuan == dayOfWeek &&
                            s.DangHoatDong &&
                            (s.HieuLucTu == null || s.HieuLucTu.Value.Date <= date.Date) &&
                            (s.HieuLucDen == null || s.HieuLucDen.Value.Date >= date.Date))
                .ToListAsync())
                .OrderBy(s => s.GioBatDau)
                .ToList();

            if (!schedules.Any())
            {
                return Array.Empty<string>();
            }

            var bookedTimes = await _context.Appointments
                .Where(a => a.BacSiId == doctorId &&
                            a.ThoiGian.Date == date.Date &&
                            a.TrangThai != "DaHuy")
                .Select(a => a.ThoiGian.TimeOfDay)
                .ToListAsync();

            var bookedSet = bookedTimes.ToHashSet();
            var now = DateTime.Now;
            var slots = new SortedSet<string>(StringComparer.Ordinal);

            foreach (var schedule in schedules)
            {
                var duration = TimeSpan.FromMinutes(schedule.ThoiLuongKhamPhut);
                for (var slot = schedule.GioBatDau; slot + duration <= schedule.GioKetThuc; slot += duration)
                {
                    var slotDateTime = date.Date.Add(slot);
                    if (slotDateTime <= now || bookedSet.Contains(slot))
                    {
                        continue;
                    }

                    slots.Add(slot.ToString(@"hh\:mm"));
                }
            }

            return slots.ToList();
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
