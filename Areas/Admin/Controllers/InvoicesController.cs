using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Helpers;
using QuanLyBenhVien.Models;
using System.Security.Claims;

namespace QuanLyBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class InvoicesController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly QuanLyBenhVien.Services.ExcelExportService _excel;

        public InvoicesController(ApplicationDbContext context, QuanLyBenhVien.Services.ExcelExportService excel)
        {
            _context = context;
            _excel = excel;
        }

        // GET: Admin/Invoices
        public async Task<IActionResult> Index(string statusFilter, string searchString, int page = 1, int? pageSize = null)
        {
            var query = BuildQuery(statusFilter, searchString);

            var paged = await query.ToPagedListAsync(page, QuanLyBenhVien.Helpers.PagedList<Invoice>.NormalisePageSize(pageSize));

            ViewBag.StatusFilter = statusFilter;
            ViewBag.SearchString = searchString;

            // Revenue tiles must total the filtered set, not the visible page.
            ViewBag.TotalMatching = paged.TotalCount;
            ViewBag.PaidCount = await query.CountAsync(i => i.TrangThaiThanhToan == "DaThanhToan");
            ViewBag.PaidAmount = await query.Where(i => i.TrangThaiThanhToan == "DaThanhToan").SumAsync(i => (decimal?)i.TongTien) ?? 0m;
            ViewBag.UnpaidCount = await query.CountAsync(i => i.TrangThaiThanhToan == "ChuaThanhToan");
            ViewBag.UnpaidAmount = await query.Where(i => i.TrangThaiThanhToan == "ChuaThanhToan").SumAsync(i => (decimal?)i.TongTien) ?? 0m;
            ViewBag.CancelledCount = await query.CountAsync(i => i.TrangThaiThanhToan == "DaHuy");

            return View(paged);
        }

        // GET: Admin/Invoices/Export
        public async Task<IActionResult> Export(string statusFilter, string searchString)
        {
            var invoices = await BuildQuery(statusFilter, searchString).ToListAsync();

            var columns = new List<QuanLyBenhVien.Services.ExcelColumn<Invoice>>
            {
                new("Mã hóa đơn", i => $"HD-{i.Id:D5}"),
                new("Ngày tạo", i => i.NgayTao.ToString("dd/MM/yyyy HH:mm")),
                new("Bệnh nhân", i => i.ExaminationRecord.Appointment.Patient.User.HoTen),
                new("Tổng tiền (VNĐ)", i => i.TongTien, "#,##0"),
                new("Trạng thái", i => PaymentLabel(i.TrangThaiThanhToan)),
                new("Phương thức", i => MethodLabel(i.PhuongThuc)),
                new("Mã giao dịch", i => i.MaGiaoDich ?? ""),
                new("Ngày thanh toán", i => i.NgayThanhToan.HasValue ? i.NgayThanhToan.Value.ToString("dd/MM/yyyy HH:mm") : "")
            };

            var content = _excel.Build(
                "Hoa don",
                "DANH SÁCH HÓA ĐƠN",
                columns,
                invoices,
                string.IsNullOrEmpty(statusFilter) ? "Toàn bộ hóa đơn" : $"Trạng thái: {PaymentLabel(statusFilter)}");

            return File(content,
                QuanLyBenhVien.Services.ExcelExportService.ContentType,
                QuanLyBenhVien.Services.ExcelExportService.FileName("danh-sach-hoa-don"));
        }

        private IQueryable<Invoice> BuildQuery(string statusFilter, string searchString)
        {
            var query = _context.Invoices
                .Include(i => i.ExaminationRecord.Appointment.Patient.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(statusFilter))
            {
                query = query.Where(i => i.TrangThaiThanhToan == statusFilter);
            }

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(i => i.ExaminationRecord.Appointment.Patient.User.HoTen.Contains(searchString));
            }

            return query.OrderByDescending(i => i.NgayTao);
        }

        private static string PaymentLabel(string status) => status switch
        {
            "DaThanhToan" => "Đã thanh toán",
            "ChuaThanhToan" => "Chưa thanh toán",
            "DaHuy" => "Đã hủy",
            _ => status
        };

        private static string MethodLabel(string method) => method switch
        {
            "TienMat" => "Tiền mặt",
            "ChuyenKhoan" => "Chuyển khoản",
            "Online" => "Thanh toán online",
            _ => method
        };

        // GET: Admin/Invoices/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .Include(i => i.ExaminationRecord.Appointment.Patient.User)
                .Include(i => i.ExaminationRecord.Appointment.Doctor.User)
                .Include(i => i.ExaminationRecord.Appointment.Doctor.Department)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound();

            return View(invoice);
        }

        // POST: Admin/Invoices/PayCounter/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayCounter(int id, string phuongThuc)
        {
            var invoice = await _context.Invoices
                .Include(i => i.ExaminationRecord.Appointment.Patient.User)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound();

            if (invoice.TrangThaiThanhToan == "DaThanhToan")
            {
                TempData["SuccessMessage"] = $"Hóa đơn HD-{id.ToString("D5")} đã được thanh toán trước đó.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (phuongThuc != "TienMat" && phuongThuc != "ChuyenKhoan")
            {
                TempData["ErrorMessage"] = "Phương thức thanh toán không hợp lệ.";
                return RedirectToAction(nameof(Details), new { id });
            }

            invoice.TrangThaiThanhToan = "DaThanhToan";
            invoice.PhuongThuc = phuongThuc; // "TienMat" or "ChuyenKhoan"
            invoice.NgayThanhToan = DateTime.Now;

            _context.Entry(invoice).State = EntityState.Modified;

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Xác nhận hóa đơn",
                ChiTiet = $"Thu ngân xác nhận hóa đơn HD-{id.ToString("D5")} trị giá {invoice.TongTien:N0}đ của BN {invoice.ExaminationRecord.Appointment.Patient.User.HoTen} thanh toán bằng {phuongThuc}."
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã thu tiền và thanh toán thành công hóa đơn HD-{id.ToString("D5")}.";
            return RedirectToAction(nameof(Details), new { id = id });
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
