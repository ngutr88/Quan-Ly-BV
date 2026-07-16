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
    public class InvoicesController : Controller
    {
        private readonly ApplicationDbContext _context;

        public InvoicesController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Invoices
        public async Task<IActionResult> Index(string statusFilter, string searchString)
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

            var invoices = await query.OrderByDescending(i => i.NgayTao).ToListAsync();
            ViewBag.StatusFilter = statusFilter;
            ViewBag.SearchString = searchString;

            return View(invoices);
        }

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
        public async Task<IActionResult> PayCounter(int id, string phuongThuc)
        {
            var invoice = await _context.Invoices
                .Include(i => i.ExaminationRecord.Appointment.Patient.User)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (invoice == null) return NotFound();

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
