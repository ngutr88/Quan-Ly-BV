using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBenhVien.Models
{
    [Table("NguoiThan")]
    public class Dependent
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int BenhNhanId { get; set; }

        [ForeignKey("BenhNhanId")]
        public virtual Patient Patient { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string HoTen { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string QuanHe { get; set; } = string.Empty;

        [Required]
        [StringLength(10)]
        public string GioiTinh { get; set; } = string.Empty;

        [Required]
        public int NamSinh { get; set; }

        [Required]
        [StringLength(10)]
        public string NhomMau { get; set; } = "O+";

        [Required]
        [StringLength(50)]
        public string SoBHYT { get; set; } = string.Empty;

        [Required]
        [StringLength(500)]
        public string TienSuBenhLy { get; set; } = string.Empty;
    }
}
