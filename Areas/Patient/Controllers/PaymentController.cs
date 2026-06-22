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
    public class PaymentController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PaymentController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Patient/Payment
        public async Task<IActionResult> Index()
        {
            var patientUserId = GetCurrentUserId();
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.NguoiDungId == patientUserId);
            if (patient == null) return NotFound();

            var invoices = await _context.Invoices
                .Include(i => i.ExaminationRecord.Appointment.Doctor.User)
                .Where(i => i.ExaminationRecord.Appointment.BenhNhanId == patient.Id)
                .OrderByDescending(i => i.NgayTao)
                .ToListAsync();

            return View(invoices);
        }

        // GET: /Patient/Payment/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var patientUserId = GetCurrentUserId();
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.NguoiDungId == patientUserId);
            if (patient == null) return NotFound();

            var invoice = await _context.Invoices
                .Include(i => i.InvoiceDetails)
                .Include(i => i.ExaminationRecord.Appointment.Doctor.User)
                .Include(i => i.ExaminationRecord.Appointment.Doctor.Department)
                .FirstOrDefaultAsync(i => i.Id == id && i.ExaminationRecord.Appointment.BenhNhanId == patient.Id);

            if (invoice == null) return NotFound("Hóa đơn không tồn tại hoặc bạn không có quyền xem.");

            return View(invoice);
        }

        // GET: /Patient/Payment/Simulate/5
        [HttpGet]
        public async Task<IActionResult> Simulate(int id)
        {
            var patientUserId = GetCurrentUserId();
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.NguoiDungId == patientUserId);
            if (patient == null) return NotFound();

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == id && i.ExaminationRecord.Appointment.BenhNhanId == patient.Id);

            if (invoice == null || invoice.TrangThaiThanhToan != "ChuaThanhToan")
            {
                return RedirectToAction(nameof(Index));
            }

            return View(invoice);
        }

        // POST: /Patient/Payment/PaymentCallback/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PaymentCallback(int id)
        {
            var patientUserId = GetCurrentUserId();
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.NguoiDungId == patientUserId);
            if (patient == null) return NotFound();

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == id && i.ExaminationRecord.Appointment.BenhNhanId == patient.Id);

            if (invoice == null) return NotFound();

            if (invoice.TrangThaiThanhToan == "ChuaThanhToan")
            {
                invoice.TrangThaiThanhToan = "DaThanhToan";
                invoice.PhuongThuc = "Online (VNPay/Momo)";
                invoice.NgayThanhToan = DateTime.Now;

                _context.Entry(invoice).State = EntityState.Modified;

                // Log audit
                _context.AuditLogs.Add(new AuditLog
                {
                    NguoiDungId = patientUserId,
                    HanhDong = "Thanh toán trực tuyến",
                    ChiTiet = $"Bệnh nhân {patient.User.HoTen} thanh toán hóa đơn HD-{id.ToString("D5")} thành công qua ví điện tử. Tổng số tiền: {invoice.TongTien:N0}đ."
                });

                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Giao dịch thành công! Hóa đơn HD-{id.ToString("D5")} đã được thanh toán trực tuyến.";
            }

            return RedirectToAction("Index", "Dashboard");
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 3;
        }
    }
}
