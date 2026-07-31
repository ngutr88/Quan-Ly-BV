using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Helpers;

namespace QuanLyBenhVien.Areas.Doctor.ViewComponents
{
    /// <summary>
    /// Builds the grouped, permission-filtered Doctor sidebar menu. Applies the
    /// exact same role-permission fail-open rule as ModulePermissionFilter (so
    /// the sidebar never shows an item a direct URL would block) plus the
    /// account-level DoctorMenuConditions (fail-closed) for Inpatient/Surgery.
    /// </summary>
    public class DoctorSidebarViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public DoctorSidebarViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var claim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            var userId = claim != null && int.TryParse(claim.Value, out var id) ? id : 0;

            var doctor = userId > 0
                ? await _context.Doctors.FirstOrDefaultAsync(d => d.NguoiDungId == userId)
                : null;

            var permissions = await _context.RolePermissions
                .AsNoTracking()
                .Where(p => p.VaiTro == "Doctor")
                .ToDictionaryAsync(p => p.ModuleKey, p => p.DuocPhep);

            int? unreadNotifications = null;
            if (userId > 0)
            {
                var count = await NotificationCountHelper.GetUnreadCountAsync(_context, userId);
                if (count > 0) unreadNotifications = count;
            }

            var labelByKey = ModulePermissionRegistry.DoctorModules.ToDictionary(m => m.Key, m => m.Label);

            var groups = new List<DoctorSidebarGroupViewModel>();
            foreach (var (groupKey, groupLabel) in DoctorMenuGroups.Ordered)
            {
                var items = new List<DoctorSidebarMenuItemViewModel>();

                foreach (var item in DoctorMenuRegistry.Items.Where(i => i.Group == groupKey))
                {
                    // Same fail-open rule as ModulePermissionFilter: a missing
                    // row means allowed, only an explicit DuocPhep=false hides it.
                    var allowed = ModulePermissionRegistry.AlwaysAllowed.Contains(item.ModuleKey)
                        || !permissions.TryGetValue(item.ModuleKey, out var duocPhep)
                        || duocPhep;
                    if (!allowed) continue;

                    if (item.ExtraVisibilityKey != null)
                    {
                        // Fail-closed here, unlike role permissions above: no
                        // doctor profile or a false predicate hides the item.
                        if (doctor == null || !DoctorMenuConditions.ByKey[item.ExtraVisibilityKey](doctor)) continue;
                    }

                    var badge = item.BadgeSourceKey == DoctorMenuRegistry.BadgeUnreadNotifications ? unreadNotifications : null;

                    items.Add(new DoctorSidebarMenuItemViewModel
                    {
                        Label = labelByKey.TryGetValue(item.ModuleKey, out var label) ? label : item.ModuleKey,
                        Icon = item.Icon,
                        Route = item.Route,
                        BadgeCount = badge
                    });
                }

                if (items.Count > 0)
                {
                    groups.Add(new DoctorSidebarGroupViewModel { Label = groupLabel, Items = items });
                }
            }

            return View(groups);
        }
    }

    public class DoctorSidebarGroupViewModel
    {
        public string Label { get; set; } = string.Empty;
        public List<DoctorSidebarMenuItemViewModel> Items { get; set; } = new();
    }

    public class DoctorSidebarMenuItemViewModel
    {
        public string Label { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public int? BadgeCount { get; set; }
    }
}
