using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBenhVien.Models
{
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
    }
}
