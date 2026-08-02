namespace QuanLyBenhVien.Helpers;

/// <summary>
/// Turns a raw User-Agent header into a short "Trình duyệt trên Hệ điều hành"
/// summary for the "Phiên đăng nhập đang hoạt động" list - simple substring
/// matching, not a full parser. Good enough to tell devices apart at a
/// glance; not meant to be forensically precise.
/// </summary>
public static class UserAgentHelper
{
    public static string Summarize(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent)) return "Thiết bị không xác định";

        var ua = userAgent;

        // Thứ tự so khớp có ý nghĩa: Edge/Opera cũng chứa "Chrome" trong UA
        // của chúng, phải kiểm các trình duyệt dựa trên Chromium đặc thù
        // TRƯỚC "Chrome" chung chung.
        string browser = ua switch
        {
            _ when ua.Contains("Edg/") || ua.Contains("EdgA/") || ua.Contains("EdgiOS/") => "Edge",
            _ when ua.Contains("OPR/") || ua.Contains("Opera") => "Opera",
            _ when ua.Contains("Firefox/") => "Firefox",
            _ when ua.Contains("CriOS/") => "Chrome",
            _ when ua.Contains("Chrome/") => "Chrome",
            _ when ua.Contains("Safari/") && ua.Contains("Version/") => "Safari",
            _ => "Trình duyệt"
        };

        string os = ua switch
        {
            _ when ua.Contains("Windows") => "Windows",
            _ when ua.Contains("Mac OS X") && ua.Contains("Mobile") => "iOS",
            _ when ua.Contains("iPhone") || ua.Contains("iPad") => "iOS",
            _ when ua.Contains("Mac OS X") => "macOS",
            _ when ua.Contains("Android") => "Android",
            _ when ua.Contains("Linux") => "Linux",
            _ => null
        };

        return os == null ? browser : $"{browser} trên {os}";
    }
}
