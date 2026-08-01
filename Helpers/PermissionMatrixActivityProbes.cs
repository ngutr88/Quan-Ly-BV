using System;
using System.Linq;
using QuanLyBenhVien.Data;

namespace QuanLyBenhVien.Helpers;

/// <summary>
/// Extensible "is this module actively in use right now" registry for the
/// permission-matrix confirmation screen. Only a handful of high-risk modules
/// have a real business-data probe wired up (deliberately scoped, not all
/// ~39 modules - agreed with the user); everything else falls back to the
/// generic "N tài khoản đang giữ vai trò này" warning the caller already
/// computes from account counts. Add a new case here whenever a module gets
/// a meaningful "turning this off mid-flight breaks something" story.
/// </summary>
public static class PermissionMatrixActivityProbes
{
    public static string? GetWarning(ApplicationDbContext context, string moduleKey) => moduleKey switch
    {
        "Doctor.Chat" => WarnIf(
            context.Conversations.Count(c => c.TrangThai != "DaDong"),
            n => $"{n} hội thoại tư vấn đang mở sẽ không ai xử lý."),

        "Doctor.Queue" => WarnIf(
            context.Appointments.Count(a => a.ThoiGian.Date == DateTime.Today && a.TrangThai != "HoanThanh" && a.TrangThai != "DaHuy"),
            n => $"{n} ca khám hôm nay chưa xử lý xong."),

        "Admin.Medicines" => WarnIf(
            context.GoodsReceipts.Count(g => g.TrangThai == "ChoDuyet"),
            n => $"{n} phiếu nhập kho đang chờ duyệt."),

        "Admin.LabOrders" => WarnIf(
            context.LabOrderItems.Count(i => i.TrangThai == "ChoThucHien" || i.TrangThai == "DangThucHien"),
            n => $"{n} chỉ định cận lâm sàng đang chờ/đang thực hiện."),

        _ => null
    };

    private static string? WarnIf(int count, Func<int, string> message) => count > 0 ? message(count) : null;
}
