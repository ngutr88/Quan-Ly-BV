using System;
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
    public class DashboardController : Controller
    {
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

            if (patient == null) return NotFound("Hồ sơ bệnh nhân không tồn tại.");

            // 1. Upcoming Appointments
            var upcomingApps = await _context.Appointments
                .Include(a => a.Doctor.User)
                .Include(a => a.Doctor.Department)
                .Where(a => a.BenhNhanId == patient.Id && a.ThoiGian >= DateTime.Now && a.TrangThai != "DaHuy")
                .OrderBy(a => a.ThoiGian)
                .ToListAsync();

            // 2. Recent Prescriptions
            var recentPrescriptions = await _context.Prescriptions
                .Include(p => p.ExaminationRecord.Appointment.Doctor.User)
                .Where(p => p.ExaminationRecord.Appointment.BenhNhanId == patient.Id)
                .OrderByDescending(p => p.NgayKe)
                .Take(5)
                .ToListAsync();

            // 3. Unpaid Invoices count
            ViewBag.UnpaidCount = await _context.Invoices
                .Where(i => i.ExaminationRecord.Appointment.BenhNhanId == patient.Id && i.TrangThaiThanhToan == "ChuaThanhToan")
                .CountAsync();

            ViewBag.Patient = patient;
            ViewBag.RecentPres = recentPrescriptions;

            return View(upcomingApps);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 3; // Default seeded patient user id
        }
    }
}
