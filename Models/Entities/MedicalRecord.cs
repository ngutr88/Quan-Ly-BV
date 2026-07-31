using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBenhVien.Models
{
    [Table("TaiLieuBenhNhan")]
    public class PatientDocument
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BenhNhanId { get; set; }

        [ForeignKey("BenhNhanId")]
        public virtual Patient Patient { get; set; } = null!;

        [Required, StringLength(150)]
        public string TenTaiLieu { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string LoaiTaiLieu { get; set; } = "GiayToKhamBenh";

        [Required, StringLength(260)]
        public string TenLuuTru { get; set; } = string.Empty;

        [Required, StringLength(100)]
        public string ContentType { get; set; } = string.Empty;

        public long KichThuoc { get; set; }

        [StringLength(500)]
        public string GhiChu { get; set; } = string.Empty;

        public DateTime NgayTaiLen { get; set; } = DateTime.Now;
    }

    [Table("PhieuKham")]
    public class ExaminationRecord
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int LichKhamId { get; set; }

        [ForeignKey("LichKhamId")]
        public virtual Appointment Appointment { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string TrieuChung { get; set; } = string.Empty;

        [StringLength(20)]
        public string HuyetAp { get; set; } = string.Empty; // e.g. "120/80"

        public int? NhipTim { get; set; } // e.g. 75

        [Column(TypeName = "decimal(4, 1)")]
        public decimal? NhietDo { get; set; } // e.g. 36.5

        [Column(TypeName = "decimal(5, 2)")]
        public decimal? CanNang { get; set; } // e.g. 65.5

        [Column(TypeName = "decimal(5, 2)")]
        public decimal? ChieuCao { get; set; } // e.g. 170.0

        [Column(TypeName = "decimal(4, 2)")]
        public decimal? BMI { get; set; } // Body Mass Index

        [Required]
        [StringLength(500)]
        public string ChanDoan { get; set; } = string.Empty;

        [StringLength(500)]
        public string LoiDan { get; set; } = string.Empty;

        [StringLength(500)]
        public string ChiDinhCLS { get; set; } = string.Empty; // Xét nghiệm cận lâm sàng chỉ định

        [StringLength(1000)]
        public string KetQuaCLS { get; set; } = string.Empty; // Kết quả cận lâm sàng (file link hoặc mô tả)

        public DateTime NgayKham { get; set; } = DateTime.Now;

        // Ngày hẹn tái khám do bác sĩ chỉ định lúc kê đơn/hoàn tất khám - cấp
        // dữ liệu có cấu trúc cho nhắc tái khám ở Cổng bệnh nhân (trước đây chỉ
        // suy đoán qua từ khóa bệnh mạn tính + 12 tuần chưa khám).
        public DateTime? NgayHenTaiKham { get; set; }

        // Ghi chú nhận định riêng của bác sĩ khi đọc kết quả CLS - tách khỏi
        // LoiDan (đó là dặn dò cho bệnh nhân), trường này chỉ nội bộ chuyên môn.
        [StringLength(1000)]
        public string? GhiChuCLSCuaBacSi { get; set; }
    }

    [Table("DonThuoc")]
    public class Prescription
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PhieuKhamId { get; set; }

        [ForeignKey("PhieuKhamId")]
        public virtual ExaminationRecord ExaminationRecord { get; set; } = null!;

        public DateTime NgayKe { get; set; } = DateTime.Now;

        // Bác sĩ kê đơn - tham chiếu trực tiếp (trước đây chỉ suy ra gián tiếp
        // qua ExaminationRecord.Appointment.BacSiId), cần thiết để truy vấn
        // "đơn tôi đã kê" hiệu quả cho trang Kê đơn & Y lệnh.
        [Required]
        public int BacSiKeId { get; set; }

        [ForeignKey("BacSiKeId")]
        public virtual Doctor BacSiKe { get; set; } = null!;

        [Required]
        [StringLength(20)]
        public string TrangThai { get; set; } = "ChoCapPhat"; // Nhap, ChoCapPhat, DaCapPhat, DuocPhanHoi, DaHuy

        [StringLength(500)]
        public string? LyDoHuy { get; set; }

        public virtual ICollection<PrescriptionDetail> PrescriptionDetails { get; set; } = new List<PrescriptionDetail>();
    }

    [Table("ChiTietDonThuoc")]
    public class PrescriptionDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DonThuocId { get; set; }

        [ForeignKey("DonThuocId")]
        public virtual Prescription Prescription { get; set; } = null!;

        [Required]
        public int ThuocId { get; set; }

        [ForeignKey("ThuocId")]
        public virtual Medicine Medicine { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string LieuDung { get; set; } = string.Empty; // e.g. "Ngày uống 2 lần, mỗi lần 1 viên sau ăn"

        [Required]
        public int SoLuong { get; set; }

        // Liều dùng có cấu trúc (bổ sung cho LieuDung tự do ở trên, giữ nguyên
        // để không phá dữ liệu cũ) - dùng để engine an toàn kiểm tra liều/ngày
        // và để tự tính SoLuong ở server thay vì tin số client gửi.
        [Column(TypeName = "decimal(10, 2)")]
        public decimal? LieuMoiLan { get; set; }

        public int? SoLanMoiNgay { get; set; }

        [StringLength(50)]
        public string? DuongDung { get; set; } // Uống, Tiêm, Bôi ngoài da...

        [StringLength(50)]
        public string? ThoiDiemDung { get; set; } // Trước ăn sáng, sau ăn tối...

        public int? SoNgayDung { get; set; }

        [StringLength(300)]
        public string? HuongDanSuDung { get; set; } // Nhãn in, tự sinh từ các cột trên nhưng cho phép sửa tay
    }

    [Table("TienSuGiaDinh")]
    public class FamilyHistory
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BenhNhanId { get; set; }

        [ForeignKey("BenhNhanId")]
        public virtual Patient Patient { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string QuanHe { get; set; } = string.Empty; // Cha, Mẹ, Anh/Chị/Em, Ông/Bà...

        [Required]
        [StringLength(200)]
        public string TenBenh { get; set; } = string.Empty;

        [StringLength(300)]
        public string GhiChu { get; set; } = string.Empty;

        public DateTime NgayGhiNhan { get; set; } = DateTime.Now;
    }

    [Table("TiemChung")]
    public class Immunization
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BenhNhanId { get; set; }

        [ForeignKey("BenhNhanId")]
        public virtual Patient Patient { get; set; } = null!;

        [Required]
        [StringLength(150)]
        public string TenVaccine { get; set; } = string.Empty;

        [Required]
        public DateTime NgayTiem { get; set; }

        [StringLength(20)]
        public string MuiSo { get; set; } = string.Empty; // "Mũi 1", "Mũi 2", "Nhắc lại"...

        [StringLength(300)]
        public string GhiChu { get; set; } = string.Empty;
    }

    [Table("ChiSoSucKhoeTuDo")]
    public class PatientHealthMetric
    {
        [Key]
        public int Id { get; set; }
        [Required]
        public int BenhNhanId { get; set; }
        [ForeignKey("BenhNhanId")]
        public virtual Patient Patient { get; set; } = null!;
        public DateTime NgayDo { get; set; } = DateTime.Now;
        [Column(TypeName = "decimal(5, 2)")]
        public decimal? CanNang { get; set; }
        [Column(TypeName = "decimal(5, 2)")]
        public decimal? ChieuCao { get; set; }
        public int? HuyetApTamThu { get; set; }
        public int? HuyetApTamTruong { get; set; }
        public int? NhipTim { get; set; }
        [Column(TypeName = "decimal(5, 2)")]
        public decimal? DuongHuyet { get; set; }
        [StringLength(300)]
        public string GhiChu { get; set; } = string.Empty;
        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
