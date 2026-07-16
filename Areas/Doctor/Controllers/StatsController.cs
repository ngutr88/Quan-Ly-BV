using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;

namespace QuanLyBenhVien.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize(Roles = "Doctor")]
    public class StatsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public StatsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var currentUserId = GetCurrentUserId();
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Department)
                .FirstOrDefaultAsync(d => d.NguoiDungId == currentUserId);

            if (doctor == null) return NotFound("Bác sĩ không tồn tại trong hệ thống.");

            var now = DateTime.Now;
            var thisMonthStart = new DateTime(now.Year, now.Month, 1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);
            var thisYearStart = new DateTime(now.Year, 1, 1);

            // All completed appointments
            var allCompleted = await _context.Appointments
                .Include(a => a.Patient.User)
                .Where(a => a.BacSiId == doctor.Id && a.TrangThai == "HoanThanh")
                .OrderByDescending(a => a.ThoiGian)
                .ToListAsync();

            // Summary stats
            ViewBag.TotalCompleted = allCompleted.Count;
            ViewBag.ThisMonthCompleted = allCompleted.Count(a => a.ThoiGian >= thisMonthStart);
            ViewBag.LastMonthCompleted = allCompleted.Count(a => a.ThoiGian >= lastMonthStart && a.ThoiGian < thisMonthStart);
            ViewBag.ThisYearCompleted = allCompleted.Count(a => a.ThoiGian >= thisYearStart);

            // Monthly breakdown for last 6 months
            var monthlyCounts = Enumerable.Range(0, 6)
                .Select(i => {
                    var monthStart = thisMonthStart.AddMonths(-i);
                    var monthEnd = monthStart.AddMonths(1);
                    return new {
                        Label = monthStart.ToString("MM/yyyy"),
                        Count = allCompleted.Count(a => a.ThoiGian >= monthStart && a.ThoiGian < monthEnd)
                    };
                })
                .Reverse()
                .ToList();
            ViewBag.MonthlyLabels = monthlyCounts.Select(m => m.Label).ToList();
            ViewBag.MonthlyCounts = monthlyCounts.Select(m => m.Count).ToList();
            ViewBag.MaxMonthlyCount = monthlyCounts.Max(m => m.Count) > 0 ? monthlyCounts.Max(m => m.Count) : 1;

            // Reviews / rating
            var reviews = await _context.Reviews
                .Where(r => r.BacSiId == doctor.Id)
                .OrderByDescending(r => r.NgayTao)
                .ToListAsync();
            ViewBag.TotalReviews = reviews.Count;
            ViewBag.AvgRating = reviews.Any() ? Math.Round(reviews.Average(r => r.SoSao), 1) : 5.0;
            ViewBag.Rating5 = reviews.Count(r => r.SoSao == 5);
            ViewBag.Rating4 = reviews.Count(r => r.SoSao == 4);
            ViewBag.Rating3 = reviews.Count(r => r.SoSao == 3);
            ViewBag.Rating2 = reviews.Count(r => r.SoSao == 2);
            ViewBag.Rating1 = reviews.Count(r => r.SoSao == 1);
            ViewBag.RecentReviews = reviews.Take(5).ToList();

            // Top returning patients — use List of string[] to avoid anonymous type in ViewBag
            var topPatients = allCompleted
                .GroupBy(a => a.Patient.User.HoTen)
                .Select(g => new { Name = g.Key, Count = g.Count(), LastVisit = g.Max(a => a.ThoiGian) })
                .OrderByDescending(x => x.Count)
                .Take(5)
                .Select(x => new List<object> { x.Name, x.Count, x.LastVisit })
                .ToList();
            ViewBag.TopPatients = topPatients;

            // Today stats
            ViewBag.TodayCompleted = allCompleted.Count(a => a.ThoiGian.Date == DateTime.Today);
            ViewBag.TodayPending = await _context.Appointments
                .CountAsync(a => a.BacSiId == doctor.Id
                    && a.ThoiGian.Date == DateTime.Today
                    && (a.TrangThai == "DaXacNhan" || a.TrangThai == "ChoXacNhan"));

            ViewBag.DoctorProfile = doctor;
            return View();
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
