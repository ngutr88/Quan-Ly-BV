using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using QuanLyBenhVien.Hubs;

namespace QuanLyBenhVien.Services
{
    /// <summary>
    /// Thin wrapper around <see cref="IHubContext{ConsultationChatHub}"/>.
    /// Unlike DoctorDashboardNotifier's bare signals, these carry a real
    /// payload - a chat window that's actually open needs the message itself,
    /// not just "something changed, go refetch".
    /// </summary>
    public class ConsultationChatNotifier
    {
        private readonly IHubContext<ConsultationChatHub> _hub;

        public ConsultationChatNotifier(IHubContext<ConsultationChatHub> hub)
        {
            _hub = hub;
        }

        public Task NotifyMessageReceivedAsync(int conversationId, object messageDto) =>
            _hub.Clients.Group(ConsultationChatHub.ConversationGroupName(conversationId))
                .SendAsync("MessageReceived", messageDto);

        public Task NotifyConversationStatusChangedAsync(int conversationId, string trangThai) =>
            _hub.Clients.Group(ConsultationChatHub.ConversationGroupName(conversationId))
                .SendAsync("ConversationStatusChanged", new { conversationId, trangThai });
    }
}
