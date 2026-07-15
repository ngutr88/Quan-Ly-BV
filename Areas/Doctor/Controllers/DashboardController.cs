using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Models;

namespace QuanLyBenhVien.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize(Roles = "Doctor")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserId = GetCurrentUserId();
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .FirstOrDefaultAsync(d => d.NguoiDungId == currentUserId);

            if (doctor == null)
            {
                // Fallback for older cookies that may not contain a user ID.
                var identityValue = User.Identity?.Name;
                doctor = await _context.Doctors
                    .Include(d => d.User)
                    .FirstOrDefaultAsync(d => d.User.HoTen == identityValue || d.User.Email == identityValue);
            }

            if (doctor == null) return NotFound("Bác sĩ không tồn tại trong hệ thống.");

            // Stats
            var allAppointments = await _context.Appointments
                .Where(a => a.BacSiId == doctor.Id)
                .ToListAsync();

            ViewBag.TotalVisits = allAppointments.Count(a => a.TrangThai == "HoanThanh");
            
            var todayApps = allAppointments
                .Where(a => a.ThoiGian.Date == DateTime.Today)
                .ToList();
            
            ViewBag.TodayTotal = todayApps.Count;
            ViewBag.TodayCompleted = todayApps.Count(a => a.TrangThai == "HoanThanh");
            ViewBag.TodayPending = todayApps.Count(a => a.TrangThai == "ChoXacNhan" || a.TrangThai == "DaXacNhan");

            var reviews = await _context.Reviews.Where(r => r.BacSiId == doctor.Id).ToListAsync();
            ViewBag.AvgRating = reviews.Any() ? reviews.Average(r => r.SoSao) : 5.0;

            // Today's Queue list
            var queue = await _context.Appointments
                .Include(a => a.Patient.User)
                .Where(a => a.BacSiId == doctor.Id && a.ThoiGian.Date == DateTime.Today && a.TrangThai != "DaHuy")
                .OrderBy(a => a.ThoiGian)
                .ToListAsync();

            ViewBag.DoctorProfile = doctor;
            return View(queue);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
