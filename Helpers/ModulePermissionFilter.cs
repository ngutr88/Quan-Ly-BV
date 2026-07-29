using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;

namespace QuanLyBenhVien.Helpers;

/// <summary>
/// Runs after the existing [Authorize(Roles = "...")] gate on every area
/// controller and applies one more, admin-editable on/off switch per
/// (role, module) from the `PhanQuyenVaiTro` table. It can only narrow access
/// further within a role's own area — it never overrides the area-level
/// [Authorize] check, so it cannot let a Doctor into /Admin or a Patient into
/// /Doctor. A missing row means "allowed" (fail open on this extra layer only,
/// since the real security boundary is the area gate that already ran).
/// </summary>
public class ModulePermissionFilter : IAsyncActionFilter
{
    private readonly ApplicationDbContext _context;

    public ModulePermissionFilter(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var area = context.RouteData.Values["area"] as string;
        var controller = context.RouteData.Values["controller"] as string;
        var user = context.HttpContext.User;

        if (!string.IsNullOrEmpty(area) && !string.IsNullOrEmpty(controller) && user.Identity?.IsAuthenticated == true)
        {
            var moduleKey = $"{area}.{controller}";

            if (!ModulePermissionRegistry.AlwaysAllowed.Contains(moduleKey))
            {
                var role = user.FindFirst(ClaimTypes.Role)?.Value;
                if (!string.IsNullOrEmpty(role))
                {
                    var permission = await _context.RolePermissions
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.VaiTro == role && p.ModuleKey == moduleKey);

                    if (permission != null && !permission.DuocPhep)
                    {
                        context.Result = new RedirectToActionResult("AccessDenied", "Auth", new { area = "" });
                        return;
                    }
                }
            }
        }

        await next();
    }
}
