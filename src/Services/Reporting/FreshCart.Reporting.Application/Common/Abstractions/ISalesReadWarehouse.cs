using FreshCart.Reporting.Domain.Sales;

namespace FreshCart.Reporting.Application.Common.Abstractions;

/// <summary>
/// Read-only access to the materialised sales tables built by the projection consumers.
/// Implementations use Dapper against MySQL for hot dashboard reads.
/// </summary>
public interface ISalesReadWarehouse
{
    Task<SalesSnapshot> GetAggregateAsync(ReportingPeriod period, CancellationToken cancellationToken);

    Task<IReadOnlyList<SalesSnapshot>> GetTimeSeriesAsync(
        ReportingPeriod period,
        AggregationBucket bucket,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RevenueByCategoryRow>> GetRevenueByCategoryAsync(
        ReportingPeriod period,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<RevenueByPaymentMethodRow>> GetRevenueByPaymentMethodAsync(
        ReportingPeriod period,
        CancellationToken cancellationToken);
}
