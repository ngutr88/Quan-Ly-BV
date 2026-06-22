using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Models;
using System.Security.Claims;

namespace QuanLyBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class AppointmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AppointmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Appointments
        public async Task<IActionResult> Index(DateTime? filterDate, string statusFilter)
        {
            var query = _context.Appointments
                .Include(a => a.Patient.User)
                .Include(a => a.Doctor.User)
                .Include(a => a.Doctor.Department)
                .AsQueryable();

            if (filterDate.HasValue)
            {
                query = query.Where(a => a.ThoiGian.Date == filterDate.Value.Date);
            }

            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(a => a.TrangThai == statusFilter);
            }

            var appointments = await query.OrderByDescending(a => a.ThoiGian).ToListAsync();
            ViewBag.FilterDate = filterDate?.ToString("yyyy-MM-dd");
            ViewBag.StatusFilter = statusFilter;

            return View(appointments);
        }

        // GET: Admin/Appointments/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var appointment = await _context.Appointments
                .Include(a => a.Patient.User)
                .Include(a => a.Doctor.User)
                .Include(a => a.Doctor.Department)
                .FirstOrDefaultAsync(a => a.Id == id);

            if (appointment == null) return NotFound();

            return View(appointment);
        }

        // POST: Admin/Appointments/Confirm/5
        [HttpPost]
        public async Task<IActionResult> Confirm(int id)
        {
            var app = await _context.Appointments.Include(a => a.Patient.User).FirstOrDefaultAsync(a => a.Id == id);
            if (app == null) return NotFound();

            app.TrangThai = "DaXacNhan";
            _context.Entry(app).State = EntityState.Modified;

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Xác nhận lịch khám",
                ChiTiet = $"Admin xác nhận lịch khám #{id} cho BN {app.Patient.User.HoTen} vào lúc {app.ThoiGian}."
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xác nhận lịch hẹn #{id} thành công.";
            return RedirectToAction(nameof(Details), new { id = id });
        }

        // POST: Admin/Appointments/Cancel/5
        [HttpPost]
        public async Task<IActionResult> Cancel(int id, string lyDoHuy)
        {
            var app = await _context.Appointments.Include(a => a.Patient.User).FirstOrDefaultAsync(a => a.Id == id);
            if (app == null) return NotFound();

            app.TrangThai = "DaHuy";
            app.LyDoKham = $"[ĐÃ HỦY - Lý do: {lyDoHuy}] " + app.LyDoKham;
            _context.Entry(app).State = EntityState.Modified;

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Hủy lịch khám",
                ChiTiet = $"Admin hủy lịch khám #{id} cho BN {app.Patient.User.HoTen}. Lý do: {lyDoHuy}."
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã hủy lịch hẹn #{id}.";
            return RedirectToAction(nameof(Details), new { id = id });
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 1;
        }
    }
}
