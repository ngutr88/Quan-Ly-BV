using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;

namespace QuanLyBenhVien.Helpers;

/// <summary>
/// Shared unread-notification-count query for any role - Notification.NguoiDungId
/// is a generic User FK, so this single query backs Doctor/Notification/UnreadCount
/// (bell badge polling), DoctorSidebarViewComponent, and PatientSidebarViewComponent
/// alike, keeping every badge in sync with the exact same definition of "unread".
/// </summary>
public static class NotificationCountHelper
{
    public static Task<int> GetUnreadCountAsync(ApplicationDbContext context, int userId) =>
        context.Notifications.CountAsync(n => n.NguoiDungId == userId && !n.DaDoc);
}
