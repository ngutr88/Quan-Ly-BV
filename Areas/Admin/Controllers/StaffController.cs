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
        private readonly QuanLyBenhVien.Services.IEmailSender _emailSender;

        public StaffController(ApplicationDbContext context, QuanLyBenhVien.Services.ExcelExportService excel, QuanLyBenhVien.Services.IEmailSender emailSender)
        {
            _context = context;
            _excel = excel;
            _emailSender = emailSender;
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

                        // Add work schedules - từ chối nếu mô tả free-text sinh ra các ca
                        // chồng giờ nhau trong cùng một ngày (mục 7 của yêu cầu nghiệp vụ).
                        var newSchedules = DoctorScheduleHelper.BuildSchedulesFromDescription(doctor.Id, doctor.LichLamViec);
                        if (DoctorScheduleHelper.HasOverlap(newSchedules))
                        {
                            ModelState.AddModelError("", "Mô tả lịch làm việc tạo ra các ca bị chồng giờ nhau. Vui lòng chỉnh lại.");
                            ViewBag.KhoaId = new SelectList(await _context.Departments.ToListAsync(), "Id", "TenKhoa", khoaId);
                            return View();
                        }

                        _context.DoctorWorkSchedules.AddRange(newSchedules);
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
        public async Task<IActionResult> Edit(int id, string hoTen, string sdt, string vaiTro, string trangThai, int? khoaId, string chuyenKhoa, string hocVi, int? soNamKinhNghiem, string lichLamViec, string chucVu, bool duocDuyetPhieuNhapKho, bool duocXuLyCLS)
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
                    user.DuocDuyetPhieuNhapKho = duocDuyetPhieuNhapKho;
                    user.DuocXuLyCLS = duocXuLyCLS;
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

                            var newSchedules = DoctorScheduleHelper.BuildSchedulesFromDescription(doctor.Id, doctor.LichLamViec);
                            if (DoctorScheduleHelper.HasOverlap(newSchedules))
                            {
                                ModelState.AddModelError("", "Mô tả lịch làm việc tạo ra các ca bị chồng giờ nhau. Vui lòng chỉnh lại.");
                                ViewBag.KhoaId = new SelectList(await _context.Departments.ToListAsync(), "Id", "TenKhoa", khoaId);
                                return View(user);
                            }

                            _context.DoctorWorkSchedules.AddRange(newSchedules);
                        }
                        else
                        {
                            // Update existing doctor profile. Nếu hồ sơ này trước đó đã bị
                            // xóa mềm (đổi vai trò Doctor -> Admin rồi quay lại Doctor), việc
                            // đổi vai trò về Doctor đồng nghĩa với kích hoạt lại hồ sơ.
                            var doctor = user.DoctorProfile;
                            doctor.KhoaId = khoaId.Value;
                            doctor.ChuyenKhoa = chuyenKhoa ?? "Đa khoa";
                            doctor.HocVi = hocVi ?? "BS";
                            doctor.SoNamKinhNghiem = soNamKinhNghiem ?? 1;
                            doctor.ChucVu = chucVu ?? "Bác sĩ";
                            doctor.DaXoa = false;
                            doctor.NgayXoa = null;
                            doctor.XoaBoiId = null;

                            var scheduleChanged = doctor.LichLamViec != lichLamViec;
                            doctor.LichLamViec = string.IsNullOrEmpty(lichLamViec) ? "Ca sang (08:00 - 12:00) & Chiều (13:30 - 17:30)" : lichLamViec;
                            _context.Entry(doctor).State = EntityState.Modified;

                            if (scheduleChanged)
                            {
                                var newSchedules = DoctorScheduleHelper.BuildSchedulesFromDescription(doctor.Id, doctor.LichLamViec);
                                if (DoctorScheduleHelper.HasOverlap(newSchedules))
                                {
                                    ModelState.AddModelError("", "Mô tả lịch làm việc tạo ra các ca bị chồng giờ nhau. Vui lòng chỉnh lại.");
                                    ViewBag.KhoaId = new SelectList(await _context.Departments.ToListAsync(), "Id", "TenKhoa", khoaId);
                                    return View(user);
                                }

                                var oldSchedules = await _context.DoctorWorkSchedules
                                    .Where(s => s.BacSiId == doctor.Id)
                                    .ToListAsync();
                                _context.DoctorWorkSchedules.RemoveRange(oldSchedules);
                                _context.DoctorWorkSchedules.AddRange(newSchedules);
                            }
                        }
                    }
                    else if (oldRole == "Doctor" && vaiTro == "Admin")
                    {
                        // Xóa mềm hồ sơ bác sĩ khi đổi vai trò sang Admin - KHÔNG xóa cứng,
                        // để giữ lại lịch sử LichKham/DanhGia đã gắn với BacSiId này (mục 5).
                        // Lịch làm việc thì gỡ bỏ vì bác sĩ không còn nhận lịch khám mới.
                        if (user.DoctorProfile != null)
                        {
                            var oldSchedules = await _context.DoctorWorkSchedules
                                .Where(s => s.BacSiId == user.DoctorProfile.Id)
                                .ToListAsync();
                            _context.DoctorWorkSchedules.RemoveRange(oldSchedules);

                            user.DoctorProfile.DaXoa = true;
                            user.DoctorProfile.NgayXoa = DateTime.Now;
                            user.DoctorProfile.XoaBoiId = GetCurrentUserId();
                            _context.Entry(user.DoctorProfile).State = EntityState.Modified;
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

        // POST: Admin/Staff/SendResetLink/5
        //
        // Nhân sự bị chặn tự phục vụ ở /Auth/ForgotPassword (xem AuthController) -
        // đây là 1 trong 2 công cụ duy nhất Admin có để giúp họ lấy lại mật
        // khẩu. Tái dùng đúng bảng YeuCauKhoiPhucMatKhau của luồng bệnh nhân,
        // bỏ qua toàn bộ cột OTP (đi thẳng vào ResetTokenHash) vì việc Admin
        // xác nhận đúng người đã đóng vai trò bước xác thực thứ 2.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendResetLink(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            var rawToken = GenerateResetToken();
            _context.PasswordResetRequests.Add(new PasswordResetRequest
            {
                NguoiDungId = user.Id,
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1",
                KhoiTaoBoi = "Admin",
                Kenh = null,
                ResetTokenHash = HashResetToken(rawToken),
                ThoiHanResetToken = DateTime.Now.AddMinutes(10)
            });

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Gửi link đặt lại mật khẩu cho nhân sự",
                ChiTiet = $"Gửi link đặt lại mật khẩu cho nhân sự {user.HoTen} (ID: {user.Id})."
            });
            await _context.SaveChangesAsync();

            var resetUrl = Url.Action("SetNewPassword", "Auth", new { area = "", token = rawToken }, Request.Scheme);
            await _emailSender.SendAsync(user.Email, "Đặt lại mật khẩu MediFlow HMS",
                $"Quản trị viên đã yêu cầu đặt lại mật khẩu cho tài khoản của bạn. Bấm vào liên kết sau trong 10 phút để đặt mật khẩu mới: {resetUrl}");

            TempData["SuccessMessage"] = $"Đã gửi link đặt lại mật khẩu tới email của nhân sự {user.HoTen}.";
            return RedirectToAction(nameof(Details), new { id = id });
        }

        // POST: Admin/Staff/IssueTempPassword/5
        //
        // Sinh mật khẩu ngẫu nhiên, KHÔNG cho Admin gõ/chọn mật khẩu cố định
        // cho người khác - mật khẩu tạm chỉ hiển thị MỘT LẦN cho Admin ngay sau
        // khi sinh (giống tiền lệ hiển thị OTP demo một lần) để truyền lại cho
        // nhân sự qua điện thoại/trực tiếp, có hạn 24h và bắt đổi ở lần đăng
        // nhập kế tiếp.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IssueTempPassword(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            var tempPassword = GenerateTempPassword();
            user.MatKhauHash = HashHelper.HashPassword(tempPassword);
            user.PhaiDoiMatKhau = true;
            user.MatKhauTamHetHan = DateTime.Now.AddHours(24);
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            _context.Entry(user).State = EntityState.Modified;

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = GetCurrentUserId(),
                HanhDong = "Cấp mật khẩu tạm cho nhân sự",
                ChiTiet = $"Cấp mật khẩu tạm (hạn 24 giờ, bắt buộc đổi ở lần đăng nhập kế tiếp) cho nhân sự {user.HoTen} (ID: {user.Id})."
            });

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã cấp mật khẩu tạm cho nhân sự {user.HoTen}: {tempPassword} (hiệu lực 24 giờ, bắt buộc đổi ở lần đăng nhập kế tiếp). Vui lòng truyền lại mật khẩu này cho nhân sự - hệ thống sẽ không hiển thị lại.";
            return RedirectToAction(nameof(Details), new { id = id });
        }

        private static string GenerateResetToken()
        {
            var raw = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            return raw.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private static string HashResetToken(string rawToken)
        {
            using var sha = System.Security.Cryptography.SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(rawToken)));
        }

        private static string GenerateTempPassword()
        {
            // 12 ký tự CSPRNG, đảm bảo có đủ hoa/thường/số để tự thỏa chính sách
            // mật khẩu ngay từ đầu (không phụ thuộc PhaiDoiMatKhau redirect kịp lúc).
            const string upper = "ABCDEFGHJKLMNPQRSTUVWXYZ";
            const string lower = "abcdefghijkmnpqrstuvwxyz";
            const string digits = "23456789";
            const string all = upper + lower + digits;

            var chars = new char[12];
            chars[0] = upper[System.Security.Cryptography.RandomNumberGenerator.GetInt32(upper.Length)];
            chars[1] = lower[System.Security.Cryptography.RandomNumberGenerator.GetInt32(lower.Length)];
            chars[2] = digits[System.Security.Cryptography.RandomNumberGenerator.GetInt32(digits.Length)];
            for (var i = 3; i < chars.Length; i++)
            {
                chars[i] = all[System.Security.Cryptography.RandomNumberGenerator.GetInt32(all.Length)];
            }

            // Xáo trộn để 3 ký tự đầu không luôn cố định hoa/thường/số.
            for (var i = chars.Length - 1; i > 0; i--)
            {
                var j = System.Security.Cryptography.RandomNumberGenerator.GetInt32(i + 1);
                (chars[i], chars[j]) = (chars[j], chars[i]);
            }

            return new string(chars);
        }

        // GET: Admin/Staff/PermissionMatrix
        public async Task<IActionResult> PermissionMatrix()
        {
            var saved = await _context.RolePermissions.AsNoTracking().ToListAsync();
            var savedLookup = saved.ToDictionary(p => p.VaiTro + "|" + p.ModuleKey, p => p.DuocPhep);
            var roles = QuanLyBenhVien.Helpers.ModulePermissionRegistry.AllGroups()
                .Select(g => (g.Role, g.RoleLabel)).ToList();

            var rows = new List<StaffPermissionMatrixRow>();
            foreach (var (owningRole, _, modules) in QuanLyBenhVien.Helpers.ModulePermissionRegistry.AllGroups())
            {
                foreach (var module in modules)
                {
                    rows.Add(new StaffPermissionMatrixRow
                    {
                        GroupRole = owningRole,
                        OwningRole = owningRole,
                        ModuleKey = module.Key,
                        Label = module.Label,
                        Allowed = !savedLookup.TryGetValue(owningRole + "|" + module.Key, out var allowed) || allowed,
                        Locked = QuanLyBenhVien.Helpers.ModulePermissionRegistry.AlwaysAllowed.Contains(module.Key)
                    });
                }
            }

            var model = new StaffPermissionMatrixViewModel
            {
                Roles = roles,
                Rows = rows
            };

            return View(model);
        }

        // POST: Admin/Staff/SavePermissionMatrix
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SavePermissionMatrix([FromForm] List<string> allowedKeys)
        {
            allowedKeys ??= new List<string>();
            var allowedSet = allowedKeys.ToHashSet();

            var existing = await _context.RolePermissions.ToListAsync();
            var existingLookup = existing.ToDictionary(p => p.VaiTro + "|" + p.ModuleKey, p => p);

            var changedModules = new List<string>();

            foreach (var (role, _, modules) in QuanLyBenhVien.Helpers.ModulePermissionRegistry.AllGroups())
            {
                foreach (var module in modules)
                {
                    // These modules always stay allowed regardless of what was posted,
                    // so an admin can never lock every admin out of Staff/Dashboard.
                    var isLocked = QuanLyBenhVien.Helpers.ModulePermissionRegistry.AlwaysAllowed.Contains(module.Key);
                    var combo = role + "|" + module.Key;
                    var wantAllowed = isLocked || allowedSet.Contains(combo);

                    if (existingLookup.TryGetValue(combo, out var row))
                    {
                        if (row.DuocPhep != wantAllowed)
                        {
                            row.DuocPhep = wantAllowed;
                            row.CapNhatLuc = DateTime.Now;
                            row.CapNhatBoiId = GetCurrentUserId();
                            _context.Entry(row).State = EntityState.Modified;
                            changedModules.Add($"{module.Label} ({role}) -> {(wantAllowed ? "Bật" : "Tắt")}");
                        }
                    }
                    else if (!wantAllowed)
                    {
                        // Only insert a row when explicitly turning a module off;
                        // "allowed" is already the default for any missing row.
                        _context.RolePermissions.Add(new RolePermission
                        {
                            VaiTro = role,
                            ModuleKey = module.Key,
                            DuocPhep = false,
                            CapNhatLuc = DateTime.Now,
                            CapNhatBoiId = GetCurrentUserId()
                        });
                        changedModules.Add($"{module.Label} ({role}) -> Tắt");
                    }
                }
            }

            if (changedModules.Count > 0)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    NguoiDungId = GetCurrentUserId(),
                    HanhDong = "Cập nhật ma trận phân quyền",
                    ChiTiet = $"Thay đổi quyền truy cập module: {string.Join("; ", changedModules)}."
                });
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã lưu ma trận phân quyền thành công.";
            }
            else
            {
                TempData["SuccessMessage"] = "Không có thay đổi nào để lưu.";
            }

            return RedirectToAction(nameof(PermissionMatrix));
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }

    public class StaffPermissionMatrixViewModel
    {
        public List<(string Role, string RoleLabel)> Roles { get; set; } = new();
        public List<StaffPermissionMatrixRow> Rows { get; set; } = new();
    }

    public class StaffPermissionMatrixRow
    {
        // Which role column this module's row "lives under" — every module belongs
        // to exactly one role's area, so only that role's cell in the grid gets a
        // real checkbox; the other role columns render a fixed "not applicable"
        // cell, since the area-level [Authorize] gate makes cross-role grants
        // impossible regardless of what this matrix says.
        public string GroupRole { get; set; } = string.Empty;
        public string OwningRole { get; set; } = string.Empty;
        public string ModuleKey { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        public bool Allowed { get; set; }
        public bool Locked { get; set; }
    }
}
