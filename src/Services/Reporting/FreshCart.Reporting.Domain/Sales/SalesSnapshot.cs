namespace FreshCart.Reporting.Domain.Sales;

/// <summary>
/// Materialised sales aggregate row stored in the read warehouse. Built by the projection
/// consumers from the integration-event stream, never written to from a request handler.
/// </summary>
/// <remarks>
/// The schema is denormalised on purpose so dashboard queries return in a single index seek.
/// Daily granularity is the smallest bucket; hourly buckets are stored in a separate table when
/// real-time KPIs are needed (not in the initial scope).
/// </remarks>
public sealed record SalesSnapshot(
    DateOnly Day,
    int OrderCount,
    int UniqueCustomerCount,
    decimal GrossRevenue,
    decimal DiscountTotal,
    decimal RefundTotal,
    decimal TaxTotal,
    decimal ShippingTotal,
    decimal NetRevenue)
{
    /// <summary>Average order value for the snapshot bucket.</summary>
    public decimal AverageOrderValue => OrderCount == 0
        ? 0m
        : Math.Round(GrossRevenue / OrderCount, 2, MidpointRounding.AwayFromZero);

    /// <summary>Refund rate as a fraction of gross revenue (0–1).</summary>
    public decimal RefundRate => GrossRevenue == 0m
        ? 0m
        : Math.Round(RefundTotal / GrossRevenue, 4, MidpointRounding.AwayFromZero);
}
