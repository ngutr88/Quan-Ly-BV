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
    /// Realtime transport for an OPEN consultation chat window. Unlike
    /// DoctorDashboardHub (push-only, doctor-only, groups by doctor), this hub
    /// groups by conversation so both sides can join the same room, and it
    /// accepts client-initiated calls (typing, mark-as-read) because a chat
    /// window genuinely needs both directions. Message CONTENT and attachments
    /// never travel through this hub - they always go through the ordinary
    /// SendMessage MVC action (antiforgery + file validation), so there is
    /// exactly one place that writes business data. MarkAsRead is the one
    /// deliberate exception: flipping a "seen" flag is a very high-frequency,
    /// non-business-content write, so round-tripping it through a full HTTP
    /// action would be wasteful.
    /// </summary>
    [Authorize(Roles = "Doctor,Patient")]
    public class ConsultationChatHub : Hub
    {
        private readonly ApplicationDbContext _context;

        public ConsultationChatHub(ApplicationDbContext context)
        {
            _context = context;
        }

        public static string ConversationGroupName(int conversationId) => $"conversation-{conversationId}";

        public async Task JoinConversation(int conversationId)
        {
            if (!await CurrentUserBelongsToConversationAsync(conversationId)) return;
            await Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroupName(conversationId));
        }

        public async Task LeaveConversation(int conversationId)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, ConversationGroupName(conversationId));
        }

        // Ephemeral - không ghi DB, chỉ relay cho phía còn lại trong phòng.
        public async Task NotifyTyping(int conversationId)
        {
            if (!await CurrentUserBelongsToConversationAsync(conversationId)) return;
            await Clients.OthersInGroup(ConversationGroupName(conversationId)).SendAsync("TypingReceived", conversationId);
        }

        public async Task MarkAsRead(int conversationId)
        {
            var (belongs, role) = await ResolveMembershipAsync(conversationId);
            if (!belongs) return;

            // Đánh dấu đã xem các tin của PHÍA CÒN LẠI (bác sĩ mở thread thì
            // đánh dấu tin bệnh nhân là đã xem, và ngược lại).
            var otherRole = role == "Doctor" ? "Patient" : "Doctor";
            var unseen = await _context.ConversationMessages
                .Where(m => m.HoiThoaiId == conversationId && m.VaiTroNguoiGui == otherRole && !m.DaXemBoiNguoiNhan)
                .ToListAsync();

            if (unseen.Count == 0) return;

            var now = System.DateTime.Now;
            foreach (var m in unseen)
            {
                m.DaXemBoiNguoiNhan = true;
                m.NgayXem = now;
            }
            await _context.SaveChangesAsync();

            await Clients.Group(ConversationGroupName(conversationId)).SendAsync("MessagesRead", conversationId);
        }

        private async Task<(bool Belongs, string Role)> ResolveMembershipAsync(int conversationId)
        {
            var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null || !int.TryParse(claim.Value, out var userId)) return (false, string.Empty);

            var conversation = await _context.Conversations
                .Where(c => c.Id == conversationId)
                .Select(c => new { c.Doctor.NguoiDungId, PatientUserId = c.Patient.NguoiDungId })
                .FirstOrDefaultAsync();
            if (conversation == null) return (false, string.Empty);

            if (conversation.NguoiDungId == userId) return (true, "Doctor");
            if (conversation.PatientUserId == userId) return (true, "Patient");
            return (false, string.Empty);
        }

        private async Task<bool> CurrentUserBelongsToConversationAsync(int conversationId)
        {
            var (belongs, _) = await ResolveMembershipAsync(conversationId);
            return belongs;
        }
    }
}
