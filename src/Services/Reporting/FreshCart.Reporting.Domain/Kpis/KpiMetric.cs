namespace FreshCart.Reporting.Domain.Kpis;

/// <summary>
/// A single KPI tile value as rendered on the executive dashboard. The shape stays stable across
/// every metric so the frontend can render every tile from a uniform contract.
/// </summary>
public sealed record KpiMetric(
    string Code,
    string DisplayName,
    decimal CurrentValue,
    decimal? PreviousValue,
    KpiUnit Unit,
    string? Description = null)
{
    /// <summary>Percentage delta vs. the previous period. <c>null</c> when no comparison is available.</summary>
    public decimal? DeltaPercentage => PreviousValue is null or 0m
        ? null
        : Math.Round((CurrentValue - PreviousValue.Value) / Math.Abs(PreviousValue.Value) * 100m, 2);

    public KpiTrend Trend
    {
        get
        {
            if (PreviousValue is null)
            {
                return KpiTrend.Flat;
            }

            if (CurrentValue > PreviousValue.Value)
            {
                return KpiTrend.Up;
            }

            return CurrentValue < PreviousValue.Value ? KpiTrend.Down : KpiTrend.Flat;
        }
    }
}
