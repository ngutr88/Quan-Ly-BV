using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBenhVien.Models
{
    // Một hàng cho mỗi lượt bác sĩ đề xuất thay đổi nhóm trường "hồ sơ hành
    // nghề" (học hàm, CCHN, họ tên, ngày sinh, chuyên khoa, khoa công tác,
    // chức vụ - xem ProfileController.ProposeProfileChange). Lưu diff dạng
    // JSON cho CẢ lượt đề xuất (không phải 1 hàng/trường) - khớp với "tạo Yêu
    // cầu thay đổi (diff cũ→mới)" số ít trong đặc tả gốc; DuLieuCuJson là ảnh
    // chụp giá trị tại thời điểm đề xuất, không phải giá trị "gốc" bất biến.
    [Table("YeuCauThayDoiHoSo")]
    public class ProfileChangeRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BacSiId { get; set; }

        [ForeignKey("BacSiId")]
        public virtual Doctor Doctor { get; set; } = null!;

        public DateTime NgayDeXuat { get; set; } = DateTime.Now;

        [Required]
        [StringLength(20)]
        public string TrangThai { get; set; } = "ChoDuyet"; // ChoDuyet, DaDuyet, TuChoi

        [Required]
        [StringLength(4000)]
        public string DuLieuCuJson { get; set; } = "{}";

        [Required]
        [StringLength(4000)]
        public string DuLieuMoiJson { get; set; } = "{}";

        public int? NguoiDuyetId { get; set; }

        [ForeignKey("NguoiDuyetId")]
        public virtual User? NguoiDuyet { get; set; }

        public DateTime? NgayDuyet { get; set; }

        [StringLength(500)]
        public string? LyDoTuChoi { get; set; }
    }
}
