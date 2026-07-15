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

            // 8. 7-day appointment statistics for Chart.js
            var sevenDaysAgo = DateTime.Today.AddDays(-6);
            var appointmentsLast7Days = await _context.Appointments
                .Where(a => a.ThoiGian.Date >= sevenDaysAgo)
                .ToListAsync();

            var appointmentData = Enumerable.Range(0, 7)
                .Select(offset => sevenDaysAgo.AddDays(offset))
                .Select(date => new {
                    DateStr = date.ToString("dd/MM"),
                    Count = appointmentsLast7Days.Count(a => a.ThoiGian.Date == date)
                })
                .ToList();

            ViewBag.ChartLabels = appointmentData.Select(x => x.DateStr).ToArray();
            ViewBag.ChartData = appointmentData.Select(x => x.Count).ToArray();

            // 9. 7-day revenue statistics for Chart.js
            var paidInvoicesLast7Days = await _context.Invoices
                .Where(i => i.TrangThaiThanhToan == "DaThanhToan" && i.NgayThanhToan.HasValue && i.NgayThanhToan.Value.Date >= sevenDaysAgo)
                .ToListAsync();

            var revenueData = Enumerable.Range(0, 7)
                .Select(offset => sevenDaysAgo.AddDays(offset))
                .Select(date => new {
                    Total = paidInvoicesLast7Days.Where(i => i.NgayThanhToan.HasValue && i.NgayThanhToan.Value.Date == date).Sum(i => i.TongTien)
                })
                .ToList();

            ViewBag.RevenueChartData = revenueData.Select(x => x.Total).ToArray();

            // ==========================================
            // REPORT DATA INJECTION
            // ==========================================
            var currentYear = DateTime.Today.Year;
            
            // A. Annual revenue
            ViewBag.AnnualRevenue = paidInvoices
                .Where(i => i.NgayThanhToan.HasValue && i.NgayThanhToan.Value.Year == currentYear)
                .Sum(i => i.TongTien);

            // B. Appointment count & success rate this year
            var appointmentsThisYear = await _context.Appointments
                .Where(a => a.ThoiGian.Year == currentYear)
                .ToListAsync();
            ViewBag.TotalAppointmentsThisYear = appointmentsThisYear.Count;
            ViewBag.CompletionRate = appointmentsThisYear.Any() 
                ? (double)appointmentsThisYear.Count(a => a.TrangThai == "HoanThanh") / appointmentsThisYear.Count * 100 
                : 0.0;

            // C. Revenue by Department
            var revenueByDept = await _context.Invoices
                .Where(i => i.TrangThaiThanhToan == "DaThanhToan" && i.NgayThanhToan.HasValue)
                .Include(i => i.ExaminationRecord.Appointment.Doctor.Department)
                .GroupBy(i => i.ExaminationRecord.Appointment.Doctor.Department.TenKhoa)
                .Select(g => new
                {
                    Department = g.Key,
                    Revenue = g.Sum(i => i.TongTien)
                })
                .ToListAsync();

            ViewBag.RevenueByDeptLabels = revenueByDept.Select(r => r.Department).ToArray();
            ViewBag.RevenueByDeptData = revenueByDept.Select(r => r.Revenue).ToArray();

            // D. Appointments by Department
            var appointmentsByDept = await _context.Appointments
                .Include(a => a.Doctor.Department)
                .GroupBy(a => a.Doctor.Department.TenKhoa)
                .Select(g => new
                {
                    Department = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            ViewBag.AppByDeptLabels = appointmentsByDept.Select(r => r.Department).ToArray();
            ViewBag.AppByDeptData = appointmentsByDept.Select(r => r.Count).ToArray();

            // E. Monthly Revenue labels & data (T1 to T12)
            var paidInvoicesThisYear = paidInvoices
                .Where(i => i.NgayThanhToan.HasValue && i.NgayThanhToan.Value.Year == currentYear)
                .ToList();
            var monthlyRevenue = Enumerable.Range(1, 12)
                .Select(month => new
                {
                    Month = $"T{month}",
                    Total = paidInvoicesThisYear.Where(i => i.NgayThanhToan.Value.Month == month).Sum(i => i.TongTien)
                })
                .ToList();

            ViewBag.MonthlyRevenueLabels = monthlyRevenue.Select(m => m.Month).ToArray();
            ViewBag.MonthlyRevenueData = monthlyRevenue.Select(m => m.Total).ToArray();

            // F. Doctor performance (Top 5 leaderboard)
            var doctorPerf = await _context.Appointments
                .Where(a => a.TrangThai == "HoanThanh")
                .Include(a => a.Doctor.User)
                .Include(a => a.Doctor.Department)
                .GroupBy(a => new { a.Doctor.Id, a.Doctor.User.HoTen, a.Doctor.Department.TenKhoa })
                .Select(g => new DashboardDoctorPerformanceDto
                {
                    DoctorName = g.Key.HoTen,
                    Department = g.Key.TenKhoa,
                    CompletedCount = g.Count()
                })
                .OrderByDescending(x => x.CompletedCount)
                .Take(5)
                .ToListAsync();

            ViewBag.DoctorPerformance = doctorPerf;

            return View();
        }
    }

    public class DashboardDoctorPerformanceDto
    {
        public string DoctorName { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public int CompletedCount { get; set; }
    }
}
