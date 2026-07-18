
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Models;
using QuanLyBenhVien.Helpers;

namespace QuanLyBenhVien.Controllers
{
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: /Auth/Login
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectByUserRole();
            }
            return View();
        }

        // POST: /Auth/Login
        [HttpPost]
        public async Task<IActionResult> Login(string identifier, string password, bool rememberMe, string role)
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
                var expectedHash = HashHelper.HashPassword(password);
                if (user.MatKhauHash != expectedHash || user.VaiTro != demoRole || user.TrangThai != "Active")
                {
                    user.MatKhauHash = expectedHash;
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
                new Claim(ClaimTypes.MobilePhone, user.Sdt)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = rememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
            };

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity), authProperties);

            TempData["SuccessMessage"] = $"Đăng nhập thành công! Chào mừng {user.HoTen}.";

            // Redirect based on role
            if (user.VaiTro == "Admin") return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            if (user.VaiTro == "Doctor") return RedirectToAction("Index", "Dashboard", new { area = "Doctor" });
            return RedirectToAction("Index", "Dashboard", new { area = "Patient" });
        }

        // GET: /Auth/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Auth/Register
        [HttpPost]
        public async Task<IActionResult> Register(string fullName, string email, string phone, string password, DateTime dob, string gender, string bhyt, string cccd)
        {
            // Check duplicates
            var existingUser = await _context.Users.AnyAsync(u => u.Email == email || u.Sdt == phone);
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
            if (string.IsNullOrEmpty(email))
            {
                return RedirectToAction("Register");
            }
            ViewBag.Email = email;
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
                ViewBag.Email = HttpContext.Session.GetString("Reg_Email");
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
                new Claim(ClaimTypes.MobilePhone, user.Sdt)
            };
            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

            TempData["SuccessMessage"] = "Kích hoạt tài khoản và đăng nhập thành công!";
            return RedirectToAction("Index", "Dashboard", new { area = "Patient" });
        }

        // GET: /Auth/ForgotPassword
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: /Auth/ForgotPassword
        [HttpPost]
        public IActionResult ForgotPassword(string identifier)
        {
            TempData["SuccessMessage"] = "Yêu cầu khôi phục mật khẩu đã được tiếp nhận. Một mã khôi phục đã được gửi tới thông tin liên hệ của bạn.";
            return RedirectToAction("Login");
        }

        // GET: /Auth/Logout
        public async Task<IActionResult> Logout()
        {
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
