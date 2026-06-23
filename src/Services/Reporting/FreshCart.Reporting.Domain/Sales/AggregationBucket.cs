namespace FreshCart.Reporting.Domain.Sales;

/// <summary>
/// Aggregation bucket size used by time-series KPI queries.
/// </summary>
public enum AggregationBucket
{
    Hourly,
    Daily,
    Weekly,
    Monthly,
}
