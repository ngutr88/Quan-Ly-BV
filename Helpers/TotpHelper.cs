using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace QuanLyBenhVien.Helpers;

/// <summary>
/// Self-contained TOTP (RFC 6238, on top of the HOTP dynamic truncation from
/// RFC 4226) - HMAC-SHA1, 30s step, 6 digits, matching what every mainstream
/// authenticator app (Google Authenticator, Authy, ...) expects by default.
/// No third-party auth library: the algorithm is a thin, well-defined wrapper
/// around <see cref="HMACSHA1"/> already available via the BCL, same posture
/// as <c>HashHelper</c> for passwords.
/// </summary>
public static class TotpHelper
{
    private const int Digits = 6;
    private const int PeriodSeconds = 30;
    private static readonly char[] Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();

    public static string GenerateSecretBase32() => Base32Encode(RandomNumberGenerator.GetBytes(20));

    /// <param name="utcNow">Phải là giờ UTC thật (DateTime.UtcNow) - mọi ứng
    /// dụng authenticator tính mã dựa trên Unix time UTC, dùng giờ local sẽ
    /// lệch đúng bằng múi giờ server và không bao giờ khớp.</param>
    public static bool ValidateCode(string? secretBase32, string? code, DateTime utcNow, int driftSteps = 1)
    {
        if (string.IsNullOrWhiteSpace(secretBase32) || string.IsNullOrWhiteSpace(code)) return false;
        code = code.Trim();
        if (code.Length != Digits || !code.All(char.IsDigit)) return false;

        var keyBytes = Base32Decode(secretBase32);
        if (keyBytes.Length == 0) return false;

        var counter = ToCounter(utcNow);
        for (var drift = -driftSteps; drift <= driftSteps; drift++)
        {
            if (ComputeCode(keyBytes, counter + drift, Digits) == code) return true;
        }
        return false;
    }

    public static string BuildProvisioningUri(string secretBase32, string accountLabel)
    {
        var label = Uri.EscapeDataString($"MediFlow HMS:{accountLabel}");
        var issuer = Uri.EscapeDataString("MediFlow HMS");
        return $"otpauth://totp/{label}?secret={secretBase32}&issuer={issuer}&digits={Digits}&period={PeriodSeconds}&algorithm=SHA1";
    }

    /// <summary>RFC 4226 dynamic truncation - public (không chỉ nội bộ) để
    /// unit test đối chiếu trực tiếp với vector chuẩn RFC 6238 Appendix B
    /// (byte khoá thô, không qua Base32), thay vì chỉ tự nhất quán với chính nó.</summary>
    public static string ComputeCode(byte[] secretBytes, long counter, int digits = Digits)
    {
        var counterBytes = BitConverter.GetBytes(counter);
        if (BitConverter.IsLittleEndian) Array.Reverse(counterBytes);

        using var hmac = new HMACSHA1(secretBytes);
        var hash = hmac.ComputeHash(counterBytes);

        var offset = hash[^1] & 0x0F;
        var binaryCode = ((hash[offset] & 0x7F) << 24)
            | ((hash[offset + 1] & 0xFF) << 16)
            | ((hash[offset + 2] & 0xFF) << 8)
            | (hash[offset + 3] & 0xFF);

        var otp = binaryCode % (int)Math.Pow(10, digits);
        return otp.ToString(new string('0', digits));
    }

    private static long ToCounter(DateTime utcNow) => (long)(utcNow - DateTime.UnixEpoch).TotalSeconds / PeriodSeconds;

    /// <summary>Public (không chỉ nội bộ) cùng lý do như ComputeCode - cho
    /// phép unit test mã hoá đúng bí mật ASCII đã biết từ RFC 6238 Appendix B
    /// thành Base32 rồi đưa qua ValidateCode, kiểm tra nguyên vẹn đường đi
    /// thật thay vì chỉ so khớp với chính thuật toán.</summary>
    public static string Base32Encode(byte[] data)
    {
        var sb = new StringBuilder();
        int bits = 0, value = 0;
        foreach (var b in data)
        {
            value = (value << 8) | b;
            bits += 8;
            while (bits >= 5)
            {
                sb.Append(Base32Alphabet[(value >> (bits - 5)) & 0x1F]);
                bits -= 5;
            }
        }
        if (bits > 0)
        {
            sb.Append(Base32Alphabet[(value << (5 - bits)) & 0x1F]);
        }
        return sb.ToString();
    }

    public static byte[] Base32Decode(string base32)
    {
        base32 = base32.Trim().TrimEnd('=').ToUpperInvariant();
        var bytes = new System.Collections.Generic.List<byte>();
        int bits = 0, value = 0;
        foreach (var c in base32)
        {
            var idx = Array.IndexOf(Base32Alphabet, c);
            if (idx < 0) continue; // bỏ qua khoảng trắng/ký tự lạ (người dùng có thể dán kèm dấu cách)
            value = (value << 5) | idx;
            bits += 5;
            if (bits >= 8)
            {
                bytes.Add((byte)((value >> (bits - 8)) & 0xFF));
                bits -= 8;
            }
        }
        return bytes.ToArray();
    }
}
