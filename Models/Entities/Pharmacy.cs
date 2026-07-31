using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBenhVien.Models
{
    [Table("Thuoc")]
    public class Medicine
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string TenThuoc { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string HoatChat { get; set; } = string.Empty;

        // Hàm lượng hoạt chất (vd. "500mg"). Nullable: thuốc có từ trước khi
        // có tính năng phiếu nhập kho chưa khai báo giá trị này.
        [StringLength(50)]
        public string? HamLuong { get; set; }

        // Quy cách đóng gói (vd. "Hộp 10 vỉ x 10 viên"). Nullable cùng lý do trên.
        [StringLength(150)]
        public string? QuyCachDongGoi { get; set; }

        [Required]
        [StringLength(50)]
        public string DonViTinh { get; set; } = string.Empty; // e.g. "Viên", "Chai", "Vỉ"

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Gia { get; set; }

        [Required]
        public int TonKho { get; set; }

        [Required]
        public int NguongToiThieu { get; set; } // Ngưỡng tồn tối thiểu để cảnh báo

        // Chỉ để hiển thị trong kết quả tìm thuốc khi kê đơn - KHÔNG tính đồng
        // chi trả BHYT thật (đó là một tính năng tài chính riêng, lớn hơn nhiều).
        public bool CoBaoHiemYTe { get; set; } = false;

        // Ngưỡng an toàn cho công cụ kê đơn (Kê đơn & Y lệnh). Theo SỐ ĐƠN VỊ/NGÀY
        // (viên/ngày theo DonViTinh), KHÔNG phải mg/kg thật, vì HamLuong là text
        // tự do chưa chuẩn hóa số - nullable vì phần lớn thuốc hiện có chưa khai
        // báo giá trị này.
        public int? LieuToiDaMoiNgay { get; set; }

        [Column(TypeName = "decimal(10, 4)")]
        public decimal? LieuToiDaMoiNgayTheoKg { get; set; }

        // Bội số đóng gói nhỏ nhất để làm tròn số lượng kê lên (vd 10 cho "vỉ 10
        // viên") - không tái dùng QuyCachDongGoi vì đó là text tự do không parse
        // được đáng tin cậy.
        public int? QuyCachSoLuong { get; set; }

        public virtual ICollection<MedicineBatch> LoThuocs { get; set; } = new List<MedicineBatch>();
    }

    [Table("LoThuoc")]
    public class MedicineBatch
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ThuocId { get; set; }

        [ForeignKey("ThuocId")]
        public virtual Medicine Medicine { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string SoLo { get; set; } = string.Empty; // Batch/Lot number

        // Ngày nhập lô thuốc. Bắt buộc để ràng buộc CHECK (HanSuDung >= NgayNhap)
        // có thể kiểm tra được; mặc định ngày hiện tại cho lô mới.
        [Required]
        public DateTime NgayNhap { get; set; } = DateTime.Now;

        [Required]
        public DateTime HanSuDung { get; set; }

        [Required]
        public int SoLuongNhap { get; set; }

        [Required]
        public int SoLuongTon { get; set; }

        // Giá nhập (chưa VAT) của lô này. Nullable: các lô có từ trước khi có
        // tính năng phiếu nhập kho (mục "Nâng cấp ReceiveBatch") không có giá trị này.
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? GiaNhap { get; set; }

        public int? NhaCungCapId { get; set; }

        [ForeignKey("NhaCungCapId")]
        public virtual Supplier? NhaCungCap { get; set; }
    }
}
