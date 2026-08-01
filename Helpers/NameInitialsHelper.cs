using System;
using System.Linq;

namespace QuanLyBenhVien.Helpers;

/// <summary>
/// Single rule for turning a Vietnamese "Họ Tên" full name into a 2-letter
/// avatar abbreviation, applied system-wide. Previously several views each
/// rolled their own (Substring(0,2) on the whole string, or just the first
/// character) which reads wrong for "Họ Tên đệm Tên" order - e.g. "Trần Văn
/// A" produced "TH" instead of a name-shaped abbreviation. The rule here is
/// first letter of the FIRST word (họ) + first letter of the LAST word (tên),
/// which is the part people actually go by.
/// </summary>
public static class NameInitialsHelper
{
    public static string GetInitials(string? hoTen)
    {
        if (string.IsNullOrWhiteSpace(hoTen)) return "?";

        var words = hoTen.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0) return "?";
        if (words.Length == 1) return words[0][..1].ToUpperInvariant();

        return (words[0][..1] + words[^1][..1]).ToUpperInvariant();
    }
}
