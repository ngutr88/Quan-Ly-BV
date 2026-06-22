using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBenhVien.Models
{
    [Table("Khoa")]
    public class Department
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string TenKhoa { get; set; } = string.Empty;

        [StringLength(500)]
        public string MoTa { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string ViTri { get; set; } = string.Empty;
    }

    [Table("DichVu")]
    public class Service
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int KhoaId { get; set; }

        [ForeignKey("KhoaId")]
        public virtual Department Department { get; set; } = null!;

        [Required]
        [StringLength(150)]
        public string TenDichVu { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Gia { get; set; }
    }

    [Table("LichKham")]
    public class Appointment
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BenhNhanId { get; set; }

        [ForeignKey("BenhNhanId")]
        public virtual Patient Patient { get; set; } = null!;

        public int? BacSiId { get; set; }

        [ForeignKey("BacSiId")]
        public virtual Doctor? Doctor { get; set; }

        [Required]
        public DateTime ThoiGian { get; set; }

        [Required]
        [StringLength(50)]
        public string TrangThai { get; set; } = "ChoXacNhan"; // ChoXacNhan, DaXacNhan, DangKham, HoanThanh, DaHuy, VangMat

        [StringLength(500)]
        public string LyDoKham { get; set; } = string.Empty;

        public DateTime NgayTao { get; set; } = DateTime.Now;
    }

    [Table("DanhGia")]
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BenhNhanId { get; set; }

        [ForeignKey("BenhNhanId")]
        public virtual Patient Patient { get; set; } = null!;

        [Required]
        public int BacSiId { get; set; }

        [ForeignKey("BacSiId")]
        public virtual Doctor Doctor { get; set; } = null!;

        [Range(1, 5)]
        public int SoSao { get; set; }

        [StringLength(500)]
        public string NhanXet { get; set; } = string.Empty;

        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
