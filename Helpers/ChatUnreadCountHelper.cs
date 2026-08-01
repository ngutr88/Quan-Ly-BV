using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;

namespace QuanLyBenhVien.Helpers;

/// <summary>
/// Shared "unread" count for the consultation-chat inbox on both sides -
/// counts CONVERSATIONS with at least one message from the other party not
/// yet seen, matching what the sidebar badge and the list screen both need.
/// Backs DoctorSidebarViewComponent/PatientSidebarViewComponent and each
/// side's ChatController.UnreadCount polling endpoint.
/// </summary>
public static class ChatUnreadCountHelper
{
    public static Task<int> GetUnreadCountForDoctorAsync(ApplicationDbContext context, int doctorId) =>
        context.Conversations.CountAsync(conv =>
            conv.BacSiId == doctorId &&
            conv.TinNhan.Any(m => m.VaiTroNguoiGui != "Doctor" && !m.DaXemBoiNguoiNhan));

    public static Task<int> GetUnreadCountForPatientAsync(ApplicationDbContext context, int patientId) =>
        context.Conversations.CountAsync(conv =>
            conv.BenhNhanId == patientId &&
            conv.TinNhan.Any(m => m.VaiTroNguoiGui == "Doctor" && !m.DaXemBoiNguoiNhan));
}
