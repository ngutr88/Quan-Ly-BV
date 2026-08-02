using System.Collections.Generic;
using System.Security.Cryptography;

namespace QuanLyBenhVien.Helpers;

/// <summary>
/// Generates one-time 2FA backup codes shown exactly once right after 2FA is
/// enabled (Areas/Doctor/Controllers/ProfileController.TwoFactorSetup). Only
/// the hash is ever persisted (via <see cref="HashHelper"/>, the same scheme
/// already used for passwords) - the plaintext codes exist only in the HTTP
/// response that shows them, never in the database.
/// </summary>
public static class BackupCodeHelper
{
    public static List<string> GenerateCodes(int count = 10)
    {
        var codes = new List<string>(count);
        for (var i = 0; i < count; i++)
        {
            var value = RandomNumberGenerator.GetInt32(0, 100_000_000);
            codes.Add(value.ToString("D8"));
        }
        return codes;
    }
}
