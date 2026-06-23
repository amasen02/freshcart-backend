using System.Globalization;
using FluentAssertions;
using FreshCart.Reporting.Domain.Sales;

namespace FreshCart.Reporting.Tests.Domain;

public sealed class ReportingPeriodTests
{
    private static readonly DateTimeOffset FixedNowUtc = new(2026, 5, 25, 14, 30, 0, TimeSpan.Zero);
    private readonly FakeTimeProvider clock = new(FixedNowUtc);

    [Fact]
    public void TodayCoversStartOfDayToStartOfNextDay()
    {
        var period = ReportingPeriod.Today(clock);

        period.FromUtc.Should().Be(new DateTimeOffset(2026, 5, 25, 0, 0, 0, TimeSpan.Zero));
        period.ToUtcExclusive.Should().Be(new DateTimeOffset(2026, 5, 26, 0, 0, 0, TimeSpan.Zero));
    }

    [Fact]
    public void Last30DaysEndsAtNowAndStartsThirtyDaysBefore()
    {
        var period = ReportingPeriod.Last30Days(clock);

        period.ToUtcExclusive.Should().Be(FixedNowUtc);
        period.FromUtc.Should().Be(FixedNowUtc.AddDays(-30));
    }

    [Fact]
    public void MonthToDateRunsFromTheFirstOfTheMonthToNow()
    {
        var period = ReportingPeriod.MonthToDate(clock);

        period.FromUtc.Should().Be(new DateTimeOffset(2026, 5, 1, 0, 0, 0, TimeSpan.Zero));
        period.ToUtcExclusive.Should().Be(FixedNowUtc);
    }

    [Fact]
    public void YearToDateRunsFromJanuaryFirstToNow()
    {
        var period = ReportingPeriod.YearToDate(clock);

        period.FromUtc.Should().Be(new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero));
        period.ToUtcExclusive.Should().Be(FixedNowUtc);
    }

    [Theory]
    [InlineData("2026-05-25T13:00:00Z", true)]
    [InlineData("2026-05-26T00:00:00Z", false)]
    [InlineData("2026-05-24T23:59:59Z", false)]
    public void ContainsTreatsTheUpperBoundAsExclusive(string moment, bool expected)
    {
        var period = ReportingPeriod.Today(clock);

        period.Contains(DateTimeOffset.Parse(moment, CultureInfo.InvariantCulture)).Should().Be(expected);
    }

    private sealed class FakeTimeProvider(DateTimeOffset fixedNowUtc) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => fixedNowUtc;
    }
}
