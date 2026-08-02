using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Models;
using QuanLyBenhVien.Services;

namespace QuanLyBenhVien.Areas.Doctor.Controllers
{
    // Sprint 1 của "Lịch làm việc": đăng ký nghỉ phép + số dư phép năm + lịch
    // tháng chỉ hiển thị nghỉ phép đã duyệt (Ca trực/Lịch phẫu thuật chưa có
    // nguồn dữ liệu nên chưa đổ vào lịch - xem Sprint 2). Nếu bác sĩ hiện tại
    // là Trưởng khoa (Doctor.ChucVu), trang còn có thêm khối "Duyệt yêu cầu
    // của khoa" cho các yêu cầu Chờ duyệt của đồng nghiệp cùng KhoaId.
    [Area("Doctor")]
    [Authorize(Roles = "Doctor")]
    public class ScheduleController : Controller
    {
        private const string TruongKhoa = "Trưởng khoa";

        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly LeaveRequestService _leaveService;

        public ScheduleController(ApplicationDbContext context, IWebHostEnvironment environment, LeaveRequestService leaveService)
        {
            _context = context;
            _environment = environment;
            _leaveService = leaveService;
        }

        public async Task<IActionResult> Index(int? year, int? month)
        {
            var userId = GetCurrentUserId();
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.NguoiDungId == userId);
            if (doctor == null) return NotFound();

            var today = DateTime.Today;
            var viewYear = year ?? today.Year;
            var viewMonth = month ?? today.Month;
            if (viewMonth < 1 || viewMonth > 12)
            {
                viewYear = today.Year;
                viewMonth = today.Month;
            }
            var monthStart = new DateTime(viewYear, viewMonth, 1);
            var monthEnd = monthStart.AddMonths(1).AddDays(-1);

            var balance = await _leaveService.GetOrCreateBalanceAsync(doctor.Id, today.Year);
            var remaining = Helpers.LeaveBalanceCalculator.ComputeRemaining(
                balance.TongSoNgay, balance.CongDonTuNamTruoc, balance.DaDung, balance.DaTamGiu);

            var approvedThisMonth = await _context.LeaveRequests
                .Where(r => r.BacSiId == doctor.Id && r.TrangThai == "DaDuyet" &&
                            r.TuNgay <= monthEnd && r.DenNgay >= monthStart)
                .ToListAsync();

            var myRequests = await _context.LeaveRequests
                .Where(r => r.BacSiId == doctor.Id)
                .OrderByDescending(r => r.NgayTao)
                .Take(30)
                .ToListAsync();

            var pendingCount = myRequests.Count(r => r.TrangThai == "ChoDuyet");

            ViewBag.Doctor = doctor;
            ViewBag.Balance = balance;
            ViewBag.Remaining = remaining;
            ViewBag.ViewYear = viewYear;
            ViewBag.ViewMonth = viewMonth;
            ViewBag.MonthStart = monthStart;
            ViewBag.ApprovedThisMonth = approvedThisMonth;
            ViewBag.MyRequests = myRequests;
            ViewBag.PendingCount = pendingCount;

            if (doctor.ChucVu == TruongKhoa)
            {
                var deptPending = await _context.LeaveRequests
                    .Include(r => r.Doctor).ThenInclude(d => d.User)
                    .Where(r => r.TrangThai == "ChoDuyet" && r.Doctor.KhoaId == doctor.KhoaId && r.BacSiId != doctor.Id)
                    .OrderBy(r => r.TuNgay)
                    .ToListAsync();

                var conflictByRequestId = new Dictionary<int, bool>();
                var remainingByRequestId = new Dictionary<int, decimal>();
                foreach (var req in deptPending)
                {
                    conflictByRequestId[req.Id] = await _leaveService.HasScheduleConflictAsync(req.BacSiId, req.TuNgay, req.DenNgay);
                    var reqBalance = await _leaveService.GetOrCreateBalanceAsync(req.BacSiId, req.TuNgay.Year);
                    remainingByRequestId[req.Id] = Helpers.LeaveBalanceCalculator.ComputeRemaining(
                        reqBalance.TongSoNgay, reqBalance.CongDonTuNamTruoc, reqBalance.DaDung, reqBalance.DaTamGiu);
                }

                ViewBag.DeptPending = deptPending;
                ViewBag.ConflictByRequestId = conflictByRequestId;
                ViewBag.RemainingByRequestId = remainingByRequestId;
            }

