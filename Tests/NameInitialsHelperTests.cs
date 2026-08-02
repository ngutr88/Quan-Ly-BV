using QuanLyBenhVien.Helpers;

namespace QuanLyBenhVien.Tests;

public class NameInitialsHelperTests
{
    [Theory]
    [InlineData("Nguyễn Văn Trung", "NT")]
    [InlineData("Trần Văn A", "TA")]
    [InlineData("  Lê Thị Mai   Anh  ", "LA")]
    public void GetInitials_MultiWordName_UsesFirstAndLastWord(string hoTen, string expected)
    {
        Assert.Equal(expected, NameInitialsHelper.GetInitials(hoTen));
    }

    [Theory]
    [InlineData("Madonna", "M")]
    [InlineData("x", "X")]
    public void GetInitials_SingleWordName_UsesFirstLetterOnly(string hoTen, string expected)
    {
        Assert.Equal(expected, NameInitialsHelper.GetInitials(hoTen));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void GetInitials_EmptyOrWhitespace_ReturnsPlaceholder(string? hoTen)
    {
        Assert.Equal("?", NameInitialsHelper.GetInitials(hoTen));
    }
}
