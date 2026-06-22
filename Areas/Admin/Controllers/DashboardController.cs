using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Models;

namespace QuanLyBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. KPI Stats
            ViewBag.DoctorCount = await _context.Doctors.CountAsync();
            ViewBag.DepartmentCount = await _context.Departments.CountAsync();
            ViewBag.PatientCount = await _context.Patients.CountAsync();
            ViewBag.NewPatientsToday = await _context.Users
                .Where(u => u.VaiTro == "Patient" && u.NgayTao.Date == DateTime.Today)
                .CountAsync();

            // 2. Appointment Stats
            var todayAppointments = await _context.Appointments
                .Where(a => a.ThoiGian.Date == DateTime.Today)
                .ToListAsync();

            ViewBag.TotalAppointmentsToday = todayAppointments.Count;
            ViewBag.PendingAppCount = todayAppointments.Count(a => a.TrangThai == "ChoXacNhan");
            ViewBag.CompletedAppCount = todayAppointments.Count(a => a.TrangThai == "HoanThanh");
            ViewBag.ConfirmedAppCount = todayAppointments.Count(a => a.TrangThai == "DaXacNhan");
            ViewBag.CancelledAppCount = todayAppointments.Count(a => a.TrangThai == "DaHuy");

            // 3. Revenue Stats
            var paidInvoices = await _context.Invoices
                .Where(i => i.TrangThaiThanhToan == "DaThanhToan")
                .ToListAsync();

            ViewBag.RevenueToday = paidInvoices
                .Where(i => i.NgayThanhToan.HasValue && i.NgayThanhToan.Value.Date == DateTime.Today)
                .Sum(i => i.TongTien);
            ViewBag.RevenueThisMonth = paidInvoices
                .Where(i => i.NgayThanhToan.HasValue && i.NgayThanhToan.Value.Month == DateTime.Today.Month && i.NgayThanhToan.Value.Year == DateTime.Today.Year)
                .Sum(i => i.TongTien);

            // 4. Low stock medicines alert
            ViewBag.LowStockMedicines = await _context.Medicines
                .Where(m => m.TonKho <= m.NguongToiThieu)
                .Take(5)
                .ToListAsync();

            // 5. Near Expiry batches alert (expires in <= 30 days)
            var expiryThreshold = DateTime.Today.AddDays(30);
            ViewBag.NearExpiryBatches = await _context.MedicineBatches
                .Include(b => b.Medicine)
                .Where(b => b.SoLuongTon > 0 && b.HanSuDung <= expiryThreshold)
                .Take(5)
                .ToListAsync();

            // 6. Recent Appointments list
            ViewBag.RecentAppointments = await _context.Appointments
                .Include(a => a.Patient.User)
                .Include(a => a.Doctor.User)
                .Include(a => a.Doctor.Department)
                .OrderByDescending(a => a.ThoiGian)
                .Take(5)
                .ToListAsync();

            // 7. Recent System Logs
            ViewBag.RecentLogs = await _context.AuditLogs
                .Include(l => l.User)
                .OrderByDescending(l => l.ThoiGian)
                .Take(5)
                .ToListAsync();

            return View();
        }
    }
}
