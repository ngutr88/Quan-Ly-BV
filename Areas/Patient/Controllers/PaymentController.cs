using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Models;
using QuanLyBenhVien.Services;

namespace QuanLyBenhVien.Areas.Patient.Controllers
{
    [Area("Patient")]
    [Authorize(Roles = "Patient")]
    public class PaymentController : Controller
    {
        // "Cần thanh toán" = 3 trạng thái này (Yêu cầu 1) - hóa đơn Thất bại
        // VẪN đang nợ, không được bỏ sót.
        private static readonly string[] PayableStatuses = { "ChuaThanhToan", "ThanhToanThatBai", "QuaHan" };
        private static readonly string[] OpenTransactionStatuses = { "ChoXuLy", "DangXuLy" };

        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(ApplicationDbContext context, IEmailSender emailSender, ILogger<PaymentController> logger)
        {
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
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

        // POST: /Patient/Payment/Confirm - màn xác nhận trước khi khởi tạo giao
        // dịch thật (Yêu cầu 2: xác nhận số tiền + chọn phương thức trước khi
        // sang cổng). Số tiền hiển thị luôn tính lại ở đây, không nhận từ client.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Confirm(int[] invoiceIds)
        {
            if (invoiceIds == null || invoiceIds.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ít nhất 1 hóa đơn để thanh toán.";
                return RedirectToAction(nameof(Index));
            }

            var patientUserId = GetCurrentUserId();
            var patient = await _context.Patients.FirstOrDefaultAsync(p => p.NguoiDungId == patientUserId);
            if (patient == null) return NotFound();

            var invoices = await _context.Invoices
                .Include(i => i.GiaoDichThanhToanHienTai)
                .Where(i => invoiceIds.Contains(i.Id) && i.ExaminationRecord.Appointment.BenhNhanId == patient.Id)
                .ToListAsync();

            if (invoices.Count != invoiceIds.Length)
            {
                TempData["ErrorMessage"] = "Một số hóa đơn không tồn tại hoặc không thuộc quyền của bạn.";
                return RedirectToAction(nameof(Index));
            }

            // Chống trả trùng khi F5/bấm 2 lần: nếu đã có giao dịch đang mở cho
            // 1 trong các hóa đơn này, đưa thẳng về ĐÚNG giao dịch đó thay vì
            // tạo giao dịch mới - kiểm tra TRƯỚC khi xét trạng thái hóa đơn, vì
            // hóa đơn đang DangXuLy không còn nằm trong PayableStatuses nữa.
            var pendingTransaction = invoices
                .Select(i => i.GiaoDichThanhToanHienTai)
                .FirstOrDefault(t => t != null && OpenTransactionStatuses.Contains(t.TrangThai));
            if (pendingTransaction != null)
            {
                return RedirectToAction(nameof(Simulate), new { key = pendingTransaction.IdempotencyKey });
            }

            if (invoices.Any(i => !PayableStatuses.Contains(i.TrangThaiThanhToan)))
            {
                TempData["ErrorMessage"] = "Một số hóa đơn đã chọn không còn ở trạng thái có thể thanh toán (có thể đã được thanh toán trước đó).";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.TotalAmount = invoices.Sum(i => i.TongTien);
            ViewBag.InvoiceIds = invoiceIds;
            return View(invoices);
        }

        // POST: /Patient/Payment/InitiatePayment - tạo giao dịch thật, re-check
        // toàn bộ (không tin dữ liệu đã qua bước Confirm), bọc transaction DB
        // theo đúng khuôn ApproveReceipt/ExamController.CompleteSession.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InitiatePayment(int[] invoiceIds, string paymentMethod)
        {
            var allowedMethods = new[] { "vnpay", "momo", "zalopay", "chuyenkhoan" };
            if (invoiceIds == null || invoiceIds.Length == 0 || !allowedMethods.Contains(paymentMethod))
            {
                TempData["ErrorMessage"] = "Thiếu thông tin thanh toán hoặc phương thức không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var patientUserId = GetCurrentUserId();
            var patient = await _context.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.NguoiDungId == patientUserId);
            if (patient == null) return NotFound();

            try
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();

                var invoices = await _context.Invoices
                    .Include(i => i.GiaoDichThanhToanHienTai)
                    .Where(i => invoiceIds.Contains(i.Id) && i.ExaminationRecord.Appointment.BenhNhanId == patient.Id)
                    .ToListAsync();

                if (invoices.Count != invoiceIds.Length)
                {
                    TempData["ErrorMessage"] = "Một số hóa đơn không tồn tại hoặc không thuộc quyền của bạn.";
                    return RedirectToAction(nameof(Index));
                }

                var pendingTransaction = invoices
                    .Select(i => i.GiaoDichThanhToanHienTai)
                    .FirstOrDefault(t => t != null && OpenTransactionStatuses.Contains(t.TrangThai));
                if (pendingTransaction != null)
                {
                    return RedirectToAction(nameof(Simulate), new { key = pendingTransaction.IdempotencyKey });
                }

                if (invoices.Any(i => !PayableStatuses.Contains(i.TrangThaiThanhToan)))
                {
                    TempData["ErrorMessage"] = "Danh sách hóa đơn đã thay đổi, vui lòng thử lại.";
                    return RedirectToAction(nameof(Index));
                }

                var paymentTransaction = new PaymentTransaction
                {
                    NguoiKhoiTaoId = patientUserId,
                    IdempotencyKey = Guid.NewGuid().ToString("N"),
                    SoTien = invoices.Sum(i => i.TongTien), // TÍNH LẠI Ở SERVER - không nhận từ client
                    PhuongThuc = paymentMethod,
                    TrangThai = "ChoXuLy"
                };
                _context.PaymentTransactions.Add(paymentTransaction);
                await _context.SaveChangesAsync(); // sinh Id cho giao dịch trước khi hóa đơn tham chiếu tới

                foreach (var invoice in invoices)
                {
                    invoice.GiaoDichThanhToanHienTaiId = paymentTransaction.Id;
                    invoice.TrangThaiThanhToan = "DangXuLy";
                }

                _context.AuditLogs.Add(new AuditLog
                {
                    NguoiDungId = patientUserId,
                    HanhDong = "Khởi tạo giao dịch thanh toán",
                    ChiTiet = $"{patient.User.HoTen} khởi tạo thanh toán {invoices.Count} hóa đơn, tổng {paymentTransaction.SoTien:N0}đ qua {MethodLabel(paymentMethod)}.",
                    DoiTuongLoai = "GiaoDichThanhToan",
                    DoiTuongId = paymentTransaction.Id
                });

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return RedirectToAction(nameof(Simulate), new { key = paymentTransaction.IdempotencyKey });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi khởi tạo giao dịch thanh toán cho bệnh nhân {PatientUserId}", patientUserId);
                TempData["ErrorMessage"] = "Không thể khởi tạo giao dịch thanh toán lúc này. Vui lòng thử lại sau ít phút.";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /Patient/Payment/Simulate?key=... - "cổng thanh toán" mô phỏng.
        [HttpGet]
        public async Task<IActionResult> Simulate(string key)
        {
            var patientUserId = GetCurrentUserId();
            var paymentTransaction = await _context.PaymentTransactions
                .Include(t => t.Invoices)
                .FirstOrDefaultAsync(t => t.IdempotencyKey == key && t.NguoiKhoiTaoId == patientUserId);

            if (paymentTransaction == null) return NotFound();

            if (!OpenTransactionStatuses.Contains(paymentTransaction.TrangThai))
            {
                return RedirectToAction(nameof(PaymentReturn), new { key });
            }

            return View(paymentTransaction);
        }

        // POST: /Patient/Payment/Webhook - NGUỒN CHÂN LÝ DUY NHẤT cho trạng
        // thái giao dịch (Yêu cầu 3). Trong bản mô phỏng này, endpoint được gọi
        // bởi chính trình duyệt (từ Simulate.cshtml) thay vì server cổng thanh
        // toán thật; khi tích hợp cổng thật, đổi xác thực sang chữ ký/secret
        // riêng của cổng (không phải cookie đăng nhập) và bỏ ValidateAntiForgeryToken.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Webhook(string key, string status, string? gatewayTransactionId)
        {
            if (string.IsNullOrWhiteSpace(key)) return BadRequest("Thiếu mã giao dịch.");
            if (status != "success" && status != "failed" && status != "processing")
                return BadRequest("Trạng thái không hợp lệ.");

            try
            {
                await using var dbTransaction = await _context.Database.BeginTransactionAsync();

                var paymentTransaction = await _context.PaymentTransactions
                    .Include(t => t.Invoices)
                    .FirstOrDefaultAsync(t => t.IdempotencyKey == key);

                if (paymentTransaction == null) return NotFound();

                var patientUserId = paymentTransaction.NguoiKhoiTaoId;
                var methodLabel = MethodLabel(paymentTransaction.PhuongThuc);

                // Chống trả trùng: webhook gọi lại nhiều lần (hoặc F5 ở trang mô
                // phỏng) không được xử lý lại - chỉ nhận khi giao dịch còn mở.
                if (!OpenTransactionStatuses.Contains(paymentTransaction.TrangThai))
                {
                    _context.AuditLogs.Add(new AuditLog
                    {
                        NguoiDungId = patientUserId,
                        HanhDong = "Bỏ qua webhook trùng lặp",
                        ChiTiet = $"Giao dịch {key} đã ở trạng thái chốt ({paymentTransaction.TrangThai}), bỏ qua webhook status={status}.",
                        DoiTuongLoai = "GiaoDichThanhToan",
                        DoiTuongId = paymentTransaction.Id
                    });
                    await _context.SaveChangesAsync();
                    await dbTransaction.CommitAsync();
                    return RedirectToAction(nameof(PaymentReturn), new { key });
                }

                if (status == "success")
                {
                    paymentTransaction.TrangThai = "ThanhCong";
                    paymentTransaction.MaGiaoDichCong = string.IsNullOrWhiteSpace(gatewayTransactionId)
                        ? GenerateFallbackTransactionCode(paymentTransaction.PhuongThuc)
                        : gatewayTransactionId;
                    paymentTransaction.NgayCapNhat = DateTime.Now;

                    foreach (var invoice in paymentTransaction.Invoices)
                    {
                        invoice.TrangThaiThanhToan = "DaThanhToan";
                        invoice.PhuongThuc = methodLabel;
                        invoice.NgayThanhToan = DateTime.Now;
                        invoice.MaGiaoDich = paymentTransaction.MaGiaoDichCong;

                        _context.AuditLogs.Add(new AuditLog
                        {
                            NguoiDungId = patientUserId,
                            HanhDong = "Thanh toán trực tuyến thành công",
                            ChiTiet = $"Hóa đơn HD-{invoice.Id:D5} thanh toán thành công qua {methodLabel}. Số tiền: {invoice.TongTien:N0}đ. Mã GD: {paymentTransaction.MaGiaoDichCong}.",
                            DoiTuongLoai = "HoaDon",
                            DoiTuongId = invoice.Id
                        });
                    }

                    _context.AuditLogs.Add(new AuditLog
                    {
                        NguoiDungId = patientUserId,
                        HanhDong = "Giao dịch thanh toán thành công",
                        ChiTiet = $"Giao dịch {key} thành công, {paymentTransaction.Invoices.Count} hóa đơn, tổng {paymentTransaction.SoTien:N0}đ qua {methodLabel}.",
                        DoiTuongLoai = "GiaoDichThanhToan",
                        DoiTuongId = paymentTransaction.Id
                    });

                    _context.Notifications.Add(new Notification
                    {
                        NguoiDungId = patientUserId,
                        NoiDung = $"[ThanhToan] Thanh toán thành công|Đã thanh toán thành công {paymentTransaction.Invoices.Count} hóa đơn, tổng {paymentTransaction.SoTien:N0}đ qua {methodLabel}.",
                        NgayGui = DateTime.Now,
                        DaDoc = false
                    });
                }
                else if (status == "failed")
                {
                    paymentTransaction.TrangThai = "ThatBai";
                    paymentTransaction.NgayCapNhat = DateTime.Now;

                    foreach (var invoice in paymentTransaction.Invoices)
                    {
                        invoice.TrangThaiThanhToan = "ThanhToanThatBai";

                        _context.AuditLogs.Add(new AuditLog
                        {
                            NguoiDungId = patientUserId,
                            HanhDong = "Thanh toán trực tuyến thất bại",
                            ChiTiet = $"Hóa đơn HD-{invoice.Id:D5} thanh toán thất bại qua {methodLabel}. Số tiền: {invoice.TongTien:N0}đ.",
                            DoiTuongLoai = "HoaDon",
                            DoiTuongId = invoice.Id
                        });
                    }

                    _context.AuditLogs.Add(new AuditLog
                    {
                        NguoiDungId = patientUserId,
                        HanhDong = "Giao dịch thanh toán thất bại",
                        ChiTiet = $"Giao dịch {key} thất bại, {paymentTransaction.Invoices.Count} hóa đơn qua {methodLabel}.",
                        DoiTuongLoai = "GiaoDichThanhToan",
                        DoiTuongId = paymentTransaction.Id
                    });

                    _context.Notifications.Add(new Notification
                    {
                        NguoiDungId = patientUserId,
                        NoiDung = $"[ThanhToan] Thanh toán chưa hoàn tất|Giao dịch qua {methodLabel} chưa thành công - bạn chưa bị trừ tiền. Vui lòng thử lại.",
                        NgayGui = DateTime.Now,
                        DaDoc = false
                    });
                }
                else // processing
                {
                    paymentTransaction.TrangThai = "DangXuLy";
                    paymentTransaction.NgayCapNhat = DateTime.Now;
                    // Hóa đơn giữ nguyên DangXuLy (đã set từ lúc InitiatePayment).

                    _context.AuditLogs.Add(new AuditLog
                    {
                        NguoiDungId = patientUserId,
                        HanhDong = "Giao dịch thanh toán đang xử lý",
                        ChiTiet = $"Giao dịch {key} đang chờ xác nhận từ {methodLabel}, {paymentTransaction.Invoices.Count} hóa đơn, tổng {paymentTransaction.SoTien:N0}đ.",
                        DoiTuongLoai = "GiaoDichThanhToan",
                        DoiTuongId = paymentTransaction.Id
                    });

                    _context.Notifications.Add(new Notification
                    {
                        NguoiDungId = patientUserId,
                        NoiDung = $"[ThanhToan] Đang xử lý|Giao dịch qua {methodLabel} đang được xác nhận, vui lòng đợi ít phút.",
                        NgayGui = DateTime.Now,
                        DaDoc = false
                    });
                }

                await _context.SaveChangesAsync();
                await dbTransaction.CommitAsync();

                return RedirectToAction(nameof(PaymentReturn), new { key });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi xử lý webhook thanh toán cho giao dịch {Key}", key);
                TempData["ErrorMessage"] = "Có lỗi khi xác nhận giao dịch. Nếu tiền đã bị trừ, hệ thống sẽ tự cập nhật trong ít phút - vui lòng không thanh toán lại ngay.";
                return RedirectToAction(nameof(PaymentReturn), new { key });
            }
        }

        // GET: /Patient/Payment/PaymentReturn?key=... - CHỈ hiển thị, KHÔNG phải
        // nguồn chân lý (Webhook mới là nơi ghi trạng thái thật).
        [HttpGet]
        public async Task<IActionResult> PaymentReturn(string key)
        {
            var patientUserId = GetCurrentUserId();
            var paymentTransaction = await _context.PaymentTransactions
                .Include(t => t.Invoices)
                .FirstOrDefaultAsync(t => t.IdempotencyKey == key && t.NguoiKhoiTaoId == patientUserId);

            if (paymentTransaction == null) return NotFound();

            return View(paymentTransaction);
        }

        // POST: /Patient/Payment/SendEmailReceipt/5
        [HttpPost]
        [ValidateAntiForgeryToken]
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

            try
            {
                await _emailSender.SendAsync(patient.User.Email, $"Biên lai thanh toán HD-{invoice.Id:D5} - MediFlow HMS",
                    $"Biên lai hóa đơn HD-{invoice.Id:D5}, số tiền {invoice.TongTien:N0}đ, trạng thái: {invoice.TrangThaiThanhToan}. " +
                    "Vui lòng đăng nhập Cổng bệnh nhân để xem/tải biên lai đầy đủ.");

                _context.AuditLogs.Add(new AuditLog
                {
                    NguoiDungId = patientUserId,
                    HanhDong = "Gửi biên lai qua email",
                    ChiTiet = $"Biên lai hóa đơn HD-{invoice.Id:D5} đã được gửi tới email {patient.User.Email}.",
                    DoiTuongLoai = "HoaDon",
                    DoiTuongId = invoice.Id
                });
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = $"Biên lai đã được gửi thành công đến email: {patient.User.Email}!" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi gửi biên lai email cho hóa đơn {InvoiceId}", id);
                return Json(new { success = false, message = "Không thể gửi email biên lai lúc này. Vui lòng thử lại sau." });
            }
        }

        private static string MethodLabel(string? method) => method switch
        {
            "vnpay" => "Online (VNPay)",
            "momo" => "Online (MoMo)",
            "zalopay" => "Online (ZaloPay)",
            "chuyenkhoan" => "Chuyển khoản",
            "TienMat" => "Tiền mặt",
            "ChuyenKhoan" => "Chuyển khoản",
            "Online (VNPay)" => "Online (VNPay)",
            "Online (MoMo)" => "Online (MoMo)",
            "Online (ZaloPay)" => "Online (ZaloPay)",
            "Online" => "Thanh toán online",
            null => "Chưa chọn phương thức",
            _ => method
        };

        private static string GenerateFallbackTransactionCode(string method) => method switch
        {
            "vnpay" => $"VNP{DateTime.Now:yyMMddHHmmss}",
            "momo" => $"MOMO{DateTime.Now:yyMMddHHmmss}",
            "zalopay" => $"ZP{DateTime.Now:yyMMddHHmmss}",
            "chuyenkhoan" => $"CK{DateTime.Now:yyMMddHHmmss}",
            _ => $"TXN{DateTime.Now:yyMMddHHmmss}"
        };

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
