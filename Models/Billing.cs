using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBenhVien.Models
{
    [Table("HoaDon")]
    public class Invoice
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PhieuKhamId { get; set; }

        [ForeignKey("PhieuKhamId")]
        public virtual ExaminationRecord ExaminationRecord { get; set; } = null!;

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TongTien { get; set; }

        [Required]
        [StringLength(50)]
        public string TrangThaiThanhToan { get; set; } = "ChuaThanhToan"; // ChuaThanhToan, DaThanhToan, DaHuy

        [Required]
        [StringLength(50)]
        public string PhuongThuc { get; set; } = "TienMat"; // TienMat, ChuyenKhoan, Online

        public DateTime NgayTao { get; set; } = DateTime.Now;

        public DateTime? NgayThanhToan { get; set; }

        public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();
    }

    [Table("ChiTietHoaDon")]
    public class InvoiceDetail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int HoaDonId { get; set; }

        [ForeignKey("HoaDonId")]
        public virtual Invoice Invoice { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string LoaiPhi { get; set; } = string.Empty; // e.g. "PhiKham", "PhiThuoc", "PhiDichVu"

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal SoTien { get; set; }
    }
}
