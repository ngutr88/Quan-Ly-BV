using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Models;
using QuanLyBenhVien.Services;

namespace QuanLyBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly HospitalSettingsProvider _settingsProvider;

        public SettingsController(ApplicationDbContext context, HospitalSettingsProvider settingsProvider)
        {
            _context = context;
            _settingsProvider = settingsProvider;
        }

        // GET: Admin/Settings
        public IActionResult Index()
        {
            var settings = _settingsProvider.Load();
            return View(settings);
        }

        // POST: Admin/Settings
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(HospitalSettings model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                _settingsProvider.Save(model);

                // Add audit log
                _context.AuditLogs.Add(new AuditLog
                {
                    NguoiDungId = GetCurrentUserId(),
                    HanhDong = "Cập nhật cấu hình",
                    ChiTiet = $"Cập nhật cấu hình bệnh viện động. Tên viện: {model.TenBenhVien}, Slot khám: {model.ThoiGianKhamCa} phút."
                });
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đã lưu thông tin cấu hình hệ thống bệnh viện thành công.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "Đã xảy ra lỗi khi lưu cấu hình: " + ex.Message);
                return View(model);
            }
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
