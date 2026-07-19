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

            if (patient == null)
            {
                var email = User.FindFirstValue(ClaimTypes.Email);
                var identityName = User.Identity?.Name;
                patient = await _context.Patients
                    .Include(p => p.User)
                    .FirstOrDefaultAsync(p => p.User.Email == email || p.User.Email == identityName || p.User.HoTen == identityName);
            }

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

            // 3. Outstanding balance drives the payment call-to-action.
            var unpaidInvoices = await _context.Invoices
                .Include(i => i.ExaminationRecord.Appointment.Doctor.User)
                .Where(i => i.ExaminationRecord.Appointment.BenhNhanId == patient.Id
                            && i.TrangThaiThanhToan == "ChuaThanhToan")
                .OrderByDescending(i => i.NgayTao)
                .ToListAsync();

            ViewBag.UnpaidCount = unpaidInvoices.Count;
            ViewBag.UnpaidAmount = unpaidInvoices.Sum(i => i.TongTien);
            ViewBag.UnpaidInvoices = unpaidInvoices.Take(3).ToList();

            // 4. Treatment history summary shown on the health profile card.
            var examRecords = await _context.ExaminationRecords
                .Include(e => e.Appointment.Doctor.User)
                .Where(e => e.Appointment.BenhNhanId == patient.Id)
                .OrderByDescending(e => e.NgayKham)
                .ToListAsync();

            ViewBag.TotalVisits = examRecords.Count;
            ViewBag.LastVisit = examRecords.FirstOrDefault()?.NgayKham;

            // Most recent record that actually carries vital signs — an empty
            // follow-up visit should not blank out the patient's last readings.
            ViewBag.LatestVitals = examRecords
                .FirstOrDefault(e => e.CanNang.HasValue || e.ChieuCao.HasValue
                                     || e.NhietDo.HasValue || e.BMI.HasValue);

            // 5. Notifications
            ViewBag.RecentNotifications = await _context.Notifications
                .Where(n => n.NguoiDungId == patient.NguoiDungId)
                .OrderByDescending(n => n.NgayGui)
                .Take(4)
                .ToListAsync();

            ViewBag.UnreadNotifications = await _context.Notifications
                .CountAsync(n => n.NguoiDungId == patient.NguoiDungId && !n.DaDoc);

            ViewBag.NextAppointment = upcomingApps.FirstOrDefault();
            ViewBag.Patient = patient;
            ViewBag.RecentPres = recentPrescriptions;

            return View(upcomingApps);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
