using System;
using System.Linq;

namespace QuanLyBenhVien.Helpers;

/// <summary>
/// Strips a leading Vietnamese medical-title prefix (e.g. "BS.", "ThS.BS.",
/// "PGS.TS.BS.") that got embedded directly into a "Họ Tên" string instead of
/// living in its own field (<c>BacSi.HocVi</c>). Longest-prefix-first so
/// "PGS.TS.BS." never matches only as "BS.". Matching is case-insensitive and
/// tolerates a missing space after the trailing dot ("BS.Nguyễn..." as well as
/// "BS. Nguyễn...").
/// </summary>
public static class DoctorTitlePrefixHelper
{
    // Longest first - a shorter prefix earlier in the list would shadow a
    // longer one that starts with the same tokens (e.g. "BS." vs "BS.CKI.").
    private static readonly string[] KnownPrefixes =
    {
        "PGS.TS.BS.", "GS.TS.BS.",
        "TS.BS.CKII.", "TS.BS.CKI.", "TS.BS.",
        "ThS.BS.CKII.", "ThS.BS.CKI.", "ThS.BS.",
        "BS.CKII.", "BS.CKI.", "BS."
    };

    public static (string CleanName, string? ExtractedTitle) StripLeadingTitle(string? hoTen)
    {
        var trimmed = hoTen?.Trim() ?? string.Empty;
        if (trimmed.Length == 0) return (trimmed, null);

        foreach (var prefix in KnownPrefixes)
        {
            if (trimmed.Length <= prefix.Length) continue;
            if (!trimmed.AsSpan(0, prefix.Length).Equals(prefix, StringComparison.OrdinalIgnoreCase)) continue;

            var remainder = trimmed[prefix.Length..].TrimStart();
            if (remainder.Length == 0) continue; // toàn bộ chuỗi chỉ là tiền tố - không phải "prefix + tên"

            return (remainder, prefix.TrimEnd('.'));
        }

        return (trimmed, null);
    }
}
