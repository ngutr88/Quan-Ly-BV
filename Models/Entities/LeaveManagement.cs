using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBenhVien.Models
{
    // Một lượt bác sĩ đăng ký nghỉ (phép năm/ốm/việc riêng/khác), chờ Admin
    // hoặc bác sĩ Trưởng khoa cùng KhoaId duyệt. SoNgayTru là số ngày được
    // TÍNH TẠI THỜI ĐIỂM GỬI (server), dùng để tạm giữ/trừ LeaveBalance -
    // không tính lại tại thời điểm duyệt để tránh lệch nếu chính sách đổi
    // giữa lúc gửi và lúc duyệt.
    [Table("YeuCauNghiPhep")]
    public class LeaveRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BacSiId { get; set; }

        [ForeignKey("BacSiId")]
        public virtual Doctor Doctor { get; set; } = null!;

        [Required]
        public DateTime TuNgay { get; set; }

        [Required]
        public DateTime DenNgay { get; set; }

        // null = nghỉ cả ngày; "Sang"/"Chieu" chỉ hợp lệ khi TuNgay == DenNgay
        [StringLength(10)]
        public string? Buoi { get; set; }

        [Column(TypeName = "decimal(5,1)")]
        public decimal SoNgayTru { get; set; }

        // PhepNam, Om, ViecRieng, Khac
        [Required]
        [StringLength(20)]
        public string LoaiNghi { get; set; } = "PhepNam";

        [Required]
        [StringLength(1000)]
        public string LyDo { get; set; } = string.Empty;

        // /uploads/leave-attachments/... - chỉ dùng cho LoaiNghi == "Om" (tuỳ chọn)
        [StringLength(300)]
        public string? DinhKemUrl { get; set; }

        // ChoDuyet, DaDuyet, TuChoi
        [Required]
        [StringLength(20)]
        public string TrangThai { get; set; } = "ChoDuyet";

        public int? NguoiDuyetId { get; set; }

        [ForeignKey("NguoiDuyetId")]
        public virtual User? NguoiDuyet { get; set; }

        public DateTime? NgayDuyet { get; set; }

        [StringLength(500)]
        public string? LyDoTuChoi { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;
    }

    // Số dư phép năm của 1 bác sĩ cho 1 năm dương lịch cụ thể - tạo lười
    // (lazy) khi lần đầu được truy cập trong năm đó (xem
    // LeaveRequestService.GetOrCreateBalanceAsync), không seed sẵn hàng loạt.
    [Table("SoDuPhepNam")]
    public class LeaveBalance
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BacSiId { get; set; }

        [ForeignKey("BacSiId")]
        public virtual Doctor Doctor { get; set; } = null!;

        [Required]
        public int Nam { get; set; }

        // Snapshot quota (12 + 1/5 năm kinh nghiệm) tính tại thời điểm tạo dòng
        [Column(TypeName = "decimal(5,1)")]
        public decimal TongSoNgay { get; set; }

        // Cộng dồn từ số dư còn lại năm trước, giới hạn trần 5 ngày
        [Column(TypeName = "decimal(5,1)")]
        public decimal CongDonTuNamTruoc { get; set; }

        [Column(TypeName = "decimal(5,1)")]
        public decimal DaDung { get; set; }

        // Tổng SoNgayTru của các LeaveRequest đang ChoDuyet (giữ chỗ, chưa trừ thật)
        [Column(TypeName = "decimal(5,1)")]
        public decimal DaTamGiu { get; set; }

        public DateTime NgayCapNhat { get; set; } = DateTime.Now;
    }
}
