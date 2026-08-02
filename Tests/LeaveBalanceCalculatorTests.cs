using System;
using QuanLyBenhVien.Helpers;

namespace QuanLyBenhVien.Tests;

public class LeaveBalanceCalculatorTests
{
    [Theory]
    [InlineData(0, 12)]
    [InlineData(4, 12)]
    [InlineData(5, 13)]
    [InlineData(9, 13)]
    [InlineData(10, 14)]
    [InlineData(24, 16)]
    public void ComputeAnnualQuota_MatchesFormula(int soNamKinhNghiem, decimal expected)
    {
        Assert.Equal(expected, LeaveBalanceCalculator.ComputeAnnualQuota(soNamKinhNghiem));
    }

    [Fact]
    public void ComputeAnnualQuota_NegativeExperience_TreatedAsZero()
    {
        Assert.Equal(12m, LeaveBalanceCalculator.ComputeAnnualQuota(-3));
    }

    [Theory]
    [InlineData(-2, 0)]
    [InlineData(0, 0)]
    [InlineData(3, 3)]
    [InlineData(5, 5)]
    [InlineData(8, 5)]
    public void ComputeCarryOver_ClampedBetweenZeroAndCap(decimal previousRemaining, decimal expected)
    {
        Assert.Equal(expected, LeaveBalanceCalculator.ComputeCarryOver(previousRemaining));
    }

    [Fact]
    public void ComputeRequestedDays_MultiDayRange_CountsInclusive()
    {
        var days = LeaveBalanceCalculator.ComputeRequestedDays(
            new DateTime(2026, 8, 10), new DateTime(2026, 8, 12), null);
        Assert.Equal(3m, days);
    }

    [Fact]
    public void ComputeRequestedDays_SingleDay_CountsOne()
    {
        var days = LeaveBalanceCalculator.ComputeRequestedDays(
            new DateTime(2026, 8, 10), new DateTime(2026, 8, 10), null);
        Assert.Equal(1m, days);
    }

    [Fact]
    public void ComputeRequestedDays_HalfDay_CountsHalf()
    {
        var days = LeaveBalanceCalculator.ComputeRequestedDays(
            new DateTime(2026, 8, 10), new DateTime(2026, 8, 10), "Sang");
        Assert.Equal(0.5m, days);
    }

    [Fact]
    public void ComputeRequestedDays_HalfDayAcrossMultipleDays_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            LeaveBalanceCalculator.ComputeRequestedDays(new DateTime(2026, 8, 10), new DateTime(2026, 8, 11), "Sang"));
    }

    [Fact]
    public void ComputeRequestedDays_EndBeforeStart_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            LeaveBalanceCalculator.ComputeRequestedDays(new DateTime(2026, 8, 12), new DateTime(2026, 8, 10), null));
    }

    [Theory]
    [InlineData(12, 0, 0, 0, 12)]
    [InlineData(12, 5, 3, 0, 14)]
    [InlineData(12, 0, 5, 4, 3)]
    [InlineData(12, 0, 6, 8, -2)] // âm hợp lệ về mặt toán học - caller (SubmitAsync) là nơi CHẶN trước khi tới đây
    public void ComputeRemaining_MatchesFormula(decimal tong, decimal congDon, decimal daDung, decimal daTamGiu, decimal expected)
    {
        Assert.Equal(expected, LeaveBalanceCalculator.ComputeRemaining(tong, congDon, daDung, daTamGiu));
    }
}
