using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBenhVien.Models
{
    // 10 mã dùng 1 lần sinh cùng lúc bật 2FA (Areas/Doctor/Controllers/
    // ProfileController.TwoFactorSetup) - chỉ hiện plaintext đúng 1 lần ngay
    // sau khi bật, từ đó chỉ lưu hash (tái dùng HashHelper, cùng cơ chế mật
    // khẩu) để so khớp lúc đăng nhập nếu bác sĩ mất thiết bị authenticator.
    [Table("MaDuPhongTOTP")]
    public class TotpBackupCode
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int NguoiDungId { get; set; }

        [ForeignKey("NguoiDungId")]
        public virtual User User { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string MaHash { get; set; } = string.Empty;

        public bool DaDung { get; set; } = false;

        public DateTime? NgayDung { get; set; }

        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
