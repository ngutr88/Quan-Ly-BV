using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;

namespace QuanLyBenhVien.Helpers;

// Chặn mọi request đã đăng nhập về trang bắt đổi mật khẩu khi tài khoản còn
// cờ PhaiDoiMatKhau (mật khẩu tạm do Admin cấp qua StaffController.IssueTempPassword).
public class ForcePasswordChangeFilter : IAsyncActionFilter
{
    private static readonly (string Controller, string Action)[] Exempt =
    {
        ("Auth", "ForcedPasswordChange"),
        ("Auth", "Logout")
    };

    private readonly ApplicationDbContext _context;

    public ForcePasswordChangeFilter(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated == true)
        {
            var controller = context.RouteData.Values["controller"] as string ?? string.Empty;
            var action = context.RouteData.Values["action"] as string ?? string.Empty;
            var isExempt = Exempt.Any(e =>
                e.Controller.Equals(controller, StringComparison.OrdinalIgnoreCase) &&
                e.Action.Equals(action, StringComparison.OrdinalIgnoreCase));

            if (!isExempt)
            {
                var idClaim = user.FindFirst(ClaimTypes.NameIdentifier);
                if (idClaim != null && int.TryParse(idClaim.Value, out var userId))
                {
                    var mustChange = await _context.Users.AsNoTracking()
                        .Where(u => u.Id == userId)
                        .Select(u => u.PhaiDoiMatKhau)
                        .FirstOrDefaultAsync();

                    if (mustChange)
                    {
                        context.Result = new RedirectToActionResult("ForcedPasswordChange", "Auth", new { area = "" });
                        return;
                    }
                }
            }
        }

        await next();
    }
}
