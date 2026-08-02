using System.Linq;

namespace QuanLyBenhVien.Helpers;

/// <summary>
/// Single source of truth for the minimum password policy (>= 8 chars, has
/// upper/lower/digit) - previously duplicated as a private method inside
/// <c>AuthController</c>, unreachable from any other controller that needs the
/// same rule (e.g. the Doctor self-service "Đổi mật khẩu" form).
/// </summary>
public static class PasswordPolicyHelper
{
    public static bool IsCompliant(string? password) =>
        !string.IsNullOrEmpty(password) && password.Length >= 8 &&
        password.Any(char.IsUpper) && password.Any(char.IsLower) && password.Any(char.IsDigit);
}
