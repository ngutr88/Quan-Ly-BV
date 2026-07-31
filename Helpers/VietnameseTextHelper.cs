using System.Globalization;
using System.Text;

namespace QuanLyBenhVien.Helpers;

/// <summary>
/// Small text-matching helper for free-text Vietnamese fields (e.g.
/// Doctor.ChuyenKhoa) where callers can't rely on diacritics always being
/// typed consistently.
/// </summary>
public static class VietnameseTextHelper
{
    public static string RemoveDiacritics(string text)
    {
        if (string.IsNullOrEmpty(text)) return text;

        var normalized = text.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder();
        foreach (var c in normalized)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                builder.Append(c);
        }

        return builder.ToString().Normalize(NormalizationForm.FormC).Replace('đ', 'd').Replace('Đ', 'D');
    }

    public static bool ContainsIgnoreCase(string text, string term)
    {
        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(term)) return false;
        var normalizedText = RemoveDiacritics(text);
        var normalizedTerm = RemoveDiacritics(term);
        return normalizedText.Contains(normalizedTerm, StringComparison.OrdinalIgnoreCase);
    }
}
