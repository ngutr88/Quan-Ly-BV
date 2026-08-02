namespace QuanLyBenhVien.Models.ViewModels;

/// <summary>
/// Backs the shared strength-bar + requirements-checklist partial
/// (<c>_PasswordStrengthField</c>) so the "mật khẩu mới" field looks and
/// behaves identically everywhere it appears (forgot-password flow, Doctor
/// self-service đổi mật khẩu, ...) without copy-pasting the ~80 lines of JS.
/// </summary>
public class PasswordStrengthFieldViewModel
{
    public string NewPasswordInputId { get; set; } = "newPassword";

    public string ConfirmPasswordInputId { get; set; } = "confirmPassword";
}
