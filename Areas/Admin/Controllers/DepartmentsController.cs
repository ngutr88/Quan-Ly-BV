using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Models;
using System.Security.Claims;

namespace QuanLyBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DepartmentsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DepartmentsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Departments
        public async Task<IActionResult> Index(int? id)
        {
            var depts = await _context.Departments.ToListAsync();
            if (!depts.Any())
            {
                return View();
            }

            int selectedId = id ?? depts.First().Id;
            var selectedDept = depts.FirstOrDefault(d => d.Id == selectedId) ?? depts.First();

            var services = await _context.Services
                .Where(s => s.KhoaId == selectedDept.Id)
                .ToListAsync();

            // Count doctors in each department
            var doctorCounts = await _context.Doctors
                .GroupBy(d => d.KhoaId)
                .Select(g => new { KhoaId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.KhoaId, x => x.Count);

            // Get chief of department (mock first doctor with highest degree or first doctor)
            var chiefDoctor = await _context.Doctors
                .Include(d => d.User)
                .Where(d => d.KhoaId == selectedDept.Id)
                .FirstOrDefaultAsync();

            string truongKhoa = chiefDoctor != null ? $"{chiefDoctor.HocVi}. {chiefDoctor.User.HoTen}" : "Chưa bổ nhiệm";
            int activeDocsCount = doctorCounts.ContainsKey(selectedDept.Id) ? doctorCounts[selectedDept.Id] : 0;

            // Stats
            ViewBag.TotalStaff = await _context.Doctors.CountAsync();
            ViewBag.Departments = depts;
            ViewBag.SelectedDept = selectedDept;
            ViewBag.Services = services;
            ViewBag.DoctorCounts = doctorCounts;
            ViewBag.TruongKhoa = truongKhoa;
            ViewBag.ActiveDocsCount = activeDocsCount;

            return View();
        }

        // POST: Admin/Departments/CreateDepartment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateDepartment(string tenKhoa, string moTa, string viTri)
        {
            if (string.IsNullOrEmpty(tenKhoa) || string.IsNullOrEmpty(viTri))
            {
                TempData["ErrorMessage"] = "Tên khoa và vị trí là bắt buộc.";
                return RedirectToAction(nameof(Index));
            }

            var dept = new Department
            {
                TenKhoa = tenKhoa,
                MoTa = moTa ?? string.Empty,
                ViTri = viTri
            };

            _context.Departments.Add(dept);

            // Log
            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Thêm khoa",
                ChiTiet = $"Thêm khoa mới: {tenKhoa} tại vị trí {viTri}."
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã thêm khoa {tenKhoa} thành công.";
            return RedirectToAction(nameof(Index), new { id = dept.Id });
        }

        // POST: Admin/Departments/EditDepartment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditDepartment(int id, string tenKhoa, string moTa, string viTri)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return NotFound();

            if (string.IsNullOrEmpty(tenKhoa) || string.IsNullOrEmpty(viTri))
            {
                TempData["ErrorMessage"] = "Tên khoa và vị trí là bắt buộc.";
                return RedirectToAction(nameof(Index), new { id = id });
            }

            dept.TenKhoa = tenKhoa;
            dept.MoTa = moTa ?? string.Empty;
            dept.ViTri = viTri;

            _context.Entry(dept).State = EntityState.Modified;

            // Log
            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Cập nhật khoa",
                ChiTiet = $"Cập nhật thông tin khoa {tenKhoa}."
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật khoa {tenKhoa} thành công.";
            return RedirectToAction(nameof(Index), new { id = id });
        }

        // POST: Admin/Departments/DeleteDepartment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDepartment(int id)
        {
            var dept = await _context.Departments.FindAsync(id);
            if (dept == null) return NotFound();

            // Check if any doctors belong to this department
            var hasDoctors = await _context.Doctors.AnyAsync(d => d.KhoaId == id);
            if (hasDoctors)
            {
                TempData["ErrorMessage"] = "Không thể xóa khoa này vì đang có bác sĩ công tác.";
                return RedirectToAction(nameof(Index), new { id = id });
            }

            _context.Departments.Remove(dept);

            // Log
            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Xóa khoa",
                ChiTiet = $"Xóa khoa: {dept.TenKhoa}."
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xóa khoa {dept.TenKhoa} thành công.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Departments/CreateService
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateService(int khoaId, string tenDichVu, decimal gia)
        {
            var dept = await _context.Departments.FindAsync(khoaId);
            if (dept == null) return NotFound();

            if (string.IsNullOrEmpty(tenDichVu) || gia < 0)
            {
                TempData["ErrorMessage"] = "Tên dịch vụ hợp lệ và giá là bắt buộc.";
                return RedirectToAction(nameof(Index), new { id = khoaId });
            }

            var service = new Service
            {
                KhoaId = khoaId,
                TenDichVu = tenDichVu,
                Gia = gia
            };

            _context.Services.Add(service);

            // Log
            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Thêm dịch vụ",
                ChiTiet = $"Thêm dịch vụ '{tenDichVu}' cho khoa {dept.TenKhoa}. Giá: {gia:N0}đ."
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã thêm dịch vụ '{tenDichVu}' thành công.";
            return RedirectToAction(nameof(Index), new { id = khoaId });
        }

        // POST: Admin/Departments/EditService
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditService(int id, string tenDichVu, decimal gia)
        {
            var service = await _context.Services.Include(s => s.Department).FirstOrDefaultAsync(s => s.Id == id);
            if (service == null) return NotFound();

            if (string.IsNullOrEmpty(tenDichVu) || gia < 0)
            {
                TempData["ErrorMessage"] = "Tên dịch vụ hợp lệ và giá là bắt buộc.";
                return RedirectToAction(nameof(Index), new { id = service.KhoaId });
            }

            service.TenDichVu = tenDichVu;
            service.Gia = gia;

            _context.Entry(service).State = EntityState.Modified;

            // Log
            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Cập nhật dịch vụ",
                ChiTiet = $"Cập nhật dịch vụ '{tenDichVu}' của khoa {service.Department.TenKhoa}. Giá mới: {gia:N0}đ."
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật dịch vụ '{tenDichVu}' thành công.";
            return RedirectToAction(nameof(Index), new { id = service.KhoaId });
        }

        // POST: Admin/Departments/DeleteService
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteService(int id)
        {
            var service = await _context.Services.FindAsync(id);
            if (service == null) return NotFound();

            int khoaId = service.KhoaId;
            _context.Services.Remove(service);

            // Log
            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Xóa dịch vụ",
                ChiTiet = $"Xóa dịch vụ '{service.TenDichVu}'."
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã xóa dịch vụ thành công.";
            return RedirectToAction(nameof(Index), new { id = khoaId });
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 1;
        }
    }
}
