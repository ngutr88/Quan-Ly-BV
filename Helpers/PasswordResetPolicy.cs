using System;

namespace QuanLyBenhVien.Helpers
{
    // Toàn bộ hằng số của luồng "Khôi phục mật khẩu" gom về một chỗ, theo đúng
    // khuôn tập trung hằng số đã dùng cho ModulePermissionRegistry/DoctorMenuRegistry.
    public static class PasswordResetPolicy
    {
        public static readonly TimeSpan OtpValidity = TimeSpan.FromMinutes(5);
        public const int MaxOtpAttempts = 5;

        public static readonly TimeSpan ResendCooldown = TimeSpan.FromSeconds(60);
        public const int MaxResendPerRequest = 3;

        public static readonly TimeSpan ResetTokenValidity = TimeSpan.FromMinutes(10);

        public static readonly TimeSpan RateLimitWindow = TimeSpan.FromHours(1);
        // Ngưỡng theo IP - đây là ngưỡng DUY NHẤT được phép ảnh hưởng phản hồi
        // hiển thị cho người dùng (CAPTCHA/khóa), để không mở lại kênh dò tài
        // khoản qua ngưỡng theo tài khoản (xem AuthController.ForgotPassword).
        public const int CaptchaThreshold = 4;
        public const int HardBlockThreshold = 6;
        public static readonly TimeSpan HardBlockDuration = TimeSpan.FromHours(1);

        public const int MaxSmsPerAccountPerDay = 5;

        public static readonly TimeSpan TempPasswordValidity = TimeSpan.FromHours(24);
    }
}
