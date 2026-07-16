using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Models;
using QuanLyBenhVien.Helpers;
using System.Security.Claims;

namespace QuanLyBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DoctorsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DoctorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Doctors
        public async Task<IActionResult> Index(string searchString, int? departmentId)
        {
            var query = _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Department)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(d => d.User.HoTen.Contains(searchString) || d.ChuyenKhoa.Contains(searchString));
            }

            if (departmentId.HasValue)
            {
                query = query.Where(d => d.KhoaId == departmentId.Value);
            }

            var doctors = await query.ToListAsync();

            ViewBag.Departments = new SelectList(await _context.Departments.ToListAsync(), "Id", "TenKhoa", departmentId);
            ViewBag.SearchString = searchString;

            return View(doctors);
        }

        // GET: Admin/Doctors/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var doctor = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Department)
                .Include(d => d.WorkSchedules)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (doctor == null) return NotFound();

            // Fetch doctor's appointment list
            ViewBag.Appointments = await _context.Appointments
                .Include(a => a.Patient.User)
                .Where(a => a.BacSiId == doctor.Id)
                .OrderByDescending(a => a.ThoiGian)
                .Take(10)
                .ToListAsync();

            // Fetch reviews
            var reviews = await _context.Reviews
                .Include(r => r.Patient.User)
                .Where(r => r.BacSiId == doctor.Id)
                .ToListAsync();
            
            ViewBag.Reviews = reviews;
            ViewBag.AverageRating = reviews.Any() ? reviews.Average(r => r.SoSao) : 5.0;

            return View(doctor);
        }

        // GET: Admin/Doctors/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.KhoaId = new SelectList(await _context.Departments.ToListAsync(), "Id", "TenKhoa");
            return View();
        }

        // POST: Admin/Doctors/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string hoTen, string email, string sdt, string matKhau, int khoaId, string chuyenKhoa, string hocVi, int soNamKinhNghiem, string lichLamViec, string chucVu)
        {
            if (await _context.Users.AnyAsync(u => u.Email == email || u.Sdt == sdt))
            {
                ModelState.AddModelError("", "Email hoặc số điện thoại đã tồn tại trong hệ thống.");
                ViewBag.KhoaId = new SelectList(await _context.Departments.ToListAsync(), "Id", "TenKhoa", khoaId);
                return View();
            }

            // Create User Account
            var user = new User
            {
                HoTen = hoTen,
                Email = email,
                Sdt = sdt,
                MatKhauHash = HashHelper.HashPassword(matKhau),
                VaiTro = "Doctor",
                TrangThai = "Active"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Create Doctor Profile
            var doctor = new QuanLyBenhVien.Models.Doctor
            {
                NguoiDungId = user.Id,
                KhoaId = khoaId,
                ChuyenKhoa = chuyenKhoa,
                HocVi = hocVi,
                SoNamKinhNghiem = soNamKinhNghiem,
                LichLamViec = lichLamViec,
                ChucVu = chucVu ?? "Bác sĩ"
            };
            _context.Doctors.Add(doctor);
            await _context.SaveChangesAsync();
            _context.DoctorWorkSchedules.AddRange(
                DoctorScheduleHelper.BuildSchedulesFromDescription(doctor.Id, doctor.LichLamViec));
            
            // Log action
            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Thêm bác sĩ mới",
                ChiTiet = $"Thêm tài khoản bác sĩ: {hoTen} ({email}), Khoa ID: {khoaId}."
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã thêm thành công bác sĩ {hoTen}.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Doctors/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var doctor = await _context.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == id);
            if (doctor == null) return NotFound();

            ViewBag.KhoaId = new SelectList(await _context.Departments.ToListAsync(), "Id", "TenKhoa", doctor.KhoaId);
            return View(doctor);
        }

        // POST: Admin/Doctors/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string hoTen, string sdt, int khoaId, string chuyenKhoa, string hocVi, int soNamKinhNghiem, string lichLamViec, string trangThai, string chucVu)
        {
            var doctor = await _context.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == id);
            if (doctor == null) return NotFound();

            // Check if phone unique among other users
            if (await _context.Users.AnyAsync(u => u.Sdt == sdt && u.Id != doctor.NguoiDungId))
            {
                ModelState.AddModelError("", "Số điện thoại đã được tài khoản khác sử dụng.");
                ViewBag.KhoaId = new SelectList(await _context.Departments.ToListAsync(), "Id", "TenKhoa", khoaId);
                return View(doctor);
            }

            // Update user properties
            doctor.User.HoTen = hoTen;
            doctor.User.Sdt = sdt;
            doctor.User.TrangThai = trangThai; // "Active" or "Blocked" (Soft Delete/Deactivation)

            // Update doctor properties
            doctor.KhoaId = khoaId;
            doctor.ChuyenKhoa = chuyenKhoa;
            doctor.HocVi = hocVi;
            doctor.SoNamKinhNghiem = soNamKinhNghiem;
            doctor.LichLamViec = lichLamViec;
            doctor.ChucVu = chucVu ?? "Bác sĩ";

            _context.Entry(doctor.User).State = EntityState.Modified;
            _context.Entry(doctor).State = EntityState.Modified;

            var oldSchedules = await _context.DoctorWorkSchedules
                .Where(s => s.BacSiId == doctor.Id)
                .ToListAsync();
            _context.DoctorWorkSchedules.RemoveRange(oldSchedules);
            _context.DoctorWorkSchedules.AddRange(
                DoctorScheduleHelper.BuildSchedulesFromDescription(doctor.Id, doctor.LichLamViec));

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Cập nhật bác sĩ",
                ChiTiet = $"Cập nhật hồ sơ bác sĩ: {hoTen} (Mã số Profile ID: {id}), Trạng thái: {trangThai}."
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật hồ sơ bác sĩ {hoTen} thành công.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Doctors/ToggleStatus/5
        [HttpPost]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var doctor = await _context.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.Id == id);
            if (doctor == null) return NotFound();

            // Toggle active / blocked status (soft delete)
            doctor.User.TrangThai = doctor.User.TrangThai == "Active" ? "Blocked" : "Active";
            _context.Entry(doctor.User).State = EntityState.Modified;

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Đổi trạng thái Bác sĩ",
                ChiTiet = $"Thay đổi trạng thái bác sĩ {doctor.User.HoTen} thành {doctor.User.TrangThai}."
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Trạng thái hoạt động của BS. {doctor.User.HoTen} đã được đổi thành {doctor.User.TrangThai}.";
            return RedirectToAction(nameof(Index));
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
