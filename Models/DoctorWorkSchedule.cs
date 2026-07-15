using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBenhVien.Models
{
    [Table("LichLamViecBacSi")]
    public class DoctorWorkSchedule
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BacSiId { get; set; }

        [ForeignKey("BacSiId")]
        public virtual Doctor Doctor { get; set; } = null!;

        [Range(0, 6)]
        public int ThuTrongTuan { get; set; } // 0=Sunday, 1=Monday, ..., 6=Saturday

        [Required]
        public TimeSpan GioBatDau { get; set; }

        [Required]
        public TimeSpan GioKetThuc { get; set; }

        [Range(5, 240)]
        public int ThoiLuongKhamPhut { get; set; } = 30;

        [Range(1, 50)]
        public int SoBenhNhanToiDa { get; set; } = 1;

        [StringLength(50)]
        public string PhongKham { get; set; } = string.Empty;

        public DateTime? HieuLucTu { get; set; }

        public DateTime? HieuLucDen { get; set; }

        public bool DangHoatDong { get; set; } = true;
    }
}
