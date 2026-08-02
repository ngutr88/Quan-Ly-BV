using QuanLyBenhVien.Helpers;

namespace QuanLyBenhVien.Tests;

public class DoctorTitlePrefixHelperTests
{
    [Theory]
    [InlineData("BS. Nguyễn Văn Trung", "Nguyễn Văn Trung", "BS")]
    [InlineData("BS.Nguyễn Văn Trung", "Nguyễn Văn Trung", "BS")]
    [InlineData("bs. nguyễn văn trung", "nguyễn văn trung", "BS")]
    [InlineData("ThS.BS. Nguyễn Văn Trung", "Nguyễn Văn Trung", "ThS.BS")]
    [InlineData("TS.BS. Nguyễn Đình Toàn", "Nguyễn Đình Toàn", "TS.BS")]
    [InlineData("PGS.TS.BS. Nguyễn Đình Toàn", "Nguyễn Đình Toàn", "PGS.TS.BS")]
    [InlineData("GS.TS.BS. Nguyễn Đình Toàn", "Nguyễn Đình Toàn", "GS.TS.BS")]
    [InlineData("BS.CKI. Nguyễn Đình Toàn", "Nguyễn Đình Toàn", "BS.CKI")]
    [InlineData("BS.CKII. Nguyễn Đình Toàn", "Nguyễn Đình Toàn", "BS.CKII")]
    public void StripLeadingTitle_KnownPrefix_ExtractsAndCleans(string hoTen, string expectedClean, string expectedTitle)
    {
        var (clean, extracted) = DoctorTitlePrefixHelper.StripLeadingTitle(hoTen);

        Assert.Equal(expectedClean, clean);
        Assert.Equal(expectedTitle, extracted);
    }

    [Theory]
    [InlineData("Nguyễn Văn Trung")]
    [InlineData("Trần Văn A")]
    public void StripLeadingTitle_NoPrefix_ReturnsNameUnchangedAndNullTitle(string hoTen)
    {
        var (clean, extracted) = DoctorTitlePrefixHelper.StripLeadingTitle(hoTen);

        Assert.Equal(hoTen, clean);
        Assert.Null(extracted);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void StripLeadingTitle_EmptyOrWhitespace_ReturnsEmptyAndNullTitle(string? hoTen)
    {
        var (clean, extracted) = DoctorTitlePrefixHelper.StripLeadingTitle(hoTen);

        Assert.Equal(string.Empty, clean);
        Assert.Null(extracted);
    }

    [Fact]
    public void StripLeadingTitle_StringIsOnlyThePrefix_DoesNotStripEverything()
    {
        var (clean, extracted) = DoctorTitlePrefixHelper.StripLeadingTitle("BS.");

        Assert.Equal("BS.", clean);
        Assert.Null(extracted);
    }

    [Fact]
    public void StripLeadingTitle_PrefersLongestMatchingPrefix()
    {
        var (clean, extracted) = DoctorTitlePrefixHelper.StripLeadingTitle("PGS.TS.BS. Lê Văn C");

        Assert.Equal("Lê Văn C", clean);
        Assert.Equal("PGS.TS.BS", extracted);
    }
}
