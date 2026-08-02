using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Services;

namespace QuanLyBenhVien.Helpers;

// Chặn mọi request đã đăng nhập của vai trò Doctor về trang thiết lập 2FA khi
// Admin đã bật HospitalSettings.BatBuoc2FABacSi và tài khoản đó chưa
// TotpBatDau - cùng khuôn ForcePasswordChangeFilter. Đăng ký SAU
// ForcePasswordChangeFilter trong Program.cs để đổi mật khẩu tạm luôn được
// xử lý trước 2FA.
public class ForceTwoFactorSetupFilter : IAsyncActionFilter
{
    private static readonly (string Area, string Controller)[] ExemptControllers =
    {
        ("", "Auth"),
        // Toàn bộ Doctor/Profile được miễn trừ, không chỉ action
        // TwoFactorSetup - trang Index (nơi có link/nút mở TwoFactorSetup)
        // và mọi action tự phục vụ khác trên cùng trang (đổi mật khẩu, tải
        // ảnh đại diện...) phải vẫn dùng được bình thường trong lúc bác sĩ
        // đang hoàn tất việc bật 2FA bắt buộc.
        ("Doctor", "Profile"),
    };

    private readonly ApplicationDbContext _context;
    private readonly HospitalSettingsProvider _settingsProvider;

    public ForceTwoFactorSetupFilter(ApplicationDbContext context, HospitalSettingsProvider settingsProvider)
    {
        _context = context;
        _settingsProvider = settingsProvider;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated == true && user.IsInRole("Doctor"))
        {
            var area = context.RouteData.Values["area"] as string ?? string.Empty;
            var controller = context.RouteData.Values["controller"] as string ?? string.Empty;
            var isExempt = ExemptControllers.Any(e =>
                e.Area.Equals(area, StringComparison.OrdinalIgnoreCase) &&
                e.Controller.Equals(controller, StringComparison.OrdinalIgnoreCase));

            if (!isExempt && _settingsProvider.Load().BatBuoc2FABacSi)
            {
                var idClaim = user.FindFirst(ClaimTypes.NameIdentifier);
                if (idClaim != null && int.TryParse(idClaim.Value, out var userId))
                {
                    var totpBatDau = await _context.Users.AsNoTracking()
                        .Where(u => u.Id == userId)
                        .Select(u => u.TotpBatDau)
                        .FirstOrDefaultAsync();

                    if (!totpBatDau)
                    {
                        context.Result = new RedirectToActionResult("TwoFactorSetup", "Profile", new { area = "Doctor" });
                        return;
                    }
                }
            }
        }

        await next();
    }
}
