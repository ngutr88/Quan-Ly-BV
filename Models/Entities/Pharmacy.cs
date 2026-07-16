using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBenhVien.Models
{
    [Table("Thuoc")]
    public class Medicine
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(150)]
        public string TenThuoc { get; set; } = string.Empty;

        [Required]
        [StringLength(150)]
        public string HoatChat { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string DonViTinh { get; set; } = string.Empty; // e.g. "Viên", "Chai", "Vỉ"

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Gia { get; set; }

        [Required]
        public int TonKho { get; set; }

        [Required]
        public int NguongToiThieu { get; set; } // Ngưỡng tồn tối thiểu để cảnh báo

        public virtual ICollection<MedicineBatch> LoThuocs { get; set; } = new List<MedicineBatch>();
    }

    [Table("LoThuoc")]
    public class MedicineBatch
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ThuocId { get; set; }

        [ForeignKey("ThuocId")]
        public virtual Medicine Medicine { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string SoLo { get; set; } = string.Empty; // Batch/Lot number

        [Required]
        public DateTime HanSuDung { get; set; }

        [Required]
        public int SoLuongNhap { get; set; }

        [Required]
        public int SoLuongTon { get; set; }
    }
}
