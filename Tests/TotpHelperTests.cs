using System;
using System.Text;
using QuanLyBenhVien.Helpers;

namespace QuanLyBenhVien.Tests;

public class TotpHelperTests
{
    // RFC 6238 Appendix B test vectors (SHA1), secret ASCII "12345678901234567890"
    // (20 byte, dùng thẳng không qua Base32 - đúng như phụ lục RFC quy định).
    // Giá trị gốc RFC là 8 chữ số; helper này dùng 6 chữ số nên lấy 6 chữ số
    // cuối (otp % 10^6) - cùng một phép cắt/mod, chỉ khác số chữ số giữ lại.
    private static readonly byte[] Rfc6238Secret = Encoding.ASCII.GetBytes("12345678901234567890");

    [Theory]
    [InlineData(1L, "287082")]           // Time = 59      -> counter 1,        8-digit gốc 94287082
    [InlineData(37037036L, "081804")]    // Time = 1111111109 -> counter 37037036, 8-digit gốc 07081804
    [InlineData(37037037L, "050471")]    // Time = 1111111111 -> counter 37037037, 8-digit gốc 14050471
    [InlineData(41152263L, "005924")]    // Time = 1234567890 -> counter 41152263, 8-digit gốc 89005924
    [InlineData(66666666L, "279037")]    // Time = 2000000000 -> counter 66666666, 8-digit gốc 69279037
    public void ComputeCode_MatchesRfc6238TestVectors(long counter, string expectedSixDigit)
    {
        Assert.Equal(expectedSixDigit, TotpHelper.ComputeCode(Rfc6238Secret, counter));
    }

    [Fact]
    public void ValidateCode_EndToEndThroughBase32_AcceptsCorrectCode()
    {
        var secretBase32 = TotpHelper.Base32Encode(Rfc6238Secret);
        var expectedCode = TotpHelper.ComputeCode(Rfc6238Secret, 1L); // Time = 59
        var utcNow = DateTime.UnixEpoch.AddSeconds(59);

        Assert.True(TotpHelper.ValidateCode(secretBase32, expectedCode, utcNow, driftSteps: 0));
    }

    [Fact]
    public void ValidateCode_WrongCode_Rejected()
    {
        var secretBase32 = TotpHelper.Base32Encode(Rfc6238Secret);
        var utcNow = DateTime.UnixEpoch.AddSeconds(59);

        Assert.False(TotpHelper.ValidateCode(secretBase32, "000000", utcNow, driftSteps: 0));
    }

    [Fact]
    public void ValidateCode_WithinDriftWindow_Accepted()
    {
        var secretBase32 = TotpHelper.Base32Encode(Rfc6238Secret);
        var codeForNextStep = TotpHelper.ComputeCode(Rfc6238Secret, 2L); // counter+1 so với thời điểm bên dưới
        var utcNow = DateTime.UnixEpoch.AddSeconds(59); // counter = 1

        Assert.True(TotpHelper.ValidateCode(secretBase32, codeForNextStep, utcNow, driftSteps: 1));
    }

    [Fact]
    public void ValidateCode_OutsideDriftWindow_Rejected()
    {
        var secretBase32 = TotpHelper.Base32Encode(Rfc6238Secret);
        var codeTwoStepsAhead = TotpHelper.ComputeCode(Rfc6238Secret, 3L); // counter+2
        var utcNow = DateTime.UnixEpoch.AddSeconds(59); // counter = 1

        Assert.False(TotpHelper.ValidateCode(secretBase32, codeTwoStepsAhead, utcNow, driftSteps: 1));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("12345")]   // thiếu 1 số
    [InlineData("abcdef")]  // không phải số
    public void ValidateCode_MalformedCode_Rejected(string? code)
    {
        var secretBase32 = TotpHelper.Base32Encode(Rfc6238Secret);
        Assert.False(TotpHelper.ValidateCode(secretBase32, code, DateTime.UtcNow));
    }

    [Fact]
    public void ValidateCode_NullOrEmptySecret_Rejected()
    {
        Assert.False(TotpHelper.ValidateCode(null, "123456", DateTime.UtcNow));
        Assert.False(TotpHelper.ValidateCode("", "123456", DateTime.UtcNow));
    }

    [Fact]
    public void Base32_RoundTrip_PreservesOriginalBytes()
    {
        var original = RandomNumberGeneratorBytes(20);
        var encoded = TotpHelper.Base32Encode(original);
        var decoded = TotpHelper.Base32Decode(encoded);

        Assert.Equal(original, decoded);
    }

    [Fact]
    public void GenerateSecretBase32_ProducesUsableSecretOfExpectedLength()
    {
        var secret = TotpHelper.GenerateSecretBase32();

        // 20 byte -> ceil(20*8/5) = 32 ký tự Base32, không padding.
        Assert.Equal(32, secret.Length);
        Assert.Equal(20, TotpHelper.Base32Decode(secret).Length);
    }

    [Fact]
    public void BuildProvisioningUri_ContainsSecretAndIssuer()
    {
        var secret = TotpHelper.GenerateSecretBase32();
        var uri = TotpHelper.BuildProvisioningUri(secret, "doctor@hms.com");

        Assert.StartsWith("otpauth://totp/", uri);
        Assert.Contains($"secret={secret}", uri);
        Assert.Contains("issuer=MediFlow%20HMS", uri);
        Assert.Contains("digits=6", uri);
        Assert.Contains("period=30", uri);
    }

    private static byte[] RandomNumberGeneratorBytes(int count) =>
        System.Security.Cryptography.RandomNumberGenerator.GetBytes(count);
}
