using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBenhVien.Models
{
    // Một hàng cho mỗi lần đăng nhập thành công - cho phép liệt kê "phiên đang
    // hoạt động" và thu hồi ĐÚNG 1 phiên mà không cần bump SecurityStamp toàn
    // cục (vốn thu hồi tất cả). SessionToken được nhúng vào cookie đăng nhập
    // qua claim "sid"; SecurityStampCookieValidator kiểm tra cả 2 lớp mỗi
    // request - xem ghi chú ở đó.
    [Table("PhienDangNhap")]
    public class LoginSession
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int NguoiDungId { get; set; }

        [ForeignKey("NguoiDungId")]
        public virtual User User { get; set; } = null!;

        [Required]
        [StringLength(64)]
        public string SessionToken { get; set; } = string.Empty;

        [Required]
        [StringLength(200)]
        public string ThietBi { get; set; } = "Thiết bị không xác định";

        [StringLength(50)]
        public string IpAddress { get; set; } = "127.0.0.1";

        public DateTime ThoiGianDangNhap { get; set; } = DateTime.Now;

        public DateTime ThoiGianHoatDongCuoi { get; set; } = DateTime.Now;

        [Required]
        [StringLength(20)]
        public string TrangThai { get; set; } = "HoatDong"; // HoatDong, DaDangXuat
    }
}
