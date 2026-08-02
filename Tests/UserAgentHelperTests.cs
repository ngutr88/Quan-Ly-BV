using QuanLyBenhVien.Helpers;

namespace QuanLyBenhVien.Tests;

public class UserAgentHelperTests
{
    [Theory]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36", "Chrome trên Windows")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36 Edg/120.0.0.0", "Edge trên Windows")]
    [InlineData("Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Safari/605.1.15", "Safari trên macOS")]
    [InlineData("Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:121.0) Gecko/20100101 Firefox/121.0", "Firefox trên Windows")]
    [InlineData("Mozilla/5.0 (Linux; Android 14; Pixel 8) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Mobile Safari/537.36", "Chrome trên Android")]
    [InlineData("Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) CriOS/120.0.0.0 Mobile/15E148 Safari/604.1", "Chrome trên iOS")]
    public void Summarize_KnownUserAgents_ReturnsBrowserAndOs(string userAgent, string expected)
    {
        Assert.Equal(expected, UserAgentHelper.Summarize(userAgent));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Summarize_EmptyOrWhitespace_ReturnsFallback(string? userAgent)
    {
        Assert.Equal("Thiết bị không xác định", UserAgentHelper.Summarize(userAgent));
    }

    [Fact]
    public void Summarize_UnrecognizedUserAgent_StillReturnsSomethingNonEmpty()
    {
        var result = UserAgentHelper.Summarize("SomeCustomBot/1.0");
        Assert.False(string.IsNullOrWhiteSpace(result));
    }
}
