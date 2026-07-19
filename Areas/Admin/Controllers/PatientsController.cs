using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Helpers;
using QuanLyBenhVien.Models;
using QuanLyBenhVien.Services;

namespace QuanLyBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class PatientsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ExcelExportService _excel;

        public PatientsController(ApplicationDbContext context, ExcelExportService excel)
        {
            _context = context;
            _excel = excel;
        }

        // GET: Admin/Patients
        public async Task<IActionResult> Index(string searchString, string status, int page = 1, int? pageSize = null)
        {
            var query = BuildQuery(searchString, status);

            var paged = await query.ToPagedListAsync(page, PagedList<QuanLyBenhVien.Models.Patient>.NormalisePageSize(pageSize));

            ViewBag.SearchString = searchString;
            ViewBag.StatusFilter = status;

            // Cohort tiles describe the whole filtered set, not just this page.
            ViewBag.TotalMatching = paged.TotalCount;
            ViewBag.ActiveCount = await query.CountAsync(p => p.User.TrangThai == "Active");
            ViewBag.BlockedCount = await query.CountAsync(p => p.User.TrangThai != "Active");
            ViewBag.InsuredCount = await query.CountAsync(p => p.SoBHYT != null && p.SoBHYT != "");

            return View(paged);
        }

        // GET: Admin/Patients/Export
        public async Task<IActionResult> Export(string searchString, string status)
        {
            var patients = await BuildQuery(searchString, status).ToListAsync();

            var columns = new List<ExcelColumn<QuanLyBenhVien.Models.Patient>>
            {
                new("Mã BN", p => $"BN-{p.Id:D4}"),
                new("Họ và tên", p => p.User.HoTen),
                new("Ngày sinh", p => p.NgaySinh.ToString("dd/MM/yyyy")),
                new("Giới tính", p => p.GioiTinh),
                new("Nhóm máu", p => p.NhomMau),
                new("Số CCCD", p => p.SoCCCD),
                new("Số BHYT", p => p.SoBHYT),
                new("Số điện thoại", p => p.User.Sdt),
                new("Email", p => p.User.Email),
                new("Trạng thái", p => p.User.TrangThai == "Active" ? "Hoạt động" : "Bị khóa"),
                new("Ngày tạo hồ sơ", p => p.User.NgayTao.ToString("dd/MM/yyyy"))
            };

            var content = _excel.Build(
                "Benh nhan",
                "DANH SÁCH BỆNH NHÂN",
                columns,
                patients,
                BuildFilterSummary(searchString, status));

            return File(content, ExcelExportService.ContentType, ExcelExportService.FileName("danh-sach-benh-nhan"));
        }

        private IQueryable<QuanLyBenhVien.Models.Patient> BuildQuery(string searchString, string status)
        {
            var query = _context.Patients
                .Include(p => p.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(p => p.User.HoTen.Contains(searchString) ||
                                         p.User.Sdt.Contains(searchString) ||
                                         p.SoBHYT.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(p => p.User.TrangThai == status);
            }

            return query.OrderBy(p => p.User.HoTen);
        }

        private static string BuildFilterSummary(string searchString, string status)
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(searchString))
            {
                parts.Add($"Từ khóa: {searchString}");
            }
            if (!string.IsNullOrEmpty(status))
            {
                parts.Add($"Trạng thái: {(status == "Active" ? "Hoạt động" : "Bị khóa")}");
            }
            return parts.Count == 0 ? "Toàn bộ hồ sơ" : string.Join(" • ", parts);
        }

        // GET: Admin/Patients/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (patient == null) return NotFound();

            // Historical Appointments of this patient
            ViewBag.Appointments = await _context.Appointments
                .Include(a => a.Doctor.User)
                .Include(a => a.Doctor.Department)
                .Where(a => a.BenhNhanId == patient.Id)
                .OrderByDescending(a => a.ThoiGian)
                .ToListAsync();

            // Historical Examination Records (Medical History)
            ViewBag.ExaminationRecords = await _context.ExaminationRecords
                .Include(e => e.Appointment.Doctor.User)
                .Where(e => e.Appointment.BenhNhanId == patient.Id)
                .OrderByDescending(e => e.NgayKham)
                .ToListAsync();

            // Historical Invoices & payment status
            ViewBag.Invoices = await _context.Invoices
                .Include(i => i.ExaminationRecord.Appointment.Doctor.User)
                .Where(i => i.ExaminationRecord.Appointment.BenhNhanId == patient.Id)
                .OrderByDescending(i => i.NgayTao)
                .ToListAsync();

            return View(patient);
        }
    }
}
