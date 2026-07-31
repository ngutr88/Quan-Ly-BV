using System.Collections.Generic;

namespace QuanLyBenhVien.Models.ViewModels
{
    // Dòng thuốc bác sĩ gửi lên khi hoàn tất kê đơn (thay hẳn class nội bộ
    // TempPrescriptionItem trước đây - chỉ có MedicineId/Qty/Instructions).
    // SoLuong KHÔNG được nhận từ client - server tự tính từ LieuMoiLan x
    // SoLanMoiNgay x SoNgayDung, làm tròn theo QuyCachSoLuong nếu có.
    public class PrescriptionLineSubmission
    {
        public int MedicineId { get; set; }
        public decimal LieuMoiLan { get; set; }
        public int SoLanMoiNgay { get; set; }
        public string DuongDung { get; set; } = string.Empty;
        public string? ThoiDiemDung { get; set; }
        public int SoNgayDung { get; set; }
        public string? HuongDanSuDungOverride { get; set; }
    }

    // Gói gửi lên khi hoàn tất kê đơn - cảnh báo an toàn được chạy trên TOÀN
    // BỘ danh sách thuốc cùng lúc (một tương tác/trùng hoạt chất liên quan đến
    // 2 dòng khác nhau, không thể quy về đúng 1 dòng riêng lẻ), nên việc xác
    // nhận/lý do ghi đè cũng ở cấp toàn bộ đơn, không phải theo từng dòng.
    public class PrescriptionSubmissionPayload
    {
        public List<PrescriptionLineSubmission> Lines { get; set; } = new();

        // Danh mục Category (PrescriptionSafetyWarning.Category) mà bác sĩ đã
        // xác nhận - server chạy lại toàn bộ kiểm tra an toàn và từ chối (400)
        // nếu thiếu xác nhận cho một cảnh báo vẫn còn phát hiện được.
        public List<string> AcknowledgedCategories { get; set; } = new();

        // Bắt buộc khi có ít nhất 1 cảnh báo HardBlock/MustConfirm bị ghi đè.
        public string? OverrideReason { get; set; }
    }
}
