using System.Collections.Generic;

namespace QuanLyBenhVien.Models.ViewModels.LabDiagnostics
{
    // Gói chỉ định CLS gửi lên khi hoàn tất phiên khám - bác sĩ có thể chọn
    // từng dịch vụ lẻ và/hoặc áp dụng một bộ chỉ định có sẵn (BoChiDinhCLSId
    // chỉ mang tính ghi nhận "tạo từ bộ nào", danh sách dịch vụ thực tế vẫn là
    // DichVuCLSIds - cho phép bác sĩ bớt/thêm dịch vụ trước khi lưu).
    public class LabOrderSubmissionPayload
    {
        public List<int> DichVuCLSIds { get; set; } = new();
        public int? BoChiDinhCLSId { get; set; }
        public string? GhiChu { get; set; }
    }
}
