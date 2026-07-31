using Microsoft.EntityFrameworkCore;
using QuanLyBenhVien.Data;

namespace QuanLyBenhVien.Helpers;

/// <summary>
/// Shared "chưa xem" count for a doctor's lab-results inbox - backs both the
/// sidebar badge (DoctorSidebarViewComponent) and the bell-style polling
/// endpoint (Doctor/LabResults/UnreadCount), keeping both in sync with the
/// exact same definition of "unread": result published (DaCoKetQua) but not
/// yet opened by the assigned doctor.
/// </summary>
public static class LabResultsCountHelper
{
    public static Task<int> GetUnreadCountAsync(ApplicationDbContext context, int doctorId) =>
        context.LabOrderItems.CountAsync(i =>
            i.TrangThai == "DaCoKetQua" &&
            i.PhieuChiDinh.BacSiChiDinhId == doctorId &&
            i.KetQua != null && !i.KetQua.DaXem);
}
