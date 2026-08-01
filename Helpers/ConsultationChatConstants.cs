namespace QuanLyBenhVien.Helpers;

/// <summary>
/// Static text for the consultation-chat channel. Giai đoạn 1 chỉ có mẫu câu
/// trả lời nhanh tĩnh (hard-code, không lưu DB, không có UI quản lý) - CRUD
/// mẫu cá nhân hoá + gõ "/" để tìm mẫu là phạm vi Giai đoạn 2.
/// </summary>
public static class ConsultationChatConstants
{
    public static readonly string[] DoctorQuickReplies =
    {
        "Anh/chị vui lòng đặt lịch tái khám để bác sĩ đánh giá trực tiếp nhé.",
        "Kết quả xét nghiệm của anh/chị trong giới hạn bình thường.",
        "Anh/chị tiếp tục dùng thuốc theo đúng đơn đã kê, uống đủ liều và đúng giờ nhé.",
        "Cảm ơn anh/chị đã trao đổi. Bác sĩ xin phép đóng trao đổi này, có gì cần thêm anh/chị nhắn lại nhé."
    };

    // Nguyên văn theo yêu cầu nghiệp vụ gốc - không tự đổi số 115 hay diễn đạt.
    public const string AutoReplyOffHoursText =
        "Phòng khám đã nhận tin nhắn. Chúng tôi sẽ phản hồi trong giờ làm việc. " +
        "Nếu khẩn cấp, vui lòng gọi 115 hoặc đến cơ sở y tế gần nhất.";

    // Không nhúng URL vào chuỗi này - Loai="MoiDatLich" khiến client render
    // riêng một nút/link "Đặt lịch khám ngay" trỏ đúng bác sĩ/khoa bên dưới
    // tin nhắn, thay vì đặt link thô trong văn bản (link thô trong text sẽ
    // không bấm được vì noiDung luôn được escape khi hiển thị).
    public const string InviteBookingMessageText =
        "Để được thăm khám trực tiếp, bác sĩ mời anh/chị đặt lịch khám. Bấm vào nút bên dưới để đặt lịch nhé.";
}
