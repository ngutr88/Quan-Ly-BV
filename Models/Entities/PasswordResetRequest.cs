using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QuanLyBenhVien.Models
{
    // Một hàng cho mỗi lần bắt đầu "Khôi phục mật khẩu" - kể cả khi định danh
    // không khớp tài khoản nào (NguoiDungId null) hoặc khớp một tài khoản
    // nhân sự bị chặn tự phục vụ (MaXacNhanHash vẫn null, không có OTP thật).
    // Tạo hàng ở MỌI trường hợp là bắt buộc để: (1) giới hạn tần suất theo IP
    // có gì mà đếm dù định danh giả, (2) chi phí xử lý giống nhau giữa tài
    // khoản có thật/không có thật, chống dò tài khoản qua thời gian phản hồi.
    // Cũng dùng lại cho luồng Admin ("Gửi link đặt lại" - bỏ qua toàn bộ cột
    // OTP, chỉ set thẳng ResetTokenHash).
    [Table("YeuCauKhoiPhucMatKhau")]
    public class PasswordResetRequest
    {
        [Key]
        public int Id { get; set; }

        public int? NguoiDungId { get; set; }

        [ForeignKey("NguoiDungId")]
        public virtual User? User { get; set; }

        [Required]
        [StringLength(50)]
        public string IpAddress { get; set; } = "127.0.0.1";

        [Required]
        [StringLength(20)]
        public string KhoiTaoBoi { get; set; } = "TuPhucVu"; // TuPhucVu, Admin

        [StringLength(10)]
        public string? Kenh { get; set; } // Email, Sdt - null cho link do Admin gửi

        [StringLength(200)]
        public string? MaXacNhanHash { get; set; }

        public DateTime? ThoiHanMa { get; set; }

        public int SoLanNhapSai { get; set; } = 0;

        public bool MaDaDung { get; set; } = false;

        public int SoLanGuiLai { get; set; } = 0;

        // Mốc riêng cho cooldown gửi lại - tách khỏi NgayTao vì NgayTao là mốc
        // bất biến cho cửa sổ giới hạn tần suất 1 giờ, gửi lại không được làm
        // trôi cửa sổ đó.
        public DateTime? LanGuiGanNhatLuc { get; set; }

        [StringLength(200)]
        public string? ResetTokenHash { get; set; }

        public DateTime? ThoiHanResetToken { get; set; }

        public bool ResetTokenDaDung { get; set; } = false;

        public DateTime NgayTao { get; set; } = DateTime.Now;
    }
}
