using FreshCart.Reporting.Domain.Kpis;
using FreshCart.Reporting.Domain.Sales;

namespace FreshCart.Reporting.Application.Common.Abstractions;

public interface IProductReadWarehouse
{
    Task<IReadOnlyList<TopEntityRanking>> GetTopSellingProductsAsync(
        ReportingPeriod period,
        int take,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TopEntityRanking>> GetSlowMovingProductsAsync(
        ReportingPeriod period,
        int take,
        CancellationToken cancellationToken);

    Task<InventoryHealthSummary> GetInventoryHealthAsync(CancellationToken cancellationToken);
}
