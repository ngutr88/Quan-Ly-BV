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
                .Include(i => i.ExaminationRecord.Appointment.Doctor.Department)
                .Include(i => i.InvoiceDetails) 
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
        public async Task<IActionResult> Simulate(int id, string paymentMethod)
        {
            var patientUserId = GetCurrentUserId();
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.NguoiDungId == patientUserId);
            if (patient == null) return NotFound();

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == id && i.ExaminationRecord.Appointment.BenhNhanId == patient.Id);

            if (invoice == null || (invoice.TrangThaiThanhToan != "ChuaThanhToan" && invoice.TrangThaiThanhToan != "ThanhToanThatBai" && invoice.TrangThaiThanhToan != "QuaHan"))
            {
                return RedirectToAction(nameof(Index));
            }

            ViewBag.PaymentMethod = paymentMethod ?? "vnpay";
            return View(invoice);
        }

        // POST: /Patient/Payment/PaymentCallback/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PaymentCallback(int id, string paymentMethod, string paymentStatus, string transactionId)
        {
            var patientUserId = GetCurrentUserId();
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.NguoiDungId == patientUserId);
            if (patient == null) return NotFound();

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == id && i.ExaminationRecord.Appointment.BenhNhanId == patient.Id);

            if (invoice == null) return NotFound();

            string methodDisplayName = paymentMethod switch
            {
                "vnpay" => "Online (VNPay)",
                "momo" => "Online (MoMo)",
                "zalopay" => "Online (ZaloPay)",
                "chuyenkhoan" => "Chuyển khoản",
                "tai_quay" => "Thanh toán tại quầy",
                _ => "Online"
            };

            if (paymentStatus == "success")
            {
                invoice.TrangThaiThanhToan = "DaThanhToan";
                invoice.PhuongThuc = methodDisplayName;
                invoice.NgayThanhToan = DateTime.Now;
                invoice.MaGiaoDich = string.IsNullOrEmpty(transactionId) ? "TXN" + DateTime.Now.Ticks.ToString().Substring(10) : transactionId;

                // Log audit
                _context.AuditLogs.Add(new AuditLog
                {
                    NguoiDungId = patientUserId,
                    HanhDong = "Thanh toán trực tuyến thành công",
                    ChiTiet = $"Bệnh nhân {patient.User.HoTen} thanh toán hóa đơn HD-{id.ToString("D5")} thành công qua {methodDisplayName}. Mã GD: {invoice.MaGiaoDich}. Số tiền: {invoice.TongTien:N0}đ."
                });

                // Create Notification
                _context.Notifications.Add(new Notification
                {
                    NguoiDungId = patientUserId,
                    NoiDung = $"[ThanhToan] Hóa đơn thanh toán|Hóa đơn số #INV-{invoice.Id:D4} đã được thanh toán thành công qua {methodDisplayName}.",
                    NgayGui = DateTime.Now,
                    DaDoc = false
                });

                TempData["SuccessMessage"] = $"Giao dịch thành công! Hóa đơn HD-{id.ToString("D5")} đã được thanh toán thành công qua {methodDisplayName}.";
            }
            else if (paymentStatus == "failed")
            {
                invoice.TrangThaiThanhToan = "ThanhToanThatBai";
                invoice.PhuongThuc = methodDisplayName;
                invoice.MaGiaoDich = transactionId;

                // Log audit
                _context.AuditLogs.Add(new AuditLog
                {
                    NguoiDungId = patientUserId,
                    HanhDong = "Thanh toán trực tuyến thất bại",
                    ChiTiet = $"Giao dịch thanh toán hóa đơn HD-{id.ToString("D5")} qua {methodDisplayName} thất bại. Số tiền: {invoice.TongTien:N0}đ."
                });

                // Create Notification
                _context.Notifications.Add(new Notification
                {
                    NguoiDungId = patientUserId,
                    NoiDung = $"[ThanhToan] Thanh toán thất bại|Giao dịch thanh toán hóa đơn #INV-{invoice.Id:D4} qua {methodDisplayName} không thành công.",
                    NgayGui = DateTime.Now,
                    DaDoc = false
                });

                TempData["ErrorMessage"] = $"Thanh toán thất bại cho hóa đơn HD-{id.ToString("D5")}. Vui lòng kiểm tra và thử lại.";
            }
            else if (paymentStatus == "processing")
            {
                invoice.TrangThaiThanhToan = "DangXuLy";
                invoice.PhuongThuc = methodDisplayName;
                invoice.MaGiaoDich = transactionId;

                // Log audit
                _context.AuditLogs.Add(new AuditLog
                {
                    NguoiDungId = patientUserId,
                    HanhDong = "Giao dịch thanh toán đang xử lý",
                    ChiTiet = $"Yêu cầu thanh toán hóa đơn HD-{id.ToString("D5")} qua {methodDisplayName} đang chờ xử lý."
                });

                // Create Notification
                _context.Notifications.Add(new Notification
                {
                    NguoiDungId = patientUserId,
                    NoiDung = $"[ThanhToan] Đang xử lý|Giao dịch thanh toán cho hóa đơn #INV-{invoice.Id:D4} qua {methodDisplayName} đang được xử lý.",
                    NgayGui = DateTime.Now,
                    DaDoc = false
                });

                TempData["WarningMessage"] = $"Giao dịch thanh toán hóa đơn HD-{id.ToString("D5")} đang được xử lý.";
            }

            _context.Entry(invoice).State = EntityState.Modified;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // POST: /Patient/Payment/SendEmailReceipt/5
        [HttpPost]
        public async Task<IActionResult> SendEmailReceipt(int id)
        {
            var patientUserId = GetCurrentUserId();
            var patient = await _context.Patients
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.NguoiDungId == patientUserId);
            if (patient == null) return Json(new { success = false, message = "Bệnh nhân không tìm thấy." });

            var invoice = await _context.Invoices
                .FirstOrDefaultAsync(i => i.Id == id && i.ExaminationRecord.Appointment.BenhNhanId == patient.Id);

            if (invoice == null) return Json(new { success = false, message = "Hóa đơn không tồn tại hoặc không thuộc quyền sở hữu của bạn." });

            // Log audit
            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = patientUserId,
                HanhDong = "Gửi biên lai qua email",
                ChiTiet = $"Biên lai hóa đơn HD-{invoice.Id.ToString("D5")} đã được gửi thành công tới email {patient.User.Email}."
            });

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = $"Biên lai đã được gửi thành công đến email: {patient.User.Email}!" });
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
