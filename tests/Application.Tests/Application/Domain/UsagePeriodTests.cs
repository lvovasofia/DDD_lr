
using Application.Domain.Model.ValueObjects;
using Xunit;

namespace Application.Tests.Application.Domain;

public class UsagePeriodTests
{
    [Fact]
    public void Create_WithValidDates_Succeeds()
    {
        var start = new DateTimeOffset(
            2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

        var end = start.AddDays(2);

        var period = new UsagePeriod(start, end);

        Assert.Equal(start, period.Start);
        Assert.Equal(end, period.End);
    }

    [Fact]
    public void Create_WithEqualDates_ThrowsException()
    {
        var date = new DateTimeOffset(
            2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

        Assert.Throws<ArgumentException>(() =>
            new UsagePeriod(date, date));
    }

    [Fact]
    public void Create_WithEndBeforeStart_ThrowsException()
    {
        var start = new DateTimeOffset(
            2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

        var end = start.AddDays(-1);

        Assert.Throws<ArgumentException>(() =>
            new UsagePeriod(start, end));
    }

    [Fact]
    public void TwoPeriods_WithSameDates_AreEqual()
    {
        var start = new DateTimeOffset(
            2026, 11, 10, 9, 0, 0, TimeSpan.Zero);

        var end = start.AddDays(2);

        var first = new UsagePeriod(start, end);
        var second = new UsagePeriod(start, end);

        Assert.Equal(first, second);
    }
}
