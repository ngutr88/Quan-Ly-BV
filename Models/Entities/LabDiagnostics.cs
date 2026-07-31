using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBenhVien.Models
{
    // Danh mục dịch vụ cận lâm sàng (CLS) - độc lập với Service/DichVu (bảng
    // đó chỉ 4 trường, thuần billing theo khoa, không có nhóm CLS/nơi thực
    // hiện). NoiThucHien là text tự do vì đơn vị thực hiện CLS (vd "Khoa Xét
    // nghiệm", "Phòng CĐHA") không phải là một Department lâm sàng.
    [Table("DichVuCLS")]
    public class LabServiceCatalog
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string MaDichVu { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string TenDichVu { get; set; } = string.Empty;

        [Required]
        [StringLength(30)]
        public string NhomCLS { get; set; } = string.Empty; // HuyetHoc, SinhHoa, ViSinh, CDHA, ThamDoChucNang, GiaiPhauBenh

        [StringLength(150)]
        public string? NoiThucHien { get; set; }

        [Column(TypeName = "decimal(12, 2)")]
        public decimal Gia { get; set; }

        public bool DangHoatDong { get; set; } = true;

        [StringLength(500)]
        public string? GhiChu { get; set; }

        public virtual ICollection<LabOrderBundleItem> ThuocCacBo { get; set; } = new List<LabOrderBundleItem>();
    }

    [Table("BoChiDinhCLS")]
    public class LabOrderBundle
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string TenBo { get; set; } = string.Empty;

        [StringLength(500)]
        public string? MoTa { get; set; }

        public bool DangHoatDong { get; set; } = true;

        public virtual ICollection<LabOrderBundleItem> ThanhVien { get; set; } = new List<LabOrderBundleItem>();
    }

    [Table("ChiTietBoChiDinhCLS")]
    public class LabOrderBundleItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BoChiDinhCLSId { get; set; }

        [ForeignKey("BoChiDinhCLSId")]
        public virtual LabOrderBundle BoChiDinh { get; set; } = null!;

        [Required]
        public int DichVuCLSId { get; set; }

        [ForeignKey("DichVuCLSId")]
        public virtual LabServiceCatalog DichVu { get; set; } = null!;
    }

    // Header của phiếu chỉ định - vòng đời (Chờ/Đang/Đã có KQ/Đã hủy) nằm ở
    // ChiTietPhieuChiDinhCLS.TrangThai, KHÔNG ở header, vì một phiếu có thể
    // gồm nhiều dịch vụ tiến độ khác nhau (máu trả trong ngày, CĐHA trả hôm
    // sau) - đặt trạng thái ở header sẽ ép tất cả dịch vụ chung một trạng thái.
    [Table("PhieuChiDinhCLS")]
    public class LabOrder
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(30)]
        public string MaPhieu { get; set; } = string.Empty;

        [Required]
        public int PhieuKhamId { get; set; }

        [ForeignKey("PhieuKhamId")]
        public virtual ExaminationRecord ExaminationRecord { get; set; } = null!;

        [Required]
        public int BenhNhanId { get; set; }

        [ForeignKey("BenhNhanId")]
        public virtual Patient Patient { get; set; } = null!;

        [Required]
        public int BacSiChiDinhId { get; set; }

        [ForeignKey("BacSiChiDinhId")]
        public virtual Doctor BacSiChiDinh { get; set; } = null!;

        public DateTime NgayChiDinh { get; set; } = DateTime.Now;

        // Chỉ set khi phiếu được tạo từ một bộ chỉ định có sẵn - null khi bác
        // sĩ chọn từng dịch vụ lẻ.
        public int? BoChiDinhCLSId { get; set; }

        [ForeignKey("BoChiDinhCLSId")]
        public virtual LabOrderBundle? BoChiDinh { get; set; }

        [StringLength(500)]
        public string? GhiChuChiDinh { get; set; }

        public virtual ICollection<LabOrderItem> ChiTiet { get; set; } = new List<LabOrderItem>();
    }

    [Table("ChiTietPhieuChiDinhCLS")]
    public class LabOrderItem
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PhieuChiDinhCLSId { get; set; }

        [ForeignKey("PhieuChiDinhCLSId")]
        public virtual LabOrder PhieuChiDinh { get; set; } = null!;

        [Required]
        public int DichVuCLSId { get; set; }

        [ForeignKey("DichVuCLSId")]
        public virtual LabServiceCatalog DichVu { get; set; } = null!;

        [Required]
        [StringLength(20)]
        public string TrangThai { get; set; } = "ChoThucHien"; // ChoThucHien, DangThucHien, DaCoKetQua, DaHuy

        [StringLength(500)]
        public string? LyDoHuy { get; set; }

        public DateTime? NgayNhanThucHien { get; set; }

        public int? NguoiNhanId { get; set; }

        [ForeignKey("NguoiNhanId")]
        public virtual User? NguoiNhan { get; set; }

        public virtual LabResult? KetQua { get; set; }
    }

    // "Đã xem" là bool+timestamp ngay trên dòng kết quả (không cần bảng riêng
    // theo từng bác sĩ) vì một lượt khám chỉ có đúng một bác sĩ chỉ định.
    [Table("KetQuaCLS")]
    public class LabResult
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ChiTietPhieuChiDinhCLSId { get; set; }

        [ForeignKey("ChiTietPhieuChiDinhCLSId")]
        public virtual LabOrderItem ChiTietPhieuChiDinh { get; set; } = null!;

        [Required]
        [StringLength(1000)]
        public string KetLuan { get; set; } = string.Empty;

        public bool CoBatThuong { get; set; } = false;

        [Required]
        public int NguoiThucHienId { get; set; }

        [ForeignKey("NguoiThucHienId")]
        public virtual User NguoiThucHien { get; set; } = null!;

        // Text tự do, KHÔNG FK User - hệ thống chưa có vai trò "trưởng khoa
        // xét nghiệm" để bắt buộc một tài khoản thật xác nhận kết quả.
        [StringLength(150)]
        public string? NguoiDuyetTen { get; set; }

        public DateTime NgayTra { get; set; } = DateTime.Now;

        public bool DaXem { get; set; } = false;

        public DateTime? NgayXem { get; set; }

        public virtual ICollection<LabResultFile> Files { get; set; } = new List<LabResultFile>();
    }

    [Table("FileKetQuaCLS")]
    public class LabResultFile
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int KetQuaCLSId { get; set; }

        [ForeignKey("KetQuaCLSId")]
        public virtual LabResult KetQua { get; set; } = null!;

        [Required]
        [StringLength(260)]
        public string TenGoc { get; set; } = string.Empty;

        [Required]
        [StringLength(260)]
        public string TenLuuTru { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ContentType { get; set; } = string.Empty;

        public long KichThuoc { get; set; }

        public int ThuTu { get; set; }

        public DateTime NgayTaiLen { get; set; } = DateTime.Now;
    }
}
