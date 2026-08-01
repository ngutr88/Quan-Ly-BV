using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using QuanLyBenhVien.Hubs;

namespace QuanLyBenhVien.Services
{
    /// <summary>
    /// Thin wrapper around <see cref="IHubContext{DoctorDashboardHub}"/> so
    /// controllers don't depend on SignalR directly. Every notify call sends a
    /// bare named signal - no payload - the client re-fetches through the
    /// matching AJAX endpoint (see DashboardController.QueueSection /
    /// ActionRequiredSection / Doctor NotificationController.UnreadCount).
    /// </summary>
    public class DoctorDashboardNotifier
    {
        private readonly IHubContext<DoctorDashboardHub> _hub;

        public DoctorDashboardNotifier(IHubContext<DoctorDashboardHub> hub)
        {
            _hub = hub;
        }

        public Task NotifyQueueUpdatedAsync(int? doctorId)
        {
            if (!doctorId.HasValue) return Task.CompletedTask;
            return _hub.Clients.Group(DoctorDashboardHub.GroupName(doctorId.Value)).SendAsync("QueueUpdated");
        }

        public Task NotifyActionRequiredUpdatedAsync(int? doctorId)
        {
            if (!doctorId.HasValue) return Task.CompletedTask;
            return _hub.Clients.Group(DoctorDashboardHub.GroupName(doctorId.Value)).SendAsync("ActionRequiredUpdated");
        }

        public Task NotifyNotificationCountChangedAsync(int? doctorId)
        {
            if (!doctorId.HasValue) return Task.CompletedTask;
            return _hub.Clients.Group(DoctorDashboardHub.GroupName(doctorId.Value)).SendAsync("NotificationCountChanged");
        }

        public Task NotifyLabResultsUpdatedAsync(int? doctorId)
        {
            if (!doctorId.HasValue) return Task.CompletedTask;
            return _hub.Clients.Group(DoctorDashboardHub.GroupName(doctorId.Value)).SendAsync("LabResultsUpdated");
        }

        // Chat có hub riêng (ConsultationChatHub) cho cửa sổ chat đang mở, vì
        // nó cần payload thật + nhóm theo hội thoại. Signal này chỉ phục vụ
        // badge/danh sách hội thoại phía bác sĩ khi KHÔNG có cửa sổ chat nào
        // đang mở - tái dùng đúng convention "signal rồi refetch" sẵn có ở đây.
        public Task NotifyChatUpdatedAsync(int? doctorId)
        {
            if (!doctorId.HasValue) return Task.CompletedTask;
            return _hub.Clients.Group(DoctorDashboardHub.GroupName(doctorId.Value)).SendAsync("ChatUpdated");
        }
    }
}
