using System;
using System.IO;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Models;

namespace QuanLyBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class SettingsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly string _settingsFilePath;

        public SettingsController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
            _settingsFilePath = Path.Combine(_env.ContentRootPath, "hospital_settings.json");
        }

        // GET: Admin/Settings
        public IActionResult Index()
        {
            var settings = LoadSettings();
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
                SaveSettings(model);

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

        private HospitalSettings LoadSettings()
        {
            if (!System.IO.File.Exists(_settingsFilePath))
            {
                return new HospitalSettings();
            }

            try
            {
                string json = System.IO.File.ReadAllText(_settingsFilePath);
                return JsonSerializer.Deserialize<HospitalSettings>(json) ?? new HospitalSettings();
            }
            catch
            {
                return new HospitalSettings();
            }
        }

        private void SaveSettings(HospitalSettings settings)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(settings, options);
            System.IO.File.WriteAllText(_settingsFilePath, json);
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 1;
        }
    }

    public class HospitalSettings
    {
        public string TenBenhVien { get; set; } = "Bệnh viện Đa khoa Quốc tế MediFlow";
        public string Hotline { get; set; } = "1900 6868";
        public string HotlineCapCuu { get; set; } = "028.115";
        public string EmailHoTro { get; set; } = "support@mediflow.vn";
        public string DiaChi { get; set; } = "Số 120 Đường Ba Tháng Hai, Quận 10, TP. Hồ Chí Minh";
        public string GioLamViec { get; set; } = "Thứ Hai - Chủ Nhật: 07:30 - 21:00";
        public int ThoiGianKhamCa { get; set; } = 20; // Minutes per appointment
        public int SoBenhNhanToiDaMoiCa { get; set; } = 5; // Max patients per slot
        public int NguongCanhBaoTonKho { get; set; } = 50; // Threshold alert for low stock medicines
        public int ThueVat { get; set; } = 8; // VAT rate %
        public bool BatTatThongBao { get; set; } = true; // Switch to turn notifications on/off
    }
}
