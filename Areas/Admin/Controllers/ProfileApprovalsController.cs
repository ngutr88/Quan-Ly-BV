using System;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Helpers;
using QuanLyBenhVien.Models;
using QuanLyBenhVien.Models.ViewModels;

namespace QuanLyBenhVien.Areas.Admin.Controllers
{
    // Module RBAC "Admin.ProfileApprovals" (xem ModulePermissionRegistry) chỉ
    // quyết định vào được TRANG này không, theo cả vai trò Admin. Việc bấm
    // được Duyệt/Từ chối còn cần thêm cờ User.DuocDuyetHoSoHanhNghe (theo
    // từng tài khoản) - cùng khuôn CurrentUserCanApproveAsync của
    // MedicinesController cho phiếu nhập kho.
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ProfileApprovalsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileApprovalsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/ProfileApprovals
        public async Task<IActionResult> Index()
        {
            var pending = await _context.ProfileChangeRequests
                .Include(r => r.Doctor).ThenInclude(d => d.User)
                .Where(r => r.TrangThai == "ChoDuyet")
                .OrderBy(r => r.NgayDeXuat)
                .ToListAsync();

            var recent = await _context.ProfileChangeRequests
                .Include(r => r.Doctor).ThenInclude(d => d.User)
                .Include(r => r.NguoiDuyet)
                .Where(r => r.TrangThai != "ChoDuyet")
                .OrderByDescending(r => r.NgayDuyet)
                .Take(20)
                .ToListAsync();

            ViewBag.Recent = recent;
            ViewBag.CanApprove = await CurrentUserCanApproveAsync();
            return View(pending);
        }

        // GET: Admin/ProfileApprovals/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var request = await _context.ProfileChangeRequests
                .Include(r => r.Doctor).ThenInclude(d => d.User)
                .Include(r => r.NguoiDuyet)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (request == null) return NotFound();

            var oldData = JsonSerializer.Deserialize<ProfileChangeFields>(request.DuLieuCuJson)!;
            var newData = JsonSerializer.Deserialize<ProfileChangeFields>(request.DuLieuMoiJson)!;
            var departmentNames = await _context.Departments.ToDictionaryAsync(d => d.Id, d => d.TenKhoa);

            ViewBag.Request = request;
            ViewBag.Diff = ProfileChangeDiffHelper.Compute(oldData, newData, departmentNames);
            ViewBag.CanApprove = await CurrentUserCanApproveAsync();
            return View();
        }

        // POST: Admin/ProfileApprovals/Approve/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            if (!await CurrentUserCanApproveAsync())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền duyệt yêu cầu thay đổi hồ sơ hành nghề.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var request = await _context.ProfileChangeRequests
                .Include(r => r.Doctor).ThenInclude(d => d.User)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (request == null) return NotFound();

            if (request.TrangThai != "ChoDuyet")
            {
                TempData["ErrorMessage"] = "Chỉ có thể duyệt yêu cầu đang ở trạng thái Chờ duyệt.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var newData = JsonSerializer.Deserialize<ProfileChangeFields>(request.DuLieuMoiJson)!;
            var doctor = request.Doctor;

            doctor.User.HoTen = newData.HoTen;
            doctor.NgaySinh = newData.NgaySinh;
            doctor.HocVi = newData.HocVi;
            doctor.ChuyenKhoa = newData.ChuyenKhoa;
            doctor.KhoaId = newData.KhoaId;
            doctor.ChucVu = newData.ChucVu;
            doctor.SoCCHN = newData.SoCCHN;
            doctor.NgayCapCCHN = newData.NgayCapCCHN;
            doctor.NoiCapCCHN = newData.NoiCapCCHN;
            doctor.PhamViHanhNghe = newData.PhamViHanhNghe;

            request.TrangThai = "DaDuyet";
            request.NguoiDuyetId = GetCurrentUserId();
            request.NgayDuyet = DateTime.Now;

            _context.Notifications.Add(new Notification
            {
                NguoiDungId = doctor.NguoiDungId,
                NoiDung = "[HoSo] Yêu cầu thay đổi hồ sơ đã được duyệt|Yêu cầu thay đổi hồ sơ hành nghề của bạn đã được duyệt và áp dụng."
            });

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Duyệt yêu cầu thay đổi hồ sơ hành nghề",
                ChiTiet = $"Duyệt yêu cầu #{request.Id} của {doctor.User.HoTen}, đã áp dụng thay đổi.",
                DoiTuongLoai = "YeuCauThayDoiHoSo",
                DoiTuongId = request.Id
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã duyệt và áp dụng thay đổi hồ sơ hành nghề.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/ProfileApprovals/Reject/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string lyDo)
        {
            if (!await CurrentUserCanApproveAsync())
            {
                TempData["ErrorMessage"] = "Bạn không có quyền duyệt/từ chối yêu cầu thay đổi hồ sơ hành nghề.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (string.IsNullOrWhiteSpace(lyDo))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập lý do từ chối.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var request = await _context.ProfileChangeRequests
                .Include(r => r.Doctor).ThenInclude(d => d.User)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (request == null) return NotFound();

            if (request.TrangThai != "ChoDuyet")
            {
                TempData["ErrorMessage"] = "Chỉ có thể từ chối yêu cầu đang ở trạng thái Chờ duyệt.";
                return RedirectToAction(nameof(Details), new { id });
            }

            request.TrangThai = "TuChoi";
            request.LyDoTuChoi = lyDo.Trim();
            request.NguoiDuyetId = GetCurrentUserId();
            request.NgayDuyet = DateTime.Now;

            _context.Notifications.Add(new Notification
            {
                NguoiDungId = request.Doctor.NguoiDungId,
                NoiDung = $"[HoSo] Yêu cầu thay đổi hồ sơ bị từ chối|Lý do: {request.LyDoTuChoi}"
            });

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Từ chối yêu cầu thay đổi hồ sơ hành nghề",
                ChiTiet = $"Từ chối yêu cầu #{request.Id} của {request.Doctor.User.HoTen}. Lý do: {request.LyDoTuChoi}",
                DoiTuongLoai = "YeuCauThayDoiHoSo",
                DoiTuongId = request.Id
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã từ chối yêu cầu.";
            return RedirectToAction(nameof(Index));
        }

        private async Task<bool> CurrentUserCanApproveAsync()
        {
            var currentUser = await _context.Users.FindAsync(GetCurrentUserId());
            return currentUser?.DuocDuyetHoSoHanhNghe == true;
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
