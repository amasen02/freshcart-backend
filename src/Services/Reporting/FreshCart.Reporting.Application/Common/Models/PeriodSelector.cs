using FreshCart.Reporting.Domain.Sales;

namespace FreshCart.Reporting.Application.Common.Models;

/// <summary>
/// Caller-facing period selector used at the API surface. Resolves to a concrete
/// <see cref="ReportingPeriod"/> via <see cref="ToPeriod(TimeProvider)"/> at handler time.
/// </summary>
public sealed record PeriodSelector(
    PeriodPreset? Preset = null,
    DateTimeOffset? FromUtc = null,
    DateTimeOffset? ToUtc = null)
{
    public ReportingPeriod ToPeriod(TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(timeProvider);

        if (FromUtc is not null && ToUtc is not null)
        {
            if (ToUtc <= FromUtc)
            {
                throw new InvalidOperationException("Period end must be strictly after the start.");
            }

            return new ReportingPeriod(FromUtc.Value, ToUtc.Value);
        }

        return (Preset ?? PeriodPreset.Last30Days) switch
        {
            PeriodPreset.Today        => ReportingPeriod.Today(timeProvider),
            PeriodPreset.Last30Days   => ReportingPeriod.Last30Days(timeProvider),
            PeriodPreset.MonthToDate  => ReportingPeriod.MonthToDate(timeProvider),
            PeriodPreset.YearToDate   => ReportingPeriod.YearToDate(timeProvider),
            _ => ReportingPeriod.Last30Days(timeProvider),
        };
    }
}
