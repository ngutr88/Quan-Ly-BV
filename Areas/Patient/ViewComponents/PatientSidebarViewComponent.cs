using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;
using QuanLyBenhVien.Helpers;

namespace QuanLyBenhVien.Areas.Patient.ViewComponents
{
    /// <summary>
    /// Builds the grouped, permission-filtered Patient sidebar menu - mirrors
    /// DoctorSidebarViewComponent's fail-open RolePermission rule so the
    /// sidebar never shows an item a direct URL would block.
    /// </summary>
    public class PatientSidebarViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public PatientSidebarViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var claim = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier);
            var userId = claim != null && int.TryParse(claim.Value, out var id) ? id : 0;

            var patient = userId > 0
                ? await _context.Patients.FirstOrDefaultAsync(p => p.NguoiDungId == userId)
                : null;

            var permissions = await _context.RolePermissions
                .AsNoTracking()
                .Where(p => p.VaiTro == "Patient")
                .ToDictionaryAsync(p => p.ModuleKey, p => p.DuocPhep);

            int? unpaidInvoiceCount = null;
            int? unreadNotificationCount = null;
            if (patient != null)
            {
                var unpaid = await _context.Invoices
                    .CountAsync(i => i.ExaminationRecord.Appointment.BenhNhanId == patient.Id && i.TrangThaiThanhToan == "ChuaThanhToan");
                if (unpaid > 0) unpaidInvoiceCount = unpaid;

                var unread = await NotificationCountHelper.GetUnreadCountAsync(_context, userId);
                if (unread > 0) unreadNotificationCount = unread;
            }

            var groups = new List<PatientSidebarGroupViewModel>();
            foreach (var (groupKey, groupLabel) in PatientMenuGroups.Ordered)
            {
                var items = new List<PatientSidebarMenuItemViewModel>();

                foreach (var item in PatientMenuRegistry.Items.Where(i => i.Group == groupKey))
                {
                    // Same fail-open rule as ModulePermissionFilter: a missing
                    // row means allowed, only an explicit DuocPhep=false hides it.
                    var allowed = ModulePermissionRegistry.AlwaysAllowed.Contains(item.ModuleKey)
                        || !permissions.TryGetValue(item.ModuleKey, out var duocPhep)
                        || duocPhep;
                    if (!allowed) continue;

                    int? badge = item.BadgeSourceKey switch
                    {
                        PatientMenuRegistry.BadgeUnpaidInvoices => unpaidInvoiceCount,
                        PatientMenuRegistry.BadgeUnreadNotifications => unreadNotificationCount,
                        _ => null
                    };

                    items.Add(new PatientSidebarMenuItemViewModel
                    {
                        Label = item.Label,
                        Icon = item.Icon,
                        Route = item.Route,
                        BadgeCount = badge
                    });
                }

                if (items.Count > 0)
                {
                    groups.Add(new PatientSidebarGroupViewModel { Label = groupLabel, Items = items });
                }
            }

            return View(groups);
        }
    }

    public class PatientSidebarGroupViewModel
    {
        public string Label { get; set; } = string.Empty;
        public List<PatientSidebarMenuItemViewModel> Items { get; set; } = new();
    }

    public class PatientSidebarMenuItemViewModel
    {
        public string Label { get; set; } = string.Empty;
        public string Icon { get; set; } = string.Empty;
        public string Route { get; set; } = string.Empty;
        public int? BadgeCount { get; set; }
    }
}