            return View();
        }

        // POST: /Doctor/Schedule/RequestLeave
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RequestLeave(DateTime tuNgay, DateTime denNgay, string? buoi, string loaiNghi, string lyDo, IFormFile? dinhKem)
        {
            var userId = GetCurrentUserId();
            var doctor = await _context.Doctors.FirstOrDefaultAsync(d => d.NguoiDungId == userId);
            if (doctor == null) return NotFound();

            string? dinhKemUrl = null;
            if (dinhKem != null && dinhKem.Length > 0)
            {
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
                var extension = Path.GetExtension(dinhKem.FileName).ToLowerInvariant();
                if (!allowed.Contains(extension) || dinhKem.Length > 5 * 1024 * 1024)
                {
                    TempData["ErrorMessage"] = "Đính kèm chỉ hỗ trợ JPG/PNG/PDF và dung lượng tối đa 5MB.";
                    return RedirectToAction(nameof(Index));
                }

                var storageRoot = Path.Combine(_environment.WebRootPath, "uploads", "leave-attachments");
                Directory.CreateDirectory(storageRoot);
                var storedName = $"{Guid.NewGuid():N}{extension}";
                var storedPath = Path.Combine(storageRoot, storedName);
                await using (var stream = System.IO.File.Create(storedPath))
                {
                    await dinhKem.CopyToAsync(stream);
                }
                dinhKemUrl = $"/uploads/leave-attachments/{storedName}";
            }

            var result = await _leaveService.SubmitAsync(doctor.Id, tuNgay, denNgay, buoi, loaiNghi, lyDo, dinhKemUrl);
            if (!result.Success)
            {
                TempData["ErrorMessage"] = result.ErrorMessage;
                return RedirectToAction(nameof(Index));
            }

            TempData["SuccessMessage"] = "Đã gửi yêu cầu nghỉ phép, chờ duyệt." +
                (result.Warning != null ? " " + result.Warning : "");
            return RedirectToAction(nameof(Index));
        }

        // POST: /Doctor/Schedule/ApproveAsHead/5 - chỉ dành cho bác sĩ Trưởng khoa,
        // duyệt yêu cầu của đồng nghiệp CÙNG khoa (không được tự duyệt cho chính mình).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveAsHead(int id)
        {
            var check = await CheckHeadAuthorityAsync(id);
            if (check.Error != null)
            {
                TempData["ErrorMessage"] = check.Error;
                return RedirectToAction(nameof(Index));
            }

            var result = await _leaveService.ApproveAsync(id, GetCurrentUserId());
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? "Đã duyệt yêu cầu nghỉ phép."
                : result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        // POST: /Doctor/Schedule/RejectAsHead/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectAsHead(int id, string lyDo)
        {
            var check = await CheckHeadAuthorityAsync(id);
            if (check.Error != null)
            {
                TempData["ErrorMessage"] = check.Error;
                return RedirectToAction(nameof(Index));
            }

            var result = await _leaveService.RejectAsync(id, GetCurrentUserId(), lyDo);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? "Đã từ chối yêu cầu nghỉ phép."
                : result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        private async Task<(string? Error, LeaveRequest? Request)> CheckHeadAuthorityAsync(int requestId)
        {
            var userId = GetCurrentUserId();
            var approver = await _context.Doctors.FirstOrDefaultAsync(d => d.NguoiDungId == userId);
            if (approver == null || approver.ChucVu != TruongKhoa)
            {
                return ("Bạn không có quyền duyệt yêu cầu nghỉ phép.", null);
            }

            var request = await _context.LeaveRequests.Include(r => r.Doctor).FirstOrDefaultAsync(r => r.Id == requestId);
            if (request == null)
            {
                return ("Không tìm thấy yêu cầu.", null);
            }
            if (request.Doctor.KhoaId != approver.KhoaId)
            {
                return ("Bạn chỉ được duyệt yêu cầu của bác sĩ cùng khoa.", null);
            }
            if (request.BacSiId == approver.Id)
            {
                return ("Không thể tự duyệt yêu cầu nghỉ phép của chính mình - vui lòng nhờ Admin xử lý.", null);
            }

            return (null, request);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
