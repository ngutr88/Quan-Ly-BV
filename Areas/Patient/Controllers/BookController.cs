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
        private readonly ApplicationDbContext _context;

        public BookController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Patient/Book
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var depts = await _context.Departments.ToListAsync();
            ViewBag.Departments = depts;
            return View();
        }

        // GET: /Patient/Book/GetDoctors?deptId=5
        [HttpGet]
        public async Task<IActionResult> GetDoctors(int deptId)
        {
            var doctors = await _context.Doctors
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

            // Exclude weekends (hospital rests)
            if (parsedDate.DayOfWeek == DayOfWeek.Saturday || parsedDate.DayOfWeek == DayOfWeek.Sunday)
            {
                return Json(new { success = false, message = "Bệnh viện nghỉ làm việc thứ 7 và Chủ nhật." });
            }

            // Define standard slots in shifts (morning 8-12, afternoon 13:30-17:30)
            var standardSlots = new List<string>
            {
                "08:00", "08:30", "09:00", "09:30", "10:00", "10:30", "11:00", "11:30",
                "13:30", "14:00", "14:30", "15:00", "15:30", "16:00", "16:30", "17:00"
            };

            // Query existing appointments of this doctor on that day
            var bookedTimes = await _context.Appointments
                .Where(a => a.BacSiId == doctorId && a.ThoiGian.Date == parsedDate.Date && a.TrangThai != "DaHuy")
                .Select(a => a.ThoiGian.ToString("HH:mm"))
                .ToListAsync();

            // Calculate free slots
            var availableSlots = standardSlots
                .Where(s => !bookedTimes.Contains(s))
                .Select(s => new { time = s, label = s })
                .ToList();

            return Json(new { success = true, slots = availableSlots });
        }

        // POST: /Patient/Book/ConfirmBooking
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmBooking(int deptId, int doctorId, DateTime bookingDate, string bookingTime, string lyDo)
        {
            var patientUserId = GetCurrentUserId();
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.NguoiDungId == patientUserId);

            if (patient == null) return NotFound();

            // Parse time
            var timeParts = bookingTime.Split(':');
            var appointmentTime = bookingDate.Date
                .AddHours(int.Parse(timeParts[0]))
                .AddMinutes(int.Parse(timeParts[1]));

            // Double check slot conflict on DB level to ensure safety
            var exists = await _context.Appointments
                .AnyAsync(a => a.BacSiId == doctorId && a.ThoiGian == appointmentTime && a.TrangThai != "DaHuy");

            if (exists)
            {
                TempData["ErrorMessage"] = "Khung giờ này đã có người đặt trước. Vui lòng chọn khung giờ khác.";
                return RedirectToAction(nameof(Index));
            }

            // Create appointment
            var app = new Appointment
            {
                BenhNhanId = patient.Id,
                BacSiId = doctorId,
                ThoiGian = appointmentTime,
                TrangThai = "ChoXacNhan",
                LyDoKham = lyDo,
                NgayTao = DateTime.Now
            };

            _context.Appointments.Add(app);

            // Audit Log
            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = patientUserId,
                HanhDong = "Đăng ký khám",
                ChiTiet = $"Bệnh nhân {patient.User.HoTen} đặt lịch hẹn trực tuyến với bác sĩ ID {doctorId} vào lúc {appointmentTime}."
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đặt lịch thành công! Mã số hẹn của bạn là #LK-{app.Id.ToString("D4")}. Vui lòng chờ nhân viên xác nhận.";
            return RedirectToAction("Index", "Dashboard");
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 3;
        }
    }
}
