using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBenhVien.Models
{
    [Table("NhaCungCap")]
    public class Supplier
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string TenNhaCungCap { get; set; } = string.Empty;

        [StringLength(20)]
        public string? MaSoThue { get; set; }

        [StringLength(300)]
        public string? DiaChi { get; set; }

        [StringLength(20)]
        public string? SoDienThoai { get; set; }

        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(100)]
        public string? NguoiLienHe { get; set; }

        public bool DangHoatDong { get; set; } = true;

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public virtual ICollection<GoodsReceipt> PhieuNhaps { get; set; } = new List<GoodsReceipt>();
    }

    [Table("PhieuNhapKho")]
    public class GoodsReceipt
    {
        [Key]
        public int Id { get; set; }

        // Định dạng PN-YYYY-XXXX, cấp phát tuần tự theo năm tại thời điểm lưu
        // (không tin vào giá trị hiển thị preview lúc mở form, để tránh trùng
        // số khi có nhiều phiếu được tạo gần nhau).
        [Required]
        [StringLength(20)]
        public string MaPhieu { get; set; } = string.Empty;

        [Required]
        public DateTime NgayNhap { get; set; } = DateTime.Now;

        // MuaNCC, ChuyenKho, HangTraVe, VienTro
        [Required]
        [StringLength(20)]
        public string LoaiNhap { get; set; } = "MuaNCC";

        public int? NhaCungCapId { get; set; }

        [ForeignKey("NhaCungCapId")]
        public virtual Supplier? NhaCungCap { get; set; }

        [StringLength(50)]
        public string? SoHoaDonNCC { get; set; }

        public DateTime? NgayHoaDon { get; set; }

        // Nhãn mô tả kho vật lý nhận hàng (KhoChan, KhoLe, NhaThuoc). Đây là
        // metadata mô tả cho phiếu/báo cáo, KHÔNG tách tồn kho riêng theo kho -
        // Thuoc.TonKho vẫn là một con số tổng duy nhất như thiết kế hiện tại
        // của hệ thống (tách kho vật lý thực sự là một tính năng lớn hơn,
        // nằm ngoài phạm vi nâng cấp form nhập kho lần này).
        [Required]
        [StringLength(20)]
        public string KhoNhap { get; set; } = "KhoChan";

        [StringLength(100)]
        public string? NguoiGiaoHang { get; set; }

        [StringLength(500)]
        public string? GhiChu { get; set; }

        // Nhap, ChoDuyet, DaDuyet, TuChoi, DaHuy
        [Required]
        [StringLength(20)]
        public string TrangThai { get; set; } = "Nhap";

        [Required]
        public int NguoiTaoId { get; set; }

        [ForeignKey("NguoiTaoId")]
        public virtual User NguoiTao { get; set; } = null!;

        public int? NguoiDuyetId { get; set; }

        [ForeignKey("NguoiDuyetId")]
        public virtual User? NguoiDuyet { get; set; }

        public DateTime? NgayDuyet { get; set; }

        [StringLength(500)]
        public string? LyDoTuChoi { get; set; }

        // Phiếu điều chỉnh tham chiếu phiếu gốc (self-reference) để giữ vết
        // khi cần sửa sai lệch sau khi một phiếu đã được duyệt và khóa lại.
        public int? PhieuGocId { get; set; }

        [ForeignKey("PhieuGocId")]
        public virtual GoodsReceipt? PhieuGoc { get; set; }

        // Tổng hợp lưu sẵn từ ChiTiet, tính lại mỗi khi chi tiết thay đổi -
        // tránh phải join+sum lại mỗi lần hiển thị danh sách phiếu.
        public int TongSoMatHang { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TongTienTruocVAT { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TienVAT { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal TongThanhToan { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public DateTime? NgayCapNhat { get; set; }

        public virtual ICollection<GoodsReceiptDetail> ChiTiet { get; set; } = new List<GoodsReceiptDetail>();
    }

    [Table("PhieuNhapKhoChiTiet")]
    public class GoodsReceiptDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PhieuNhapKhoId { get; set; }

        [ForeignKey("PhieuNhapKhoId")]
        public virtual GoodsReceipt PhieuNhapKho { get; set; } = null!;

        [Required]
        public int ThuocId { get; set; }

        [ForeignKey("ThuocId")]
        public virtual Medicine Medicine { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string SoLo { get; set; } = string.Empty;

        [Required]
        public DateTime HanSuDung { get; set; }

        [Required]
        public int SoLuong { get; set; }

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal DonGia { get; set; }

        [Column(TypeName = "decimal(5, 2)")]
        public decimal PhanTramVAT { get; set; }

        [Column(TypeName = "decimal(18, 2)")]
        public decimal ThanhTien { get; set; }

        // Người lập phiếu đã xác nhận "vẫn nhập" dù hạn dùng dưới 6 tháng.
        public bool XacNhanCanDate { get; set; } = false;

        // Người lập phiếu chọn "cộng dồn vào lô hiện có" (thay vì tạo lô mới)
        // khi tổ hợp (ThuocId, SoLo) đã tồn tại tại thời điểm nhập liệu. Áp
        // dụng thực sự khi phiếu được duyệt (đối chiếu lại lô hiện có tại
        // đúng thời điểm duyệt, phòng trường hợp dữ liệu đã thay đổi).
        public bool CongDonVaoLoHienCo { get; set; } = false;

        // Gắn với lô thuốc thực tế được tạo/cộng dồn SAU KHI phiếu được
        // duyệt. NULL khi phiếu còn ở trạng thái Nháp/Chờ duyệt/Từ chối.
        public int? LoThuocId { get; set; }

        [ForeignKey("LoThuocId")]
        public virtual MedicineBatch? LoThuoc { get; set; }
    }
}
