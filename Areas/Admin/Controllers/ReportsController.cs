using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Services;

namespace QuanLyBenhVien.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize(Roles = "Admin")]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ExcelExportService _excel;

    public ReportsController(ApplicationDbContext context, ExcelExportService excel)
    {
        _context = context;
        _excel = excel;
    }

    public async Task<IActionResult> Index(DateTime? fromDate, DateTime? toDate)
    {
        // Default to the current month so the page always opens on a real period.
        var from = (fromDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
        var to = (toDate ?? DateTime.Today).Date;
        if (to < from)
        {
            (from, to) = (to, from);
        }

        // Inclusive of the whole end day.
        var toExclusive = to.AddDays(1);

        var appointments = await _context.Appointments
            .Include(a => a.Doctor).ThenInclude(d => d.Department)
            .Where(a => a.ThoiGian >= from && a.ThoiGian < toExclusive)
            .ToListAsync();

        var invoices = await _context.Invoices
            .Where(i => i.NgayTao >= from && i.NgayTao < toExclusive)
            .ToListAsync();

        var paidInvoices = invoices.Where(i => i.TrangThaiThanhToan == "DaThanhToan").ToList();

        ViewBag.FromDate = from.ToString("yyyy-MM-dd");
        ViewBag.ToDate = to.ToString("yyyy-MM-dd");

        ViewBag.TotalAppointments = appointments.Count;
        ViewBag.CompletedAppointments = appointments.Count(a => a.TrangThai == "HoanThanh");
        ViewBag.CancelledAppointments = appointments.Count(a => a.TrangThai == "DaHuy");
        ViewBag.PendingAppointments = appointments.Count(a => a.TrangThai == "ChoXacNhan");

        ViewBag.Revenue = paidInvoices.Sum(i => i.TongTien);
        ViewBag.OutstandingRevenue = invoices
            .Where(i => i.TrangThaiThanhToan == "ChuaThanhToan")
            .Sum(i => i.TongTien);
        ViewBag.PaidInvoiceCount = paidInvoices.Count;
        ViewBag.InvoiceCount = invoices.Count;

        ViewBag.NewPatients = await _context.Patients
            .CountAsync(p => p.User.NgayTao >= from && p.User.NgayTao < toExclusive);

        // Department workload drives the "where is the hospital busy" table.
        ViewBag.DepartmentBreakdown = appointments
            .Where(a => a.Doctor != null)
            .GroupBy(a => a.Doctor.Department.TenKhoa)
            .Select(g => new DepartmentReportRow
            {
                Department = g.Key,
                Total = g.Count(),
                Completed = g.Count(a => a.TrangThai == "HoanThanh"),
                Cancelled = g.Count(a => a.TrangThai == "DaHuy")
            })
            .OrderByDescending(r => r.Total)
            .ToList();

        ViewBag.PaymentMethods = paidInvoices
            .GroupBy(i => i.PhuongThuc)
            .Select(g => new PaymentMethodRow
            {
                Method = g.Key,
                Count = g.Count(),
                Amount = g.Sum(i => i.TongTien)
            })
            .OrderByDescending(r => r.Amount)
            .ToList();

        return View();
    }

    // GET: Admin/Reports/Export
    public async Task<IActionResult> Export(DateTime? fromDate, DateTime? toDate)
    {
        var from = (fromDate ?? new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1)).Date;
        var to = (toDate ?? DateTime.Today).Date;
        if (to < from)
        {
            (from, to) = (to, from);
        }
        var toExclusive = to.AddDays(1);

        var appointments = await _context.Appointments
            .Include(a => a.Doctor).ThenInclude(d => d.Department)
            .Where(a => a.ThoiGian >= from && a.ThoiGian < toExclusive)
            .ToListAsync();

        var rows = appointments
            .Where(a => a.Doctor != null)
            .GroupBy(a => a.Doctor.Department.TenKhoa)
            .Select(g => new DepartmentReportRow
            {
                Department = g.Key,
                Total = g.Count(),
                Completed = g.Count(a => a.TrangThai == "HoanThanh"),
                Cancelled = g.Count(a => a.TrangThai == "DaHuy")
            })
            .OrderByDescending(r => r.Total)
            .ToList();

        var columns = new List<ExcelColumn<DepartmentReportRow>>
        {
            new("Khoa", r => r.Department),
            new("Tổng lượt khám", r => r.Total),
            new("Hoàn thành", r => r.Completed),
            new("Đã hủy", r => r.Cancelled),
            new("Tỉ lệ hoàn thành (%)", r => r.Total == 0 ? 0d : Math.Round(r.Completed * 100.0 / r.Total, 1), "0.0")
        };

        var content = _excel.Build(
            "Bao cao theo khoa",
            "BÁO CÁO TẢI CÔNG VIỆC THEO KHOA",
            columns,
            rows,
            $"Kỳ báo cáo {from:dd/MM/yyyy} – {to:dd/MM/yyyy}");

        return File(content, ExcelExportService.ContentType, ExcelExportService.FileName("bao-cao-theo-khoa"));
    }
}

public class DepartmentReportRow
{
    public string Department { get; set; } = string.Empty;
    public int Total { get; set; }
    public int Completed { get; set; }
    public int Cancelled { get; set; }
}

public class PaymentMethodRow
{
    public string Method { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
}
