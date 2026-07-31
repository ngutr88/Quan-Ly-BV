
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Models;
using QuanLyBenhVien.Helpers;
using QuanLyBenhVien.Services;

namespace QuanLyBenhVien.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ISmsSender _smsSender;

        public AuthController(ApplicationDbContext context, IEmailSender emailSender, ISmsSender smsSender)
        {
            _context = context;
            _emailSender = emailSender;
            _smsSender = smsSender;
        }

        // GET: /Auth/Login
        [HttpGet]
        public IActionResult Login(string returnUrl)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return SafeRedirect(returnUrl) ?? RedirectByUserRole();
            }

            ViewBag.ReturnUrl = SanitizeReturnUrl(returnUrl);
            return View();
        }

        // POST: /Auth/Login
        [HttpPost]
        public async Task<IActionResult> Login(string identifier, string password, bool rememberMe, string role, string returnUrl)
        {
            identifier = identifier?.Trim() ?? string.Empty;
            User? user = null;
            if (identifier.Contains("@"))
            {
                user = await _context.Users.FirstOrDefaultAsync(u => u.Email == identifier || u.Sdt == identifier);
            }
            else
            {
                var emailPrefix = identifier + "@";
                user = await _context.Users.FirstOrDefaultAsync(u => u.Email == identifier || u.Sdt == identifier || u.Email.StartsWith(emailPrefix));
            }

            // Demo credentials are public UI fixtures. Repair legacy production
            // hashes on first successful demo login without touching other users.
            var demoRole = GetDemoRole(identifier, password);
            if (user != null && demoRole != null)
            {
                if (!HashHelper.VerifyPassword(password, user.MatKhauHash) || user.VaiTro != demoRole || user.TrangThai != "Active")
                {
                    user.MatKhauHash = HashHelper.HashPassword(password);
                    user.VaiTro = demoRole;
                    user.TrangThai = "Active";
                    await _context.SaveChangesAsync();
                }
            }

            if (user == null || !HashHelper.VerifyPassword(password, user.MatKhauHash))
            {
                TempData["ErrorMessage"] = "Thông tin tài khoản hoặc mật khẩu không chính xác.";
                return View();
            }

            if (HashHelper.IsLegacyHash(user.MatKhauHash))
            {
                user.MatKhauHash = HashHelper.HashPassword(password);
                await _context.SaveChangesAsync();
            }

            if (user.PhaiDoiMatKhau && user.MatKhauTamHetHan.HasValue && user.MatKhauTamHetHan.Value < DateTime.Now)
            {
                TempData["ErrorMessage"] = "Mật khẩu tạm đã hết hạn. Vui lòng liên hệ quản trị viên để được cấp lại.";
                return View();
            }

            if (!string.IsNullOrEmpty(role) && user.VaiTro != role)
            {
                var roleName = role switch
                {
                    "Admin" => "Quản trị viên",
                    "Doctor" => "Bác sĩ",
                    _ => "Bệnh nhân"
                };
                TempData["ErrorMessage"] = $"Tài khoản này không phải là {roleName}. Vui lòng kiểm tra lại vai trò đăng nhập.";
                return View();
            }

            if (user.TrangThai == "Blocked")
            {
                TempData["ErrorMessage"] = "Tài khoản của bạn đã bị khóa. Vui lòng liên hệ Admin.";
                return View();
            }

            // Create user claims
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.HoTen),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.VaiTro),
                new Claim(ClaimTypes.MobilePhone, user.Sdt),
                new Claim("SecurityStamp", user.SecurityStamp)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = user.Id,
                HanhDong = "Đăng nhập",
                ChiTiet = $"{user.HoTen} ({user.VaiTro}) đăng nhập thành công.",
                IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"
            });
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đăng nhập thành công! Chào mừng {user.HoTen}.";

            // Honour where the visitor was heading (e.g. the booking page they
            // picked a department on) before falling back to their role home.
            var intended = SafeRedirect(returnUrl);
            if (intended != null) return intended;

            // Redirect based on role
            if (user.VaiTro == "Admin") return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            if (user.VaiTro == "Doctor") return RedirectToAction("Index", "Dashboard", new { area = "Doctor" });
            return RedirectToAction("Index", "Dashboard", new { area = "Patient" });
        }

        /// <summary>
        /// Accepts only same-site relative paths so a crafted "returnUrl" cannot
        /// bounce a freshly authenticated user to an external site.
        /// </summary>
        private string? SanitizeReturnUrl(string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl)) return null;
            return Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        }

        private IActionResult? SafeRedirect(string? returnUrl)
        {
            var safe = SanitizeReturnUrl(returnUrl);
            return safe == null ? null : Redirect(safe);
        }

        // GET: /Auth/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Auth/Register
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string fullName, string? email, string phone, string password, DateTime dob, string gender, string bhyt, string cccd, bool agree = false)
        {
            fullName = (fullName ?? string.Empty).Trim();
            email = (email ?? string.Empty).Trim();
            phone = (phone ?? string.Empty).Trim();
            gender = (gender ?? string.Empty).Trim();
            bhyt = (bhyt ?? string.Empty).Trim();
            cccd = (cccd ?? string.Empty).Trim();

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(phone) ||
                string.IsNullOrWhiteSpace(password) || password.Length < 6 ||
                dob == default || dob.Date >= DateTime.Today || string.IsNullOrWhiteSpace(gender) ||
                !cccd.All(char.IsDigit) || cccd.Length != 12 || string.IsNullOrWhiteSpace(bhyt) || !agree)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ tất cả thông tin bắt buộc và xác nhận chính sách. Email là trường duy nhất không bắt buộc.";
                return View();
            }

            if (!string.IsNullOrWhiteSpace(email) &&
                !System.Net.Mail.MailAddress.TryCreate(email, out _))
            {
                TempData["ErrorMessage"] = "Địa chỉ email không đúng định dạng.";
                return View();
            }

            // Check duplicates
            var existingUser = await _context.Users.AnyAsync(u => u.Sdt == phone ||
                (!string.IsNullOrEmpty(email) && u.Email == email));
            if (existingUser)
            {
                TempData["ErrorMessage"] = "Email hoặc Số điện thoại đã được sử dụng.";
                return View();
            }

            // Create temporary session data for registration
            HttpContext.Session.SetString("Reg_FullName", fullName);
            HttpContext.Session.SetString("Reg_Email", email);
            HttpContext.Session.SetString("Reg_Phone", phone);
            HttpContext.Session.SetString("Reg_Password", HashPasswordSimple(password));
            HttpContext.Session.SetString("Reg_Dob", dob.ToString("yyyy-MM-dd"));
            HttpContext.Session.SetString("Reg_Gender", gender);
            HttpContext.Session.SetString("Reg_BHYT", bhyt ?? string.Empty);
            HttpContext.Session.SetString("Reg_CCCD", cccd ?? string.Empty);

            HttpContext.Session.SetString("Reg_OTP", "123456");
            TempData["SuccessMessage"] = "Mã xác thực OTP demo là 123456.";
            return RedirectToAction("VerifyOtp");
        }

        // GET: /Auth/VerifyOtp
        [HttpGet]
        public IActionResult VerifyOtp()
        {
            var email = HttpContext.Session.GetString("Reg_Email");
            var phone = HttpContext.Session.GetString("Reg_Phone");
            if (string.IsNullOrEmpty(phone))
            {
                return RedirectToAction("Register");
            }
            ViewBag.Email = string.IsNullOrEmpty(email) ? phone : email;
            return View();
        }

        // POST: /Auth/VerifyOtp
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(string otp)
        {
            otp = (otp ?? string.Empty).Trim();
            var sessionOtp = HttpContext.Session.GetString("Reg_OTP");
            if (otp.Length != 6 || !otp.All(char.IsDigit) || sessionOtp != otp)
            {
                TempData["ErrorMessage"] = "Mã OTP không chính xác. Hãy nhập mã demo 123456.";
                var email = HttpContext.Session.GetString("Reg_Email");
                ViewBag.Email = string.IsNullOrEmpty(email) ? HttpContext.Session.GetString("Reg_Phone") : email;
                return View();
            }

            // Verification successful, create user in DB
            var user = new User
            {
                HoTen = HttpContext.Session.GetString("Reg_FullName")!,
                Email = HttpContext.Session.GetString("Reg_Email")!,
                Sdt = HttpContext.Session.GetString("Reg_Phone")!,
                MatKhauHash = HttpContext.Session.GetString("Reg_Password")!,
                VaiTro = "Patient",
                TrangThai = "Active"
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            var patient = new Patient
            {
                NguoiDungId = user.Id,
                NgaySinh = DateTime.Parse(HttpContext.Session.GetString("Reg_Dob")!),
                GioiTinh = HttpContext.Session.GetString("Reg_Gender")!,
                SoBHYT = HttpContext.Session.GetString("Reg_BHYT") ?? string.Empty,
                SoCCCD = HttpContext.Session.GetString("Reg_CCCD") ?? string.Empty,
                NhomMau = "O+",
                TienSuBenh = "Không"
            };
            _context.Patients.Add(patient);
            await _context.SaveChangesAsync();

            // Clear registration session
            HttpContext.Session.Remove("Reg_FullName");
            HttpContext.Session.Remove("Reg_Email");
            HttpContext.Session.Remove("Reg_Phone");
            HttpContext.Session.Remove("Reg_Password");
            HttpContext.Session.Remove("Reg_Dob");
            HttpContext.Session.Remove("Reg_Gender");
            HttpContext.Session.Remove("Reg_BHYT");
            HttpContext.Session.Remove("Reg_CCCD");
            HttpContext.Session.Remove("Reg_OTP");

            // Auto Sign In after successful registration
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.HoTen),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.VaiTro),
                new Claim(ClaimTypes.MobilePhone, user.Sdt),
                new Claim("SecurityStamp", user.SecurityStamp)
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            TempData["SuccessMessage"] = "Kích hoạt tài khoản và đăng nhập thành công!";
            return RedirectToAction("Index", "Dashboard", new { area = "Patient" });
        }

        // GET: /Auth/ForgotPassword (bước 1)
        [HttpGet]
        public async Task<IActionResult> ForgotPassword()
        {
            var ip = ClientIp();
            var ipCount = await CountRecentByIpAsync(ip);
            ViewBag.Blocked = ipCount >= PasswordResetPolicy.HardBlockThreshold;
            ViewBag.RequireCaptcha = !(bool)ViewBag.Blocked && ipCount >= PasswordResetPolicy.CaptchaThreshold;
            if ((bool)ViewBag.RequireCaptcha) PrepareCaptchaChallenge();
            return View();
        }

        // POST: /Auth/ForgotPassword (bước 1)
        //
        // Yêu cầu 2 (chống dò tài khoản): phản hồi PHẢI giống hệt nhau dù định
        // danh có khớp tài khoản hay không, dù tài khoản là bệnh nhân hay nhân
        // sự. Để đạt điều đó: luôn tra cứu, luôn sinh+hash một mã OTP, luôn ghi
        // 1 hàng YeuCauKhoiPhucMatKhau, luôn gọi cùng shape hàm gửi mock - chỉ
        // MaXacNhanHash mới quyết định mã có thật sự dùng được ở bước 2 hay
        // không (null = không có mã thật, nhân sự/tài khoản không tồn tại luôn
        // rơi vào nhánh này, và bước 2 sẽ luôn thất bại giống hệt nhập sai mã).
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(string identifier, string? captchaAnswer)
        {
            identifier = (identifier ?? string.Empty).Trim();
            var ip = ClientIp();

            var ipCount = await CountRecentByIpAsync(ip);

            // Ngưỡng theo IP là ngưỡng DUY NHẤT được phép ảnh hưởng phản hồi
            // hiển thị - xem PasswordResetPolicy để biết lý do (ngưỡng theo tài
            // khoản chỉ được phép tác động âm thầm, ở dưới).
            if (ipCount >= PasswordResetPolicy.HardBlockThreshold)
            {
                TempData["ErrorMessage"] = "Vui lòng thử lại sau.";
                ViewBag.Blocked = true;
                return View();
            }

            if (ipCount >= PasswordResetPolicy.CaptchaThreshold)
            {
                var expected = HttpContext.Session.GetInt32("PwReset_Captcha");
                HttpContext.Session.Remove("PwReset_Captcha");
                if (expected == null || !int.TryParse(captchaAnswer, out var given) || given != expected)
                {
                    TempData["ErrorMessage"] = "Vui lòng giải đúng phép tính xác nhận bên dưới.";
                    ViewBag.RequireCaptcha = true;
                    PrepareCaptchaChallenge();
                    return View();
                }
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == identifier || u.Sdt == identifier);
            var isEligiblePatient = user != null && user.VaiTro == "Patient" && user.TrangThai == "Active" && !user.DaXoa;

            var canIssueRealOtp = isEligiblePatient;
            if (isEligiblePatient)
            {
                var accountCount = await CountRecentByAccountAsync(user!.Id);
                if (accountCount >= PasswordResetPolicy.HardBlockThreshold) canIssueRealOtp = false;
            }

            // Định danh quyết định kênh hiển thị (Email/Sdt) theo đúng định dạng
            // gõ vào - tính độc lập với việc tài khoản có tồn tại hay không, để
            // không tạo thêm một nhánh rẽ khác nhau giữa 2 trường hợp.
            var channel = identifier.Contains('@') ? "Email" : "Sdt";
            if (isEligiblePatient && channel == "Sdt")
            {
                var smsToday = await CountSmsTodayAsync(user!.Id);
                if (smsToday >= PasswordResetPolicy.MaxSmsPerAccountPerDay) canIssueRealOtp = false;
            }

            var otp = GenerateOtp(); // luôn sinh, kể cả khi sẽ bị bỏ (đối xứng thời gian xử lý)

            var request = new PasswordResetRequest
            {
                NguoiDungId = isEligiblePatient ? user!.Id : null,
                IpAddress = ip,
                KhoiTaoBoi = "TuPhucVu",
                Kenh = channel,
                MaXacNhanHash = canIssueRealOtp ? Sha256Base64(otp) : null,
                ThoiHanMa = DateTime.Now.Add(PasswordResetPolicy.OtpValidity),
                LanGuiGanNhatLuc = DateTime.Now
            };
            _context.PasswordResetRequests.Add(request);

            // Yêu cầu mới vô hiệu mọi OTP/token cũ của cùng tài khoản.
            if (isEligiblePatient)
            {
                var priorActive = await _context.PasswordResetRequests
                    .Where(r => r.NguoiDungId == user!.Id && !r.MaDaDung)
                    .ToListAsync();
                foreach (var prior in priorActive) prior.MaDaDung = true;
            }

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = isEligiblePatient ? user!.Id : null,
                HanhDong = "Yêu cầu mã khôi phục mật khẩu",
                ChiTiet = "Yêu cầu gửi mã khôi phục mật khẩu.",
                IpAddress = ip
            });
            await _context.SaveChangesAsync();

            var target = isEligiblePatient
                ? (channel == "Email" ? user!.Email : user!.Sdt)
                : (channel == "Email" ? identifier : identifier);
            if (channel == "Email")
                await _emailSender.SendAsync(target, "Mã khôi phục mật khẩu MediFlow HMS", $"Mã xác nhận của bạn là {otp}. Mã có hiệu lực trong 5 phút.");
            else
                await _smsSender.SendAsync(target, $"Mã khôi phục mật khẩu MediFlow HMS của bạn là {otp}. Có hiệu lực 5 phút.");

            HttpContext.Session.SetInt32("PwReset_RequestId", request.Id);
            HttpContext.Session.SetString("PwReset_MaskedTarget", MaskIdentifier(identifier));

            // Môi trường demo: hiển thị mã luôn ở đây (giống tiền lệ Reg_OTP) -
            // BẮT BUỘC hiển thị giống hệt nhau dù mã có thật hay không, nếu
            // không bản thân việc "có hiện mã hay không" sẽ lại là kênh dò
            // tài khoản mới.
            TempData["SuccessMessage"] = $"Nếu thông tin này đã đăng ký, mã xác nhận sẽ được gửi trong ít phút. (Môi trường demo: mã vừa tạo là {otp}.)";
            return RedirectToAction(nameof(VerifyResetCode));
        }

        // GET: /Auth/VerifyResetCode (bước 2)
        [HttpGet]
        public async Task<IActionResult> VerifyResetCode()
        {
            var requestId = HttpContext.Session.GetInt32("PwReset_RequestId");
            if (requestId == null) return RedirectToAction(nameof(ForgotPassword));
            var request = await _context.PasswordResetRequests.FindAsync(requestId.Value);
            if (request == null) return RedirectToAction(nameof(ForgotPassword));

            PopulateVerifyResetCodeViewBag(request);
            return View();
        }

        // POST: /Auth/VerifyResetCode (bước 2)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyResetCode(string code)
        {
            var requestId = HttpContext.Session.GetInt32("PwReset_RequestId");
            if (requestId == null) return RedirectToAction(nameof(ForgotPassword));
            var request = await _context.PasswordResetRequests.FindAsync(requestId.Value);
            if (request == null) return RedirectToAction(nameof(ForgotPassword));

            code = (code ?? string.Empty).Trim();
            var ip = ClientIp();

            var valid = !request.MaDaDung
                && request.MaXacNhanHash != null
                && request.ThoiHanMa.HasValue && request.ThoiHanMa.Value >= DateTime.Now
                && Sha256Base64(code) == request.MaXacNhanHash;

            if (!valid)
            {
                if (!request.MaDaDung)
                {
                    request.SoLanNhapSai++;
                    if (request.SoLanNhapSai >= PasswordResetPolicy.MaxOtpAttempts)
                    {
                        request.MaDaDung = true;
                        TempData["ErrorMessage"] = "Mã đã bị vô hiệu do nhập sai nhiều lần, vui lòng yêu cầu mã mới.";
                        _context.AuditLogs.Add(new AuditLog
                        {
                            NguoiDungId = request.NguoiDungId,
                            HanhDong = "Mã khôi phục mật khẩu bị vô hiệu",
                            ChiTiet = "Mã xác nhận bị vô hiệu do nhập sai quá số lần cho phép.",
                            IpAddress = ip
                        });
                    }
                    else
                    {
                        TempData["ErrorMessage"] = "Mã không đúng hoặc đã hết hạn.";
                        _context.AuditLogs.Add(new AuditLog
                        {
                            NguoiDungId = request.NguoiDungId,
                            HanhDong = "Nhập sai mã khôi phục mật khẩu",
                            ChiTiet = $"Nhập sai mã xác nhận (lần {request.SoLanNhapSai}).",
                            IpAddress = ip
                        });
                    }
                    await _context.SaveChangesAsync();
                }
                else
                {
                    TempData["ErrorMessage"] = "Mã không đúng hoặc đã hết hạn.";
                }

                PopulateVerifyResetCodeViewBag(request);
                return View();
            }

            request.MaDaDung = true; // dùng 1 lần - vô hiệu ngay khi xác thực đúng
            var rawToken = GenerateToken();
            request.ResetTokenHash = Sha256Base64(rawToken);
            request.ThoiHanResetToken = DateTime.Now.Add(PasswordResetPolicy.ResetTokenValidity);
            request.ResetTokenDaDung = false;

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = request.NguoiDungId,
                HanhDong = "Xác thực mã khôi phục mật khẩu thành công",
                ChiTiet = "Xác thực mã xác nhận thành công, chuyển sang bước đặt mật khẩu mới.",
                IpAddress = ip
            });
            await _context.SaveChangesAsync();

            HttpContext.Session.Remove("PwReset_RequestId");
            return RedirectToAction(nameof(SetNewPassword), new { token = rawToken });
        }

        // POST: /Auth/ResendResetCode (AJAX, bước 2)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendResetCode()
        {
            var requestId = HttpContext.Session.GetInt32("PwReset_RequestId");
            if (requestId == null) return Json(new { success = false, message = "Phiên đã hết hạn, vui lòng thử lại từ đầu." });
            var request = await _context.PasswordResetRequests.FindAsync(requestId.Value);
            if (request == null || request.MaDaDung) return Json(new { success = false, message = "Phiên đã hết hạn, vui lòng thử lại từ đầu." });

            if (request.SoLanGuiLai >= PasswordResetPolicy.MaxResendPerRequest)
                return Json(new { success = false, message = "Bạn đã đạt giới hạn số lần gửi lại mã." });

            if (request.LanGuiGanNhatLuc.HasValue && DateTime.Now - request.LanGuiGanNhatLuc.Value < PasswordResetPolicy.ResendCooldown)
                return Json(new { success = false, message = "Vui lòng đợi trước khi gửi lại mã." });

            var otp = GenerateOtp();
            var hadRealOtp = request.MaXacNhanHash != null;
            if (hadRealOtp) request.MaXacNhanHash = Sha256Base64(otp);
            request.ThoiHanMa = DateTime.Now.Add(PasswordResetPolicy.OtpValidity);
            request.SoLanNhapSai = 0;
            request.SoLanGuiLai++;
            request.LanGuiGanNhatLuc = DateTime.Now;

            var ip = ClientIp();
            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = request.NguoiDungId,
                HanhDong = "Gửi lại mã khôi phục mật khẩu",
                ChiTiet = "Gửi lại mã xác nhận.",
                IpAddress = ip
            });
            await _context.SaveChangesAsync();

            if (hadRealOtp && request.NguoiDungId.HasValue)
            {
                var user = await _context.Users.FindAsync(request.NguoiDungId.Value);
                if (user != null)
                {
                    if (request.Kenh == "Email") await _emailSender.SendAsync(user.Email, "Mã khôi phục mật khẩu MediFlow HMS", $"Mã xác nhận mới của bạn là {otp}.");
                    else await _smsSender.SendAsync(user.Sdt, $"Mã khôi phục mật khẩu MediFlow HMS của bạn là {otp}.");
                }
            }

            return Json(new
            {
                success = true,
                message = $"Đã gửi lại mã. (Môi trường demo: mã mới là {otp}.)",
                cooldownSeconds = (int)PasswordResetPolicy.ResendCooldown.TotalSeconds,
                remaining = Math.Max(0, PasswordResetPolicy.MaxResendPerRequest - request.SoLanGuiLai)
            });
        }

        // GET: /Auth/SetNewPassword (bước 3)
        [HttpGet]
        public async Task<IActionResult> SetNewPassword(string token)
        {
            var request = await FindActiveResetTokenAsync(token);
            if (request?.NguoiDungId == null)
            {
                TempData["ErrorMessage"] = "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            ViewBag.Token = token;
            return View();
        }

        // POST: /Auth/SetNewPassword (bước 3)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetNewPassword(string token, string newPassword, string confirmPassword)
        {
            var request = await FindActiveResetTokenAsync(token);
            if (request?.NguoiDungId == null)
            {
                TempData["ErrorMessage"] = "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == request.NguoiDungId.Value);
            if (user == null)
            {
                TempData["ErrorMessage"] = "Liên kết đặt lại mật khẩu không hợp lệ hoặc đã hết hạn.";
                return RedirectToAction(nameof(ForgotPassword));
            }

            if (!IsPasswordPolicyCompliant(newPassword) || newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "Mật khẩu mới chưa đáp ứng đủ yêu cầu hoặc xác nhận không khớp.";
                ViewBag.Token = token;
                return View();
            }

            if (HashHelper.VerifyPassword(newPassword, user.MatKhauHash))
            {
                TempData["ErrorMessage"] = "Mật khẩu mới không được trùng mật khẩu cũ.";
                ViewBag.Token = token;
                return View();
            }

            var ip = ClientIp();
            user.MatKhauHash = HashHelper.HashPassword(newPassword);
            user.SecurityStamp = Guid.NewGuid().ToString("N");
            user.PhaiDoiMatKhau = false;
            user.MatKhauTamHetHan = null;
            request.ResetTokenDaDung = true;

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = user.Id,
                HanhDong = "Đặt lại mật khẩu thành công",
                ChiTiet = $"{user.HoTen} đã đặt lại mật khẩu qua luồng khôi phục.",
                IpAddress = ip
            });
            await _context.SaveChangesAsync();

            await _emailSender.SendAsync(user.Email, "Mật khẩu MediFlow HMS vừa được thay đổi",
                $"Mật khẩu của bạn vừa được thay đổi lúc {DateTime.Now:HH:mm dd/MM/yyyy}. Nếu không phải bạn, liên hệ ngay hotline 1900-6900.");

            return RedirectToAction(nameof(ResetPasswordConfirmation));
        }

        // GET: /Auth/ResetPasswordConfirmation (bước 4)
        [HttpGet]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }

        // GET/POST: /Auth/ForcedPasswordChange - nhân sự dùng mật khẩu tạm do
        // Admin cấp buộc phải đổi trước khi làm bất kỳ việc gì khác
        // (ForcePasswordChangeFilter điều hướng về đây).
        [HttpGet]
        [Authorize]
        public IActionResult ForcedPasswordChange()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        public async Task<IActionResult> ForcedPasswordChange(string newPassword, string confirmPassword)
        {
            var idClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (idClaim == null || !int.TryParse(idClaim.Value, out var userId)) return RedirectToAction(nameof(Login));

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
            if (user == null) return RedirectToAction(nameof(Login));

            if (!IsPasswordPolicyCompliant(newPassword) || newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "Mật khẩu mới chưa đáp ứng đủ yêu cầu hoặc xác nhận không khớp.";
                return View();
            }

            if (HashHelper.VerifyPassword(newPassword, user.MatKhauHash))
            {
                TempData["ErrorMessage"] = "Mật khẩu mới không được trùng mật khẩu cũ.";
                return View();
            }

            user.MatKhauHash = HashHelper.HashPassword(newPassword);
            user.PhaiDoiMatKhau = false;
            user.MatKhauTamHetHan = null;
            user.SecurityStamp = Guid.NewGuid().ToString("N");

            _context.AuditLogs.Add(new AuditLog
            {
                NguoiDungId = user.Id,
                HanhDong = "Đổi mật khẩu bắt buộc",
                ChiTiet = $"{user.HoTen} đã đổi mật khẩu tạm sau khi được Admin cấp.",
                IpAddress = ClientIp()
            });
            await _context.SaveChangesAsync();

            // Ký lại phiên với claim SecurityStamp mới ngay lập tức - nếu không,
            // chính phiên vừa chứng minh mật khẩu tạm sẽ bị OnValidatePrincipal
            // từ chối ở request kế tiếp (vì cookie còn mang stamp cũ).
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.HoTen),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.VaiTro),
                new Claim(ClaimTypes.MobilePhone, user.Sdt),
                new Claim("SecurityStamp", user.SecurityStamp)
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            TempData["SuccessMessage"] = "Đổi mật khẩu thành công.";
            return RedirectByUserRole();
        }

        private void PopulateVerifyResetCodeViewBag(PasswordResetRequest request)
        {
            ViewBag.MaskedTarget = HttpContext.Session.GetString("PwReset_MaskedTarget") ?? "•••";
            ViewBag.ExpiresAtUnixMs = new DateTimeOffset(request.ThoiHanMa ?? DateTime.Now, DateTimeOffset.Now.Offset).ToUnixTimeMilliseconds();
            ViewBag.ResendRemaining = Math.Max(0, PasswordResetPolicy.MaxResendPerRequest - request.SoLanGuiLai);
            ViewBag.ResendCooldownSeconds = request.LanGuiGanNhatLuc.HasValue
                ? Math.Max(0, (int)(PasswordResetPolicy.ResendCooldown - (DateTime.Now - request.LanGuiGanNhatLuc.Value)).TotalSeconds)
                : 0;
        }

        private async Task<PasswordResetRequest?> FindActiveResetTokenAsync(string? token)
        {
            if (string.IsNullOrEmpty(token)) return null;
            var tokenHash = Sha256Base64(token);
            return await _context.PasswordResetRequests.FirstOrDefaultAsync(r =>
                r.ResetTokenHash == tokenHash && !r.ResetTokenDaDung &&
                r.ThoiHanResetToken.HasValue && r.ThoiHanResetToken.Value >= DateTime.Now);
        }

        private void PrepareCaptchaChallenge()
        {
            var a = Random.Shared.Next(1, 10);
            var b = Random.Shared.Next(1, 10);
            HttpContext.Session.SetInt32("PwReset_Captcha", a + b);
            ViewBag.CaptchaA = a;
            ViewBag.CaptchaB = b;
        }

        private async Task<int> CountRecentByIpAsync(string ip)
        {
            var since = DateTime.Now - PasswordResetPolicy.RateLimitWindow;
            return await _context.PasswordResetRequests.CountAsync(r => r.IpAddress == ip && r.NgayTao >= since);
        }

        private async Task<int> CountRecentByAccountAsync(int userId)
        {
            var since = DateTime.Now - PasswordResetPolicy.RateLimitWindow;
            return await _context.PasswordResetRequests.CountAsync(r => r.NguoiDungId == userId && r.NgayTao >= since);
        }

        private async Task<int> CountSmsTodayAsync(int userId)
        {
            var todayStart = DateTime.Today;
            return await _context.PasswordResetRequests.CountAsync(r => r.NguoiDungId == userId && r.Kenh == "Sdt" && r.NgayTao >= todayStart);
        }

        private string ClientIp() => HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

        private static bool IsPasswordPolicyCompliant(string? password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8) return false;
            return password.Any(char.IsUpper) && password.Any(char.IsLower) && password.Any(char.IsDigit);
        }

        private static string GenerateOtp() => RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        private static string GenerateToken()
        {
            var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
            return raw.Replace('+', '-').Replace('/', '_').TrimEnd('=');
        }

        private static string Sha256Base64(string input)
        {
            using var sha = SHA256.Create();
            return Convert.ToBase64String(sha.ComputeHash(Encoding.UTF8.GetBytes(input)));
        }

        private static string MaskIdentifier(string identifier)
        {
            if (identifier.Contains('@'))
            {
                var parts = identifier.Split('@', 2);
                var visible = parts[0].Length <= 2 ? parts[0] : parts[0].Substring(0, 2);
                return parts.Length == 2 ? $"{visible}•••@{parts[1]}" : $"{visible}•••";
            }

            if (identifier.Length >= 7)
                return $"{identifier.Substring(0, 3)}•••{identifier.Substring(identifier.Length - 4)}";

            return "•••" + identifier;
        }

        // GET: /Auth/Logout
        public async Task<IActionResult> Logout()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim != null && int.TryParse(claim.Value, out var userId))
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    NguoiDungId = userId,
                    HanhDong = "Đăng xuất",
                    ChiTiet = $"{User.Identity?.Name} đăng xuất khỏi hệ thống.",
                    IpAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1"
                });
                await _context.SaveChangesAsync();
            }

            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            TempData["SuccessMessage"] = "Bạn đã đăng xuất khỏi hệ thống.";
            return RedirectToAction("Index", "Home");
        }

        // GET: /Auth/AccessDenied
        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectByUserRole()
        {
            if (User.IsInRole("Admin")) return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            if (User.IsInRole("Doctor")) return RedirectToAction("Index", "Dashboard", new { area = "Doctor" });
            return RedirectToAction("Index", "Dashboard", new { area = "Patient" });
        }

        private string HashPasswordSimple(string password)
        {
            return HashHelper.HashPassword(password);
        }

        private static string? GetDemoRole(string identifier, string password)
        {
            if (identifier.Equals("admin@hms.com", StringComparison.OrdinalIgnoreCase) && password == "Admin@123")
                return "Admin";
            if (identifier.Equals("doctor@hms.com", StringComparison.OrdinalIgnoreCase) && password == "Doctor@123")
                return "Doctor";
            if (identifier.Equals("patient@hms.com", StringComparison.OrdinalIgnoreCase) && password == "Patient@123")
                return "Patient";

            return null;
        }
    }
}
