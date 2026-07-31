using System.Collections.Generic;

namespace QuanLyBenhVien.Models.ViewModels.PrescriptionSafety
{
    public enum SafetyTier
    {
        HardBlock,
        MustConfirm,
        SoftNudge
    }

    // Dữ kiện lâm sàng thuần túy - KHÔNG biết ai đang gọi (kê đơn ngoại trú,
    // mẫu đơn, hay sau này y lệnh nội trú), để không phải sửa interface khi
    // thêm nơi gọi mới.
    public class PrescriptionSafetyContext
    {
        public int PatientId { get; set; }

        // Cân nặng ghi nhận ở lượt khám hiện tại - dùng cho ngưỡng liều theo
        // cân nặng bệnh nhi. Không lấy từ Patient (không có trường cân nặng
        // ổn định lâu dài trên đó), phải do nơi gọi truyền vào.
        public decimal? CanNangKg { get; set; }

        public List<CandidateDrugLine> CandidateLines { get; set; } = new();

        // Loại trừ chính đơn đang xét khi kiểm tra tương tác với "các đơn thuốc
        // đang hiệu lực khác" - dùng khi sửa/kê lại một đơn đã tồn tại.
        public int? ExcludePrescriptionId { get; set; }
    }

    public class CandidateDrugLine
    {
        public int MedicineId { get; set; }
        public decimal? LieuMoiLan { get; set; }
        public int? SoLanMoiNgay { get; set; }
        public int? SoNgayDung { get; set; }
    }

    public class PrescriptionSafetyWarning
    {
        public SafetyTier Tier { get; set; }

        // "DiUngTrucTiep","DiUngCheo","TuongTacChongChiDinh","TuongTacNghiemTrong",
        // "TuongTacTrungBinh","TuongTacNhe","TrungHoatChat","VuotLieuToiDa","VuotLieuTheoCan"
        public string Category { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public int? RelatedMedicineIdA { get; set; }
        public int? RelatedMedicineIdB { get; set; }

        // true cho HardBlock/MustConfirm - bác sĩ phải xác nhận + (với HardBlock)
        // nhập lý do mới lưu được; false cho SoftNudge (không chặn).
        public bool RequiresAcknowledgement { get; set; }
    }

    public class PrescriptionSafetyResult
    {
        public List<PrescriptionSafetyWarning> Warnings { get; set; } = new();

        public bool HasHardBlock => Warnings.Exists(w => w.Tier == SafetyTier.HardBlock);
    }
}
