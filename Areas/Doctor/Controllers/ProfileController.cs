using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Helpers;
using QuanLyBenhVien.Models;
using QuanLyBenhVien.Models.ViewModels;
using QuanLyBenhVien.Services;

namespace QuanLyBenhVien.Areas.Doctor.Controllers
{
    [Area("Doctor")]
    [Authorize(Roles = "Doctor")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IWebHostEnvironment _environment;
        private readonly HospitalSettingsProvider _settingsProvider;

        public ProfileController(ApplicationDbContext context, IEmailSender emailSender, IWebHostEnvironment environment, HospitalSettingsProvider settingsProvider)
        {
            _context = context;
            _emailSender = emailSender;
            _environment = environment;
            _settingsProvider = settingsProvider;
        }

        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            var doctor = await _context.Doctors
                .Include(d => d.User)
                .Include(d => d.Department)
                .FirstOrDefaultAsync(d => d.NguoiDungId == userId);
            if (doctor == null) return NotFound();

            var pending = await _context.ProfileChangeRequests
                .Where(r => r.BacSiId == doctor.Id && r.TrangThai == "ChoDuyet")
                .OrderByDescending(r => r.NgayDeXuat)
                .FirstOrDefaultAsync();

            var since = DateTime.Now.AddDays(-30);
            var vm = new DoctorProfileViewModel
            {
                Doctor = doctor,
                PendingRequest = pending,
                PendingFields = pending == null ? null : JsonSerializer.Deserialize<ProfileChangeFields>(pending.DuLieuMoiJson),
                Departments = await _context.Departments.OrderBy(d => d.TenKhoa).ToListAsync(),
                TwoFactorEnabled = doctor.User.TotpBatDau,
                TwoFactorForced = _settingsProvider.Load().BatBuoc2FABacSi,
                CurrentSessionToken = User.FindFirst("sid")?.Value,
                ActiveSessions = await _context.LoginSessions
                    .Where(s => s.NguoiDungId == userId && s.TrangThai == "HoatDong")
                    .OrderByDescending(s => s.ThoiGianHoatDongCuoi)
                    .ToListAsync(),
                LoginHistory = await _context.AuditLogs
                    .Where(a => a.NguoiDungId == userId && a.ThoiGian >= since &&
                                (a.HanhDong == "Đăng nhập" || a.HanhDong == "Đăng nhập thất bại"))
                    .OrderByDescending(a => a.ThoiGian)
                    .Take(50)
                    .ToListAsync()
            };

            return View(vm);
        }

