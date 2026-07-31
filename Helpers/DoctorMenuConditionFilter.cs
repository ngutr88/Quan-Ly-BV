using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;

namespace QuanLyBenhVien.Helpers;

/// <summary>
/// Route guard for the account-level conditions in DoctorMenuConditions
/// (ward assignment, surgical specialty) - the counterpart to
/// ModulePermissionFilter's role-level check. Applied per-controller via
/// [TypeFilter(typeof(DoctorMenuConditionFilter), Arguments = new object[] { conditionKey })]
/// so a menu item hidden by a condition can never be reached by direct URL either,
/// since both read the exact same DoctorMenuConditions.ByKey[conditionKey] predicate.
/// </summary>
public class DoctorMenuConditionFilter : IAsyncActionFilter
{
    private readonly ApplicationDbContext _context;
    private readonly string _conditionKey;

    public DoctorMenuConditionFilter(ApplicationDbContext context, string conditionKey)
    {
        _context = context;
        _conditionKey = conditionKey;
    }

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var claim = context.HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
        var userId = claim != null && int.TryParse(claim.Value, out var id) ? id : 0;

        var doctor = userId > 0
            ? await _context.Doctors.FirstOrDefaultAsync(d => d.NguoiDungId == userId)
            : null;

        var predicate = DoctorMenuConditions.ByKey[_conditionKey];

        if (doctor == null || !predicate(doctor))
        {
            context.Result = new RedirectToActionResult("AccessDenied", "Auth", new { area = "" });
            return;
        }

        await next();
    }
}
