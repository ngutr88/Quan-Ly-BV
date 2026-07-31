using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;

namespace QuanLyBenhVien.Hubs
{
    /// <summary>
    /// Push-only hub for the Doctor dashboard: the server notifies a doctor's
    /// group that something changed ("QueueUpdated", "ActionRequiredUpdated",
    /// "NotificationCountChanged") and the client re-fetches the relevant
    /// section through its normal AJAX endpoint. Clients never call into this
    /// hub - keeping exactly one rendering path (the existing MVC actions)
    /// instead of a second, hub-only serialization path that could drift.
    /// </summary>
    [Authorize(Roles = "Doctor")]
    public class DoctorDashboardHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public DoctorDashboardHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public static string GroupName(int doctorId) => $"doctor-{doctorId}";

        public override async Task OnConnectedAsync()
        {
            var doctorId = await ResolveDoctorIdAsync();
            if (doctorId.HasValue)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(doctorId.Value));
            }

            await base.OnConnectedAsync();
        }

        private async Task<int?> ResolveDoctorIdAsync()
        {
            var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || !int.TryParse(claim.Value, out var userId)) return null;

            return await _context.Doctors
                .Where(d => d.NguoiDungId == userId)
                .Select(d => (int?)d.Id)
                .FirstOrDefaultAsync();
        }
    }
}
