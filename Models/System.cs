using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBenhVien.Models
{
    [Table("ThongBao")]
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int NguoiDungId { get; set; }

        [ForeignKey("NguoiDungId")]
        public virtual User User { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string NoiDung { get; set; } = string.Empty;

        public DateTime NgayGui { get; set; } = DateTime.Now;

        public bool DaDoc { get; set; } = false;
    }

    [Table("NhatKyHeThong")]
    public class AuditLog
    {
        [Key]
        public int Id { get; set; }

        public int? NguoiDungId { get; set; }

        [ForeignKey("NguoiDungId")]
        public virtual User? User { get; set; }

        [Required]
        [StringLength(100)]
        public string HanhDong { get; set; } = string.Empty;

        [Required]
        [StringLength(1000)]
        public string ChiTiet { get; set; } = string.Empty;

        public DateTime ThoiGian { get; set; } = DateTime.Now;

        [StringLength(50)]
        public string IpAddress { get; set; } = "127.0.0.1";
    }
}
