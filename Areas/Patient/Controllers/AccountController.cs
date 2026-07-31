using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Helpers;
using QuanLyBenhVien.Models;

namespace QuanLyBenhVien.Areas.Patient.Controllers
{
    [Area("Patient")]
    [Authorize(Roles = "Patient")]
    public class AccountController : Controller
    {
        // The only two HanhDong values that currently ever get written against
        // a patient's DoiTuongId - see the honest-scope note rendered on the
        // page. Anything else in NhatKyHeThong is unrelated to record access.
        private static readonly string[] RecordAccessActions = { "Xem thông tin nhạy cảm", "BreakTheGlass_XemHoSoBenhNhan" };

        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Patient/Account
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var patient = await _context.Patients.Include(p => p.User).FirstOrDefaultAsync(p => p.NguoiDungId == userId);
            if (patient == null) return NotFound();

            ViewBag.Patient = patient;

            ViewBag.LoginHistory = await _context.AuditLogs
                .Where(a => a.NguoiDungId == userId && (a.HanhDong == "Đăng nhập" || a.HanhDong == "Đăng xuất"))
                .OrderByDescending(a => a.ThoiGian)
                .Take(20)
                .ToListAsync();

            ViewBag.AccessLog = await _context.AuditLogs
                .Include(a => a.User)
                .Where(a => a.DoiTuongLoai == "BenhNhan" && a.DoiTuongId == patient.Id && RecordAccessActions.Contains(a.HanhDong))
                .OrderByDescending(a => a.ThoiGian)
                .Take(50)
                .ToListAsync();

            return View();
        }

        // POST: /Patient/Account/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string matKhauHienTai, string matKhauMoi, string xacNhanMatKhauMoi)
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

            if (string.IsNullOrEmpty(matKhauHienTai) || !HashHelper.VerifyPassword(matKhauHienTai, user.MatKhauHash))
            {
                TempData["ErrorMessage"] = "Mật khẩu hiện tại không chính xác.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrEmpty(matKhauMoi) || matKhauMoi.Length < 6)
            {
                TempData["ErrorMessage"] = "Mật khẩu mới phải có ít nhất 6 ký tự.";
                return RedirectToAction(nameof(Index));
            }

            if (matKhauMoi != xacNhanMatKhauMoi)
            {
                TempData["ErrorMessage"] = "Xác nhận mật khẩu mới không khớp.";
                return RedirectToAction(nameof(Index));
            }

            user.MatKhauHash = HashHelper.HashPassword(matKhauMoi);
            _context.Entry(user).State = EntityState.Modified;

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = userId,
                HanhDong = "Đổi mật khẩu",
                ChiTiet = $"{user.HoTen} tự đổi mật khẩu tài khoản."
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã đổi mật khẩu thành công.";
            return RedirectToAction(nameof(Index));
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
