using System;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Helpers;
using QuanLyBenhVien.Models;

namespace QuanLyBenhVien.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize(Roles = "Doctor")]
    public class LabResultsController : Controller
    {
        private const string BreakTheGlassAction = "BreakTheGlass_XemKetQuaCLS";
        private const int BreakTheGlassGrantMinutes = 2;
        private const int PendingResultThresholdHours = 24;

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;

        public LabResultsController(ApplicationDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _environment = environment;
        }

        // GET: /Doctor/LabResults?tab=ChuaXem
        [HttpGet]
        public async Task<IActionResult> Index(string tab = "ChuaXem", int page = 1, int? pageSize = null)
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (!doctorId.HasValue) return Forbid();

            var scoped = _context.LabOrderItems
                .Include(i => i.DichVu)
                .Include(i => i.PhieuChiDinh).ThenInclude(o => o.Patient).ThenInclude(p => p.User)
                .Include(i => i.KetQua)
                .Where(i => i.PhieuChiDinh.BacSiChiDinhId == doctorId.Value);

            var query = tab switch
            {
                "CoBatThuong" => scoped.Where(i => i.TrangThai == "DaCoKetQua" && i.KetQua!.CoBatThuong),
                "ChoKetQua" => scoped.Where(i => i.TrangThai == "ChoThucHien" || i.TrangThai == "DangThucHien"),
                "TatCa" => scoped,
                _ => scoped.Where(i => i.TrangThai == "DaCoKetQua" && !i.KetQua!.DaXem)
            };

            var ordered = tab == "ChoKetQua"
                ? query.OrderBy(i => i.PhieuChiDinh.NgayChiDinh)
                : query.OrderByDescending(i => i.KetQua != null ? i.KetQua.NgayTra : i.PhieuChiDinh.NgayChiDinh);

            // pageSize không truyền qua query string -> dùng tuỳ chọn "Số dòng
            // mỗi trang" lưu theo tài khoản (Doctor/Profile/SaveUiPreferences),
            // null (chưa từng đặt) mới rơi về mặc định hệ thống.
            var effectivePageSize = pageSize ?? await _context.Users
                .Where(u => u.Id == GetCurrentUserId())
                .Select(u => u.SoDongMoiTrangMacDinh)
                .FirstOrDefaultAsync() ?? PagedList<LabOrderItem>.DefaultPageSize;
            var paged = await ordered.ToPagedListAsync(page, effectivePageSize);

            ViewBag.Tab = tab;
            ViewBag.PendingThresholdHours = PendingResultThresholdHours;
            ViewBag.CountChuaXem = await scoped.CountAsync(i => i.TrangThai == "DaCoKetQua" && !i.KetQua!.DaXem);
            ViewBag.CountCoBatThuong = await scoped.CountAsync(i => i.TrangThai == "DaCoKetQua" && i.KetQua!.CoBatThuong);
            ViewBag.CountChoKetQua = await scoped.CountAsync(i => i.TrangThai == "ChoThucHien" || i.TrangThai == "DangThucHien");
            ViewBag.CountTatCa = await scoped.CountAsync();

            return View(paged);
        }

        // GET: /Doctor/LabResults/Details/5  (id = ChiTietPhieuChiDinhCLS.Id)
        [HttpGet]
        public async Task<IActionResult> Details(int id, bool breakGlass = false)
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (!doctorId.HasValue) return Forbid();

            var item = await _context.LabOrderItems
                .Include(i => i.DichVu)
                .Include(i => i.PhieuChiDinh).ThenInclude(o => o.Patient).ThenInclude(p => p.User)
                .Include(i => i.PhieuChiDinh).ThenInclude(o => o.BacSiChiDinh).ThenInclude(d => d.User)
                .Include(i => i.KetQua).ThenInclude(r => r!.Files)
                .FirstOrDefaultAsync(i => i.Id == id);

            const string notFoundOrOutOfScopeMessage = "Không tìm thấy kết quả hoặc kết quả không thuộc phạm vi phụ trách của bạn.";

            if (item == null || item.TrangThai != "DaCoKetQua" || item.KetQua == null)
            {
                TempData["ErrorMessage"] = notFoundOrOutOfScopeMessage;
                return RedirectToAction(nameof(Index));
            }

            var isAssignedDoctor = item.PhieuChiDinh.BacSiChiDinhId == doctorId.Value;
            if (!isAssignedDoctor)
            {
                var currentUserId = GetCurrentUserId();
                var hasGrant = breakGlass && await _context.AuditLogs.AnyAsync(a =>
                    a.NguoiDungId == currentUserId &&
                    a.HanhDong == BreakTheGlassAction &&
                    a.DoiTuongLoai == "BenhNhan" &&
                    a.DoiTuongId == item.PhieuChiDinh.BenhNhanId &&
                    a.ThoiGian >= DateTime.Now.AddMinutes(-BreakTheGlassGrantMinutes));

                if (!hasGrant)
                {
                    TempData["ErrorMessage"] = notFoundOrOutOfScopeMessage;
                    return RedirectToAction(nameof(Index));
                }
            }
            else if (!item.KetQua.DaXem)
            {
                // Chỉ đánh dấu đã xem khi đúng là bác sĩ chỉ định mở - xem qua
                // break-the-glass KHÔNG được xóa cờ chưa đọc của người thật sự
                // phụ trách.
                item.KetQua.DaXem = true;
                item.KetQua.NgayXem = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            var patientId = item.PhieuChiDinh.BenhNhanId;
            ViewBag.ActivePrescriptionDrugs = await _context.PrescriptionDetails
                .Where(pd => (pd.Prescription.TrangThai == "ChoCapPhat" || pd.Prescription.TrangThai == "DaCapPhat")
                    && pd.Prescription.ExaminationRecord.Appointment.BenhNhanId == patientId)
                .Select(pd => pd.Medicine.TenThuoc)
                .Distinct()
                .ToListAsync();

            ViewBag.DoctorNote = await _context.ExaminationRecords
                .Where(e => e.Id == item.PhieuChiDinh.PhieuKhamId)
                .Select(e => e.GhiChuCLSCuaBacSi)
                .FirstOrDefaultAsync();

            ViewBag.IsAssignedDoctor = isAssignedDoctor;
            return View(item);
        }

        // POST: /Doctor/LabResults/AddDoctorNote/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddDoctorNote(int id, string ghiChu)
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (!doctorId.HasValue) return Forbid();

            var item = await _context.LabOrderItems
                .Include(i => i.PhieuChiDinh)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (item == null) return NotFound();

            if (item.PhieuChiDinh.BacSiChiDinhId != doctorId.Value)
            {
                TempData["ErrorMessage"] = "Chỉ bác sĩ chỉ định mới có thể ghi chú kết quả này.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var exam = await _context.ExaminationRecords.FirstOrDefaultAsync(e => e.Id == item.PhieuChiDinh.PhieuKhamId);
            if (exam != null)
            {
                exam.GhiChuCLSCuaBacSi = string.IsNullOrWhiteSpace(ghiChu) ? null : ghiChu.Trim();
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã lưu ghi chú.";
            }

            return RedirectToAction(nameof(Details), new { id });
        }

        // POST: /Doctor/LabResults/MarkAsRead/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (!doctorId.HasValue) return Forbid();

            var item = await _context.LabOrderItems
                .Include(i => i.PhieuChiDinh)
                .Include(i => i.KetQua)
                .FirstOrDefaultAsync(i => i.Id == id);
            if (item == null || item.PhieuChiDinh.BacSiChiDinhId != doctorId.Value || item.KetQua == null)
                return NotFound();

            if (!item.KetQua.DaXem)
            {
                item.KetQua.DaXem = true;
                item.KetQua.NgayXem = DateTime.Now;
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // POST: /Doctor/LabResults/BreakTheGlass
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BreakTheGlass(int patientId, string reason)
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (!doctorId.HasValue) return Forbid();

            if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5)
                return Json(new { success = false, message = "Vui lòng nhập lý do truy cập đủ rõ ràng (tối thiểu 5 ký tự)." });

            var patientExists = await _context.Patients.AnyAsync(p => p.Id == patientId);
            if (!patientExists) return NotFound();

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = BreakTheGlassAction,
                ChiTiet = reason.Trim(),
                DoiTuongLoai = "BenhNhan",
                DoiTuongId = patientId
            });
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // GET: /Doctor/LabResults/DownloadResultFile/5
        [HttpGet]
        public async Task<IActionResult> DownloadResultFile(int fileId, bool breakGlass = false)
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (!doctorId.HasValue) return Forbid();

            var file = await _context.LabResultFiles
                .Include(f => f.KetQua).ThenInclude(r => r.ChiTietPhieuChiDinh).ThenInclude(i => i.PhieuChiDinh)
                .FirstOrDefaultAsync(f => f.Id == fileId);
            if (file == null) return NotFound();

            var order = file.KetQua.ChiTietPhieuChiDinh.PhieuChiDinh;
            if (order.BacSiChiDinhId != doctorId.Value)
            {
                var currentUserId = GetCurrentUserId();
                var hasGrant = breakGlass && await _context.AuditLogs.AnyAsync(a =>
                    a.NguoiDungId == currentUserId &&
                    a.HanhDong == BreakTheGlassAction &&
                    a.DoiTuongLoai == "BenhNhan" &&
                    a.DoiTuongId == order.BenhNhanId &&
                    a.ThoiGian >= DateTime.Now.AddMinutes(-BreakTheGlassGrantMinutes));
                if (!hasGrant) return Forbid();
            }

            var path = Path.Combine(_environment.ContentRootPath, "App_Data", "lab-results", file.TenLuuTru);
            if (!System.IO.File.Exists(path)) return NotFound();
            return PhysicalFile(path, file.ContentType, file.TenGoc);
        }

        // GET: /Doctor/LabResults/PrintOrder/5  (labOrderId = PhieuChiDinhCLS.Id)
        [HttpGet]
        public async Task<IActionResult> PrintOrder(int labOrderId)
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (!doctorId.HasValue) return Forbid();

            var order = await _context.LabOrders
                .Include(o => o.Patient).ThenInclude(p => p.User)
                .Include(o => o.BacSiChiDinh).ThenInclude(d => d.User)
                .Include(o => o.BoChiDinh)
                .Include(o => o.ChiTiet).ThenInclude(i => i.DichVu)
                .FirstOrDefaultAsync(o => o.Id == labOrderId && o.BacSiChiDinhId == doctorId.Value);
            if (order == null) return NotFound();

            return View("~/Views/Shared/_LabOrderPrintDocument.cshtml", order);
        }

        // GET: /Doctor/LabResults/UnreadCount
        [HttpGet]
        public async Task<IActionResult> UnreadCount()
        {
            var doctorId = await GetCurrentDoctorIdAsync();
            if (!doctorId.HasValue) return Json(new { count = 0 });

            var count = await LabResultsCountHelper.GetUnreadCountAsync(_context, doctorId.Value);
            return Json(new { count });
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }

        private async Task<int?> GetCurrentDoctorIdAsync()
        {
            var userId = GetCurrentUserId();
            if (userId <= 0) return null;
            return await _context.Doctors
                .Where(d => d.NguoiDungId == userId)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync();
        }
    }
}
