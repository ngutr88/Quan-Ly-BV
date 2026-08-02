using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Services;

namespace QuanLyBenhVien.Areas.Admin.Controllers
{
    // Module RBAC "Admin.LeaveApprovals" (xem ModulePermissionRegistry) chỉ
    // quyết định vào được TRANG này không. Khác với ProfileApprovalsController,
    // theo đúng quyết định đã chốt với người dùng, việc bấm được Duyệt/Từ chối
    // KHÔNG cần thêm cờ User.DuocDuyet... riêng - bất kỳ tài khoản Admin nào
    // cũng duyệt được ngay (song song với bác sĩ Trưởng khoa cùng khoa, xem
    // Areas/Doctor/Controllers/ScheduleController.ApproveAsHead/RejectAsHead).
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class LeaveApprovalsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly LeaveRequestService _leaveService;

        public LeaveApprovalsController(ApplicationDbContext context, LeaveRequestService leaveService)
        {
            _context = context;
            _leaveService = leaveService;
        }

        // GET: Admin/LeaveApprovals
        public async Task<IActionResult> Index()
        {
            var pending = await _context.LeaveRequests
                .Include(r => r.Doctor).ThenInclude(d => d.User)
                .Include(r => r.Doctor).ThenInclude(d => d.Department)
                .Where(r => r.TrangThai == "ChoDuyet")
                .OrderBy(r => r.TuNgay)
                .ToListAsync();

            var recent = await _context.LeaveRequests
                .Include(r => r.Doctor).ThenInclude(d => d.User)
                .Include(r => r.NguoiDuyet)
                .Where(r => r.TrangThai != "ChoDuyet")
                .OrderByDescending(r => r.NgayDuyet)
                .Take(20)
                .ToListAsync();

            ViewBag.Recent = recent;
            return View(pending);
        }

        // GET: Admin/LeaveApprovals/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var request = await _context.LeaveRequests
                .Include(r => r.Doctor).ThenInclude(d => d.User)
                .Include(r => r.Doctor).ThenInclude(d => d.Department)
                .Include(r => r.NguoiDuyet)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (request == null) return NotFound();

            ViewBag.HasConflict = await _leaveService.HasScheduleConflictAsync(request.BacSiId, request.TuNgay, request.DenNgay);
            var balance = await _leaveService.GetOrCreateBalanceAsync(request.BacSiId, request.TuNgay.Year);
            ViewBag.Remaining = Helpers.LeaveBalanceCalculator.ComputeRemaining(
                balance.TongSoNgay, balance.CongDonTuNamTruoc, balance.DaDung, balance.DaTamGiu);

            return View(request);
        }

        // POST: Admin/LeaveApprovals/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var result = await _leaveService.ApproveAsync(id, GetCurrentUserId());
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? "Đã duyệt yêu cầu nghỉ phép."
                : result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/LeaveApprovals/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string lyDo)
        {
            var result = await _leaveService.RejectAsync(id, GetCurrentUserId(), lyDo);
            TempData[result.Success ? "SuccessMessage" : "ErrorMessage"] = result.Success
                ? "Đã từ chối yêu cầu nghỉ phép."
                : result.ErrorMessage;
            return RedirectToAction(nameof(Index));
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
