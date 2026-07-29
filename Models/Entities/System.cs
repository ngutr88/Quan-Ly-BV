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

        // Structured target reference so callers can reliably query "latest log
        // for record X" instead of substring-matching ChiTiet. Nullable/additive:
        // existing rows predate this and simply have no target.
        [StringLength(50)]
        public string? DoiTuongLoai { get; set; }

        public int? DoiTuongId { get; set; }
    }

    // Fine-grained, admin-editable on/off switch layered on top of the fixed
    // per-area [Authorize(Roles=...)] gate. A missing row means "allowed" (the
    // area gate already governs access); a row with DuocPhep=false additionally
    // hides that one module from that role. This can never grant a role access
    // to another role's area — it can only further restrict within a role's own
    // area — so it cannot loosen the authorization boundary already enforced by
    // the area-level [Authorize] attributes.
    [Table("PhanQuyenVaiTro")]
    public class RolePermission
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(20)]
        public string VaiTro { get; set; } = string.Empty;

        // "{Area}.{Controller}", e.g. "Admin.Medicines", "Doctor.Chat", "Patient.Payment".
        [Required]
        [StringLength(100)]
        public string ModuleKey { get; set; } = string.Empty;

        public bool DuocPhep { get; set; } = true;

        public DateTime CapNhatLuc { get; set; } = DateTime.Now;

        public int? CapNhatBoiId { get; set; }

        [ForeignKey("CapNhatBoiId")]
        public virtual User? CapNhatBoi { get; set; }
    }
}
