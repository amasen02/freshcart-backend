using System.Runtime.InteropServices;

namespace FreshCart.Reporting.Domain.Sales;

/// <summary>
/// Half-open time window <c>[FromUtc, ToUtcExclusive)</c> used by every period-scoped query.
/// Half-open intervals avoid the classic off-by-one bug at midnight.
/// </summary>
[StructLayout(LayoutKind.Auto)]
public readonly record struct ReportingPeriod(DateTimeOffset FromUtc, DateTimeOffset ToUtcExclusive)
{
    public bool Contains(DateTimeOffset moment) => moment >= FromUtc && moment < ToUtcExclusive;

    public TimeSpan Duration => ToUtcExclusive - FromUtc;

    public static ReportingPeriod Today(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        var nowUtc = timeProvider.GetUtcNow();
        var startOfDay = new DateTimeOffset(nowUtc.Year, nowUtc.Month, nowUtc.Day, 0, 0, 0, TimeSpan.Zero);
        return new ReportingPeriod(startOfDay, startOfDay.AddDays(1));
    }

    public static ReportingPeriod Last30Days(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        var endExclusive = timeProvider.GetUtcNow();
        return new ReportingPeriod(endExclusive.AddDays(-30), endExclusive);
    }

    public static ReportingPeriod MonthToDate(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        var nowUtc = timeProvider.GetUtcNow();
        var startOfMonth = new DateTimeOffset(nowUtc.Year, nowUtc.Month, 1, 0, 0, 0, TimeSpan.Zero);
        return new ReportingPeriod(startOfMonth, nowUtc);
    }

    public static ReportingPeriod YearToDate(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);
        var nowUtc = timeProvider.GetUtcNow();
        var startOfYear = new DateTimeOffset(nowUtc.Year, 1, 1, 0, 0, 0, TimeSpan.Zero);
        return new ReportingPeriod(startOfYear, nowUtc);
    }
}
