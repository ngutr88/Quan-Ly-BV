using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Models;
using QuanLyBenhVien.Helpers;

namespace QuanLyBenhVien.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class StaffController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly QuanLyBenhVien.Services.ExcelExportService _excel;

        public StaffController(ApplicationDbContext context, QuanLyBenhVien.Services.ExcelExportService excel)
        {
            _context = context;
            _excel = excel;
        }

        // GET: Admin/Staff
        public async Task<IActionResult> Index(string searchString, string roleFilter, int page = 1, int? pageSize = null)
        {
            var query = BuildQuery(searchString, roleFilter);

            var paged = await query.ToPagedListAsync(page, PagedList<User>.NormalisePageSize(pageSize));

            ViewBag.SearchString = searchString;
            ViewBag.RoleFilter = roleFilter;

            ViewBag.TotalMatching = paged.TotalCount;
            ViewBag.AdminCount = await query.CountAsync(u => u.VaiTro == "Admin");
            ViewBag.DoctorCount = await query.CountAsync(u => u.VaiTro == "Doctor");
            ViewBag.ActiveStaff = await query.CountAsync(u => u.TrangThai == "Active");

            return View(paged);
        }

        // GET: Admin/Staff/Export
        public async Task<IActionResult> Export(string searchString, string roleFilter)
        {
            var staff = await BuildQuery(searchString, roleFilter).ToListAsync();

            var columns = new List<QuanLyBenhVien.Services.ExcelColumn<User>>
            {
                new("Mã NV", u => $"STAFF-{u.Id:D3}"),
                new("Họ và tên", u => u.HoTen),
                new("Vai trò", u => u.VaiTro == "Admin" ? "Quản trị viên" : "Bác sĩ"),
                new("Chức vụ", u => u.DoctorProfile?.ChucVu ?? ""),
                new("Khoa", u => u.DoctorProfile?.Department?.TenKhoa ?? ""),
                new("Email", u => u.Email),
                new("Số điện thoại", u => u.Sdt),
                new("Trạng thái", u => u.TrangThai == "Active" ? "Hoạt động" : "Đã khóa"),
                new("Ngày tham gia", u => u.NgayTao.ToString("dd/MM/yyyy"))
            };

            var content = _excel.Build(
                "Nhan su",
                "DANH SÁCH NHÂN SỰ",
                columns,
                staff,
                string.IsNullOrEmpty(roleFilter)
                    ? "Toàn bộ nhân sự"
                    : $"Vai trò: {(roleFilter == "Admin" ? "Quản trị viên" : "Bác sĩ")}");

            return File(content,
                QuanLyBenhVien.Services.ExcelExportService.ContentType,
                QuanLyBenhVien.Services.ExcelExportService.FileName("danh-sach-nhan-su"));
        }

        private IQueryable<User> BuildQuery(string searchString, string roleFilter)
        {
            var query = _context.Users
                .Where(u => u.VaiTro == "Admin" || u.VaiTro == "Doctor")
                .Include(u => u.DoctorProfile)
                .ThenInclude(d => d.Department)
                .AsQueryable();

            if (!string.IsNullOrEmpty(searchString))
            {
                query = query.Where(u => u.HoTen.Contains(searchString) || u.Email.Contains(searchString) || u.Sdt.Contains(searchString));
            }

            if (!string.IsNullOrEmpty(roleFilter))
            {
                query = query.Where(u => u.VaiTro == roleFilter);
            }

            return query.OrderByDescending(u => u.NgayTao);
        }

        // GET: Admin/Staff/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var staff = await _context.Users
                .Include(u => u.DoctorProfile)
                .ThenInclude(d => d.Department)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (staff == null) return NotFound();

            return View(staff);
        }

        // GET: Admin/Staff/Create
        public async Task<IActionResult> Create()
        {
            ViewBag.KhoaId = new SelectList(await _context.Departments.ToListAsync(), "Id", "TenKhoa");
            return View();
        }

        // POST: Admin/Staff/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string hoTen, string email, string sdt, string matKhau, string vaiTro, int? khoaId, string chuyenKhoa, string hocVi, int? soNamKinhNghiem, string lichLamViec, string chucVu)
        {
            if (await _context.Users.AnyAsync(u => u.Email == email || u.Sdt == sdt))
            {
                ModelState.AddModelError("", "Email hoặc số điện thoại đã tồn tại trong hệ thống.");
                ViewBag.KhoaId = new SelectList(await _context.Departments.ToListAsync(), "Id", "TenKhoa", khoaId);
                return View();
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Create User
                    var user = new User
                    {
                        HoTen = hoTen,
                        Email = email,
                        Sdt = sdt,
                        MatKhauHash = HashHelper.HashPassword(matKhau),
                        VaiTro = vaiTro, // "Admin" or "Doctor"
                        TrangThai = "Active",
                        NgayTao = DateTime.Now
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // 2. Create Doctor Profile if role is Doctor
                    if (vaiTro == "Doctor")
                    {
                        if (!khoaId.HasValue)
                        {
                            ModelState.AddModelError("", "Vui lòng chọn khoa cho bác sĩ.");
                            ViewBag.KhoaId = new SelectList(await _context.Departments.ToListAsync(), "Id", "TenKhoa", khoaId);
                            return View();
                        }

                        var doctor = new QuanLyBenhVien.Models.Doctor
                        {
                            NguoiDungId = user.Id,
                            KhoaId = khoaId.Value,
                            ChuyenKhoa = chuyenKhoa ?? "Đa khoa",
                            HocVi = hocVi ?? "BS",
                            SoNamKinhNghiem = soNamKinhNghiem ?? 1,
                            LichLamViec = string.IsNullOrEmpty(lichLamViec) ? "Ca sang (08:00 - 12:00) & Chiều (13:30 - 17:30)" : lichLamViec,
                            ChucVu = chucVu ?? "Bác sĩ"
                        };

                        _context.Doctors.Add(doctor);
                        await _context.SaveChangesAsync();

                        // Add work schedules
                        _context.DoctorWorkSchedules.AddRange(
                            DoctorScheduleHelper.BuildSchedulesFromDescription(doctor.Id, doctor.LichLamViec));
                        await _context.SaveChangesAsync();
                    }

                    // 3. Log System Action
                    _context.AuditLogs.Add(new AuditLog
                    {
                        NguoiDungId = GetCurrentUserId(),
                        HanhDong = "Thêm nhân sự mới",
                        ChiTiet = $"Tạo nhân viên: {hoTen} ({email}) với vai trò {vaiTro}."
                    });
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Đã thêm thành công nhân sự {hoTen}.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi tạo nhân sự: " + ex.Message);
                    ViewBag.KhoaId = new SelectList(await _context.Departments.ToListAsync(), "Id", "TenKhoa", khoaId);
                    return View();
                }
            }
        }

        // GET: Admin/Staff/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null) return NotFound();

            var staff = await _context.Users
                .Include(u => u.DoctorProfile)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (staff == null) return NotFound();

            ViewBag.KhoaId = new SelectList(await _context.Departments.ToListAsync(), "Id", "TenKhoa", staff.DoctorProfile?.KhoaId);
            return View(staff);
        }

        // POST: Admin/Staff/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string hoTen, string sdt, string vaiTro, string trangThai, int? khoaId, string chuyenKhoa, string hocVi, int? soNamKinhNghiem, string lichLamViec, string chucVu)
        {
            var user = await _context.Users.Include(u => u.DoctorProfile).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            if (await _context.Users.AnyAsync(u => u.Sdt == sdt && u.Id != id))
            {
                ModelState.AddModelError("", "Số điện thoại đã được sử dụng bởi tài khoản khác.");
                ViewBag.KhoaId = new SelectList(await _context.Departments.ToListAsync(), "Id", "TenKhoa", khoaId);
                return View(user);
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    // 1. Update basic user details
                    user.HoTen = hoTen;
                    user.Sdt = sdt;
                    user.TrangThai = trangThai;
                    
                    var oldRole = user.VaiTro;
                    user.VaiTro = vaiTro;
                    _context.Entry(user).State = EntityState.Modified;

                    // 2. Manage doctor profiles when changing roles
                    if (vaiTro == "Doctor")
                    {
                        if (!khoaId.HasValue)
                        {
                            ModelState.AddModelError("", "Bác sĩ phải thuộc một khoa chuyên môn.");
                            ViewBag.KhoaId = new SelectList(await _context.Departments.ToListAsync(), "Id", "TenKhoa", khoaId);
                            return View(user);
                        }

                        if (user.DoctorProfile == null)
                        {
                            // Create new doctor profile
                            var doctor = new QuanLyBenhVien.Models.Doctor
                            {
                                NguoiDungId = user.Id,
                                KhoaId = khoaId.Value,
                                ChuyenKhoa = chuyenKhoa ?? "Đa khoa",
                                HocVi = hocVi ?? "BS",
                                SoNamKinhNghiem = soNamKinhNghiem ?? 1,
                                LichLamViec = string.IsNullOrEmpty(lichLamViec) ? "Ca sang (08:00 - 12:00) & Chiều (13:30 - 17:30)" : lichLamViec,
                                ChucVu = chucVu ?? "Bác sĩ"
                            };
                            _context.Doctors.Add(doctor);
                            await _context.SaveChangesAsync();

                            _context.DoctorWorkSchedules.AddRange(
                                DoctorScheduleHelper.BuildSchedulesFromDescription(doctor.Id, doctor.LichLamViec));
                        }
                        else
                        {
                            // Update existing doctor profile
                            var doctor = user.DoctorProfile;
                            doctor.KhoaId = khoaId.Value;
                            doctor.ChuyenKhoa = chuyenKhoa ?? "Đa khoa";
                            doctor.HocVi = hocVi ?? "BS";
                            doctor.SoNamKinhNghiem = soNamKinhNghiem ?? 1;
                            doctor.ChucVu = chucVu ?? "Bác sĩ";

                            var scheduleChanged = doctor.LichLamViec != lichLamViec;
                            doctor.LichLamViec = string.IsNullOrEmpty(lichLamViec) ? "Ca sang (08:00 - 12:00) & Chiều (13:30 - 17:30)" : lichLamViec;
                            _context.Entry(doctor).State = EntityState.Modified;

                            if (scheduleChanged)
                            {
                                var oldSchedules = await _context.DoctorWorkSchedules
                                    .Where(s => s.BacSiId == doctor.Id)
                                    .ToListAsync();
                                _context.DoctorWorkSchedules.RemoveRange(oldSchedules);
                                _context.DoctorWorkSchedules.AddRange(
                                    DoctorScheduleHelper.BuildSchedulesFromDescription(doctor.Id, doctor.LichLamViec));
                            }
                        }
                    }
                    else if (oldRole == "Doctor" && vaiTro == "Admin")
                    {
                        // Remove Doctor profile if role changed from Doctor to Admin
                        if (user.DoctorProfile != null)
                        {
                            var oldSchedules = await _context.DoctorWorkSchedules
                                .Where(s => s.BacSiId == user.DoctorProfile.Id)
                                .ToListAsync();
                            _context.DoctorWorkSchedules.RemoveRange(oldSchedules);
                            _context.Doctors.Remove(user.DoctorProfile);
                        }
                    }

                    // 3. Add Audit Log
                    _context.AuditLogs.Add(new AuditLog
                    {
                        NguoiDungId = GetCurrentUserId(),
                        HanhDong = "Cập nhật nhân sự",
                        ChiTiet = $"Cập nhật thông tin nhân viên {hoTen} (ID: {id}), Vai trò: {vaiTro}, Trạng thái: {trangThai}."
                    });

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    TempData["SuccessMessage"] = $"Đã cập nhật thông tin nhân viên {hoTen} thành công.";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    ModelState.AddModelError("", "Đã xảy ra lỗi khi cập nhật: " + ex.Message);
                    ViewBag.KhoaId = new SelectList(await _context.Departments.ToListAsync(), "Id", "TenKhoa", khoaId);
                    return View(user);
                }
            }
        }

        // POST: Admin/Staff/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            if (user.Id == GetCurrentUserId())
            {
                TempData["ErrorMessage"] = "Bạn không thể tự khóa tài khoản của chính mình.";
                return RedirectToAction(nameof(Index));
            }

            user.TrangThai = user.TrangThai == "Active" ? "Blocked" : "Active";
            _context.Entry(user).State = EntityState.Modified;

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Đổi trạng thái nhân sự",
                ChiTiet = $"Thay đổi trạng thái tài khoản {user.HoTen} (ID: {user.Id}) thành {user.TrangThai}."
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cập nhật trạng thái tài khoản {user.HoTen} thành {user.TrangThai}.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Staff/ResetPassword/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(int id, string newPassword)
        {
            if (string.IsNullOrEmpty(newPassword))
            {
                TempData["ErrorMessage"] = "Mật khẩu mới không được để trống.";
                return RedirectToAction(nameof(Details), new { id = id });
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            user.MatKhauHash = HashHelper.HashPassword(newPassword);
            _context.Entry(user).State = EntityState.Modified;

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Đặt lại mật khẩu nhân sự",
                ChiTiet = $"Đặt lại mật khẩu mới cho nhân sự {user.HoTen} (ID: {user.Id})."
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã đặt lại mật khẩu thành công cho nhân sự {user.HoTen}.";
            return RedirectToAction(nameof(Details), new { id = id });
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
