using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBenhVien.Models
{
    [Table("HoaDon")]
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PhieuKhamId { get; set; }

        [ForeignKey("PhieuKhamId")]
        public virtual ExaminationRecord ExaminationRecord { get; set; } = null!;

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TongTien { get; set; }

        [Required]
        [StringLength(50)]
        public string TrangThaiThanhToan { get; set; } = "ChuaThanhToan"; // ChuaThanhToan, DaThanhToan, DaHuy

        // Nullable: hóa đơn chưa thanh toán thì chưa có phương thức được chọn.
        // NULL nghĩa là "chưa chọn phương thức", không dùng chuỗi "ChuaThanhToan"
        // giả làm phương thức (đó là một TrangThaiThanhToan, không phải PhuongThuc).
        // Default PHẢI là null, không phải "TienMat" - trước đây default sai đã
        // khiến hóa đơn CHƯA trả hiện nhầm "Tiền mặt" (ExamController.CompleteSession
        // dựa vào default này khi tạo hóa đơn mới).
        [StringLength(50)]
        public string? PhuongThuc { get; set; } // TienMat, ChuyenKhoan, Online (MoMo), Online (VNPay), Online (ZaloPay), Online

        [StringLength(100)]
        public string? MaGiaoDich { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public DateTime? NgayThanhToan { get; set; }

        // Giao dịch (PaymentTransaction) đang/vừa xử lý hóa đơn này - null nghĩa
        // là chưa từng có giao dịch nào được khởi tạo. Chỉ giữ giao dịch GẦN
        // NHẤT (không phải lịch sử đầy đủ mọi lần thử) - lịch sử chi tiết từng
        // lần thử vẫn truy vết được qua NhatKyHeThong.
        public int? GiaoDichThanhToanHienTaiId { get; set; }

        [ForeignKey("GiaoDichThanhToanHienTaiId")]
        public virtual PaymentTransaction? GiaoDichThanhToanHienTai { get; set; }

        public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();
    }

    // Một hàng cho mỗi LẦN bấm thanh toán (có thể gộp nhiều hóa đơn cùng lúc) -
    // tách khỏi Invoice để: (1) 1 giao dịch phủ được nhiều hóa đơn, (2) có một
    // "nguồn chân lý" duy nhất cho trạng thái do webhook/IPN cập nhật, độc lập
    // với redirect trình duyệt (PaymentController.Webhook/PaymentReturn).
    [Table("GiaoDichThanhToan")]
    public class PaymentTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int NguoiKhoiTaoId { get; set; }

        [ForeignKey("NguoiKhoiTaoId")]
        public virtual User NguoiKhoiTao { get; set; } = null!;

        [Required]
        [StringLength(64)]
        public string IdempotencyKey { get; set; } = string.Empty;

        // Luôn tính lại ở server lúc tạo giao dịch (tổng TongTien của các hóa
        // đơn được chọn) - KHÔNG BAO GIỜ nhận số tiền này từ client.
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SoTien { get; set; }

        [Required]
        [StringLength(50)]
        public string PhuongThuc { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string TrangThai { get; set; } = "ChoXuLy"; // ChoXuLy, DangXuLy, ThanhCong, ThatBai, DaHuy

        [StringLength(100)]
        public string? MaGiaoDichCong { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public DateTime? NgayCapNhat { get; set; }

        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }

    [Table("ChiTietHoaDon")]
    public class InvoiceDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int HoaDonId { get; set; }

        [ForeignKey("HoaDonId")]
        public virtual Invoice Invoice { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string LoaiPhi { get; set; } = string.Empty; // e.g. "PhiKham", "PhiThuoc", "PhiDichVu"

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal SoTien { get; set; }
    }
}