        // POST: /Doctor/Profile/UploadAvatar - tự lưu ngay, cùng khuôn
        // Admin/Patients/UploadAvatar (Areas/Admin/Controllers/PatientsController.cs)
        // để nhất quán giới hạn định dạng/dung lượng và cách lưu tên file.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn ảnh để tải lên.";
                return RedirectToAction(nameof(Index));
            }

            var allowed = new[] { ".jpg", ".jpeg", ".png" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(extension) || file.Length > 5 * 1024 * 1024)
            {
                TempData["ErrorMessage"] = "Chỉ hỗ trợ JPG/PNG và dung lượng tối đa 5MB. Nên dùng ảnh vuông (chân dung) để hiển thị đẹp trên cổng bệnh nhân.";
                return RedirectToAction(nameof(Index));
            }

            var storageRoot = Path.Combine(_environment.WebRootPath, "uploads", "avatars");
            Directory.CreateDirectory(storageRoot);
            var storedName = $"{Guid.NewGuid():N}{extension}";
            var storedPath = Path.Combine(storageRoot, storedName);
            await using (var stream = System.IO.File.Create(storedPath))
            {
                await file.CopyToAsync(stream);
            }

            user.AnhDaiDien = storedName;
            _context.Entry(user).State = EntityState.Modified;

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = userId,
                HanhDong = "Cập nhật ảnh đại diện",
                ChiTiet = $"{user.HoTen} tự cập nhật ảnh đại diện từ trang Hồ sơ & Cài đặt."
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã cập nhật ảnh đại diện.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Doctor/Profile/SaveContactInfo - nhóm trường tự sửa, lưu ngay
        // (rủi ro pháp lý thấp, không cần Admin duyệt).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveContactInfo(string sdt, string email, string gioiThieuNgan, string quaTrinhDaoTao)
        {
            var userId = GetCurrentUserId();
            var doctor = await _context.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.NguoiDungId == userId);
            if (doctor == null) return NotFound();

            sdt = (sdt ?? string.Empty).Trim();
            email = (email ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(sdt) || string.IsNullOrWhiteSpace(email))
            {
                TempData["ErrorMessage"] = "Số điện thoại và email không được để trống.";
                return RedirectToAction(nameof(Index));
            }

            if (await _context.Users.AnyAsync(u => u.Id != userId && (u.Sdt == sdt || u.Email == email)))
            {
                TempData["ErrorMessage"] = "Số điện thoại hoặc email đã được dùng bởi tài khoản khác.";
                return RedirectToAction(nameof(Index));
            }

            doctor.User.Sdt = sdt;
            doctor.User.Email = email;
            doctor.GioiThieuNgan = Truncate(gioiThieuNgan, 500);
            doctor.QuaTrinhDaoTao = Truncate(quaTrinhDaoTao, 2000);
            _context.Entry(doctor.User).State = EntityState.Modified;
            _context.Entry(doctor).State = EntityState.Modified;

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = userId,
                HanhDong = "Cập nhật thông tin liên hệ",
                ChiTiet = $"{doctor.User.HoTen} tự cập nhật SĐT/email/giới thiệu từ trang Hồ sơ & Cài đặt."
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã lưu thông tin liên hệ.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Doctor/Profile/ProposeProfileChange - nhóm trường ảnh hưởng
        // pháp lý/hiển thị công khai: tạo Yêu cầu thay đổi (diff cũ→mới) thay
        // vì sửa thẳng, chờ Admin có quyền duyệt (Areas/Admin/Controllers/
        // ProfileApprovalsController.cs) xử lý.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ProposeProfileChange(
            string hoTen, DateTime? ngaySinh, string hocVi, string chuyenKhoa, int khoaId, string chucVu,
            string? soCCHN, DateTime? ngayCapCCHN, string? noiCapCCHN, string? phamViHanhNghe)
        {
            var userId = GetCurrentUserId();
            var doctor = await _context.Doctors.Include(d => d.User).FirstOrDefaultAsync(d => d.NguoiDungId == userId);
            if (doctor == null) return NotFound();

            var hasPending = await _context.ProfileChangeRequests
                .AnyAsync(r => r.BacSiId == doctor.Id && r.TrangThai == "ChoDuyet");
            if (hasPending)
            {
                TempData["ErrorMessage"] = "Bạn đang có 1 yêu cầu thay đổi hồ sơ chờ duyệt, vui lòng đợi Admin xử lý xong.";
                return RedirectToAction(nameof(Index));
            }

            if (string.IsNullOrWhiteSpace(hoTen) || string.IsNullOrWhiteSpace(hocVi) ||
                string.IsNullOrWhiteSpace(chuyenKhoa) || string.IsNullOrWhiteSpace(chucVu) ||
                !await _context.Departments.AnyAsync(d => d.Id == khoaId))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ các trường bắt buộc.";
                return RedirectToAction(nameof(Index));
            }

            var oldData = new ProfileChangeFields
            {
                HoTen = doctor.User.HoTen,
                NgaySinh = doctor.NgaySinh,
                HocVi = doctor.HocVi,
                ChuyenKhoa = doctor.ChuyenKhoa,
                KhoaId = doctor.KhoaId,
                ChucVu = doctor.ChucVu,
                SoCCHN = doctor.SoCCHN,
                NgayCapCCHN = doctor.NgayCapCCHN,
                NoiCapCCHN = doctor.NoiCapCCHN,
                PhamViHanhNghe = doctor.PhamViHanhNghe
            };
            var newData = new ProfileChangeFields
            {
                HoTen = hoTen.Trim(),
                NgaySinh = ngaySinh,
                HocVi = hocVi.Trim(),
                ChuyenKhoa = chuyenKhoa.Trim(),
                KhoaId = khoaId,
                ChucVu = chucVu.Trim(),
                SoCCHN = string.IsNullOrWhiteSpace(soCCHN) ? null : soCCHN.Trim(),
                NgayCapCCHN = ngayCapCCHN,
                NoiCapCCHN = string.IsNullOrWhiteSpace(noiCapCCHN) ? null : noiCapCCHN.Trim(),
                PhamViHanhNghe = string.IsNullOrWhiteSpace(phamViHanhNghe) ? null : phamViHanhNghe.Trim()
            };

            var request = new ProfileChangeRequest
            {
                BacSiId = doctor.Id,
                DuLieuCuJson = JsonSerializer.Serialize(oldData),
                DuLieuMoiJson = JsonSerializer.Serialize(newData)
            };
            _context.ProfileChangeRequests.Add(request);
            await _context.SaveChangesAsync();

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = userId,
                HanhDong = "Đề xuất thay đổi hồ sơ hành nghề",
                ChiTiet = $"{doctor.User.HoTen} tạo yêu cầu thay đổi hồ sơ hành nghề, chờ Admin duyệt.",
                DoiTuongLoai = "YeuCauThayDoiHoSo",
                DoiTuongId = request.Id
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã gửi yêu cầu thay đổi hồ sơ, chờ Admin duyệt.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Doctor/Profile/ChangePassword - cùng chuẩn với luồng khôi
        // phục mật khẩu (AuthController.SetNewPassword): thanh độ mạnh, chặn
        // trùng mật khẩu cũ, thu hồi mọi phiên khác qua SecurityStamp, ghi
        // audit log kèm IP thật, gửi email cảnh báo. Khác một điểm duy nhất -
        // đây là tự đổi mật khẩu đang biết trước (không phải quên mật khẩu)
        // nên cần xác nhận matKhauHienTai trước khi cho đổi.
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

            if (!PasswordPolicyHelper.IsCompliant(matKhauMoi))
            {
                TempData["ErrorMessage"] = "Mật khẩu mới chưa đáp ứng đủ yêu cầu (tối thiểu 8 ký tự, có chữ hoa, chữ thường và chữ số).";
                return RedirectToAction(nameof(Index));
            }

            if (matKhauMoi != xacNhanMatKhauMoi)
            {
                TempData["ErrorMessage"] = "Xác nhận mật khẩu mới không khớp.";
                return RedirectToAction(nameof(Index));
            }

            if (HashHelper.VerifyPassword(matKhauMoi, user.MatKhauHash))
            {
                TempData["ErrorMessage"] = "Mật khẩu mới không được trùng mật khẩu cũ.";
                return RedirectToAction(nameof(Index));
            }

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            user.MatKhauHash = HashHelper.HashPassword(matKhauMoi);
            user.SecurityStamp = Guid.NewGuid().ToString("N");

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = user.Id,
                HanhDong = "Đổi mật khẩu",
                ChiTiet = $"{user.HoTen} tự đổi mật khẩu tài khoản từ trang Hồ sơ & Cài đặt.",
                IpAddress = ip
            });
            await _context.SaveChangesAsync();

            await _emailSender.SendAsync(user.Email, "Mật khẩu MediFlow HMS vừa được thay đổi",
                $"Mật khẩu của bạn vừa được thay đổi lúc {DateTime.Now:HH:mm dd/MM/yyyy}. Nếu không phải bạn, liên hệ ngay hotline 1900-6900.");

            // Ký lại phiên hiện tại với SecurityStamp mới ngay lập tức - nếu
            // không, chính phiên vừa chứng minh mật khẩu cũ sẽ bị
            // SecurityStampCookieValidator từ chối ở request kế tiếp (cookie
            // còn mang stamp cũ). Mọi phiên/thiết bị khác thì KHÔNG được ký
            // lại nên vẫn bị từ chối như bình thường - đây chính là cách "thu
            // hồi mọi phiên khác". Giữ nguyên claim "sid" cũ - đây là TIẾP
            // DIỄN cùng phiên (không phải đăng nhập mới), không tạo dòng
            // PhienDangNhap mới.
            var oldSid = User.FindFirst("sid")?.Value;
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.HoTen),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.VaiTro),
                new Claim(ClaimTypes.MobilePhone, user.Sdt),
                new Claim("SecurityStamp", user.SecurityStamp)
            };
            if (!string.IsNullOrEmpty(oldSid)) claims.Add(new Claim("sid", oldSid));
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            TempData["SuccessMessage"] = "Đã đổi mật khẩu thành công. Các thiết bị khác đã đăng nhập trước đó sẽ cần đăng nhập lại.";
            return RedirectToAction(nameof(Index));
        }

        // GET: /Doctor/Profile/TwoFactorSetup - sinh secret MỚI mỗi lần tải
        // trang (chưa lưu DB), round-trip qua hidden field tới POST cùng
        // action - không cần Session/TempData cho bước tạm này.
        [HttpGet]
        public async Task<IActionResult> TwoFactorSetup()
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

            if (user.TotpBatDau)
            {
                TempData["ErrorMessage"] = "Tài khoản đã bật xác thực 2 lớp.";
                return RedirectToAction(nameof(Index));
            }

            var secret = TotpHelper.GenerateSecretBase32();
            PopulateTwoFactorSetupViewBag(secret, user.Email);
            return View();
        }

        // POST: /Doctor/Profile/TwoFactorSetup - xác nhận mã 6 số trước khi
        // lưu, tránh tình huống quét sai QR rồi tự khoá mình khỏi 2FA.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TwoFactorSetup(string secret, string code)
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

            if (!TotpHelper.ValidateCode(secret, code, DateTime.UtcNow))
            {
                TempData["ErrorMessage"] = "Mã xác thực không đúng. Vui lòng thử lại.";
                PopulateTwoFactorSetupViewBag(secret, user.Email);
                return View();
            }

            user.TotpBiMat = secret;
            user.TotpBatDau = true;

            var backupCodes = BackupCodeHelper.GenerateCodes();
            foreach (var plainCode in backupCodes)
            {
                _context.TotpBackupCodes.Add(new TotpBackupCode
                {
                    NguoiDungId = userId,
                    MaHash = HashHelper.HashPassword(plainCode)
                });
            }

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = userId,
                HanhDong = "Bật xác thực 2 lớp",
                ChiTiet = $"{user.HoTen} bật xác thực 2 lớp (TOTP) từ trang Hồ sơ & Cài đặt."
            });
            await _context.SaveChangesAsync();

            ViewBag.Completed = true;
            ViewBag.BackupCodes = backupCodes;
            return View();
        }

        // POST: /Doctor/Profile/DisableTwoFactor - yêu cầu nhập lại mật khẩu,
        // cùng mức xác nhận với Đổi mật khẩu, vì đây là hạ thấp bảo mật tài khoản.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DisableTwoFactor(string matKhauHienTai)
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

            if (string.IsNullOrEmpty(matKhauHienTai) || !HashHelper.VerifyPassword(matKhauHienTai, user.MatKhauHash))
            {
                TempData["ErrorMessage"] = "Mật khẩu hiện tại không chính xác.";
                return RedirectToAction(nameof(Index));
            }

            user.TotpBiMat = null;
            user.TotpBatDau = false;

            var backupCodes = await _context.TotpBackupCodes.Where(c => c.NguoiDungId == userId).ToListAsync();
            _context.TotpBackupCodes.RemoveRange(backupCodes);

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = userId,
                HanhDong = "Tắt xác thực 2 lớp",
                ChiTiet = $"{user.HoTen} tắt xác thực 2 lớp (TOTP) từ trang Hồ sơ & Cài đặt."
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã tắt xác thực 2 lớp.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Doctor/Profile/EndSession - đăng xuất từ xa đúng 1 thiết bị.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EndSession(int sessionId)
        {
            var userId = GetCurrentUserId();
            var session = await _context.LoginSessions
                .FirstOrDefaultAsync(s => s.Id == sessionId && s.NguoiDungId == userId);
            if (session == null) return NotFound();

            session.TrangThai = "DaDangXuat";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã đăng xuất thiết bị đó.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Doctor/Profile/EndAllOtherSessions - giải pháp cho máy trạm
        // khoa phòng dùng chung: đăng xuất mọi phiên khác, giữ phiên hiện tại.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EndAllOtherSessions()
        {
            var userId = GetCurrentUserId();
            var currentSid = User.FindFirst("sid")?.Value;

            var otherSessions = await _context.LoginSessions
                .Where(s => s.NguoiDungId == userId && s.TrangThai == "HoatDong" && s.SessionToken != currentSid)
                .ToListAsync();
            foreach (var session in otherSessions) session.TrangThai = "DaDangXuat";
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã đăng xuất {otherSessions.Count} thiết bị khác.";
            return RedirectToAction(nameof(Index));
        }

        // POST: /Doctor/Profile/SaveUiPreferences
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveUiPreferences(bool sidebarThuGonMacDinh, int? soDongMoiTrang)
        {
            var userId = GetCurrentUserId();
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return NotFound();

            user.SidebarThuGonMacDinh = sidebarThuGonMacDinh;
            user.SoDongMoiTrangMacDinh = soDongMoiTrang.HasValue ? PagedList<object>.NormalisePageSize(soDongMoiTrang) : null;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Đã lưu tuỳ chọn giao diện.";
            return RedirectToAction(nameof(Index));
        }

        private void PopulateTwoFactorSetupViewBag(string secret, string accountLabel)
        {
            var uri = TotpHelper.BuildProvisioningUri(secret, accountLabel);
            var qrGenerator = new QRCoder.QRCodeGenerator();
            var qrData = qrGenerator.CreateQrCode(uri, QRCoder.QRCodeGenerator.ECCLevel.Q);
            var qrPng = new QRCoder.PngByteQRCode(qrData).GetGraphic(10);

            ViewBag.Secret = secret;
            ViewBag.QrCodeDataUri = "data:image/png;base64," + Convert.ToBase64String(qrPng);
        }

        private static string? Truncate(string? value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var trimmed = value.Trim();
            return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
        }

        private int GetCurrentUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var userId) ? userId : 0;
        }
    }
}
