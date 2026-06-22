using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBenhVien.Models
{
    [Table("NguoiDung")]
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string HoTen { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Sdt { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string MatKhauHash { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string VaiTro { get; set; } = string.Empty; // Admin, Doctor, Patient

        [Required]
        [StringLength(50)]
        public string TrangThai { get; set; } = "Active"; // Active, Blocked

        public DateTime NgayTao { get; set; } = DateTime.Now;

        // Navigation properties
        public virtual Doctor? DoctorProfile { get; set; }
        public virtual Patient? PatientProfile { get; set; }
    }

    [Table("BacSi")]
    public class Doctor
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int NguoiDungId { get; set; }

        [ForeignKey("NguoiDungId")]
        public virtual User User { get; set; } = null!;

        [Required]
        public int KhoaId { get; set; }

        [ForeignKey("KhoaId")]
        public virtual Department Department { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string ChuyenKhoa { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string HocVi { get; set; } = string.Empty; // BS, ThS, TS, PGS, GS

        public int SoNamKinhNghiem { get; set; }

        [StringLength(200)]
        public string LichLamViec { get; set; } = "Ca sang (08:00 - 12:00) & Chiều (13:30 - 17:30)";
    }

    [Table("BenhNhan")]
    public class Patient
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int NguoiDungId { get; set; }

        [ForeignKey("NguoiDungId")]
        public virtual User User { get; set; } = null!;

        [Required]
        public DateTime NgaySinh { get; set; }

        [Required]
        [StringLength(10)]
        public string GioiTinh { get; set; } = string.Empty;

        [StringLength(10)]
        public string NhomMau { get; set; } = "O+";

        [StringLength(50)]
        public string SoBHYT { get; set; } = string.Empty;

        [StringLength(500)]
        public string TienSuBenh { get; set; } = string.Empty;

        [StringLength(500)]
        public string DiUng { get; set; } = string.Empty;
    }
}
