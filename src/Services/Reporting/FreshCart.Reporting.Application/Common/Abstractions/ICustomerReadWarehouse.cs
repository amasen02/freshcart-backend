using FreshCart.Reporting.Domain.Kpis;
using FreshCart.Reporting.Domain.Sales;

namespace FreshCart.Reporting.Application.Common.Abstractions;

public interface ICustomerReadWarehouse
{
    Task<IReadOnlyList<TopEntityRanking>> GetTopCustomersByLifetimeValueAsync(
        int take,
        CancellationToken cancellationToken);

    Task<CustomerAcquisitionSummary> GetAcquisitionSummaryAsync(
        ReportingPeriod period,
        CancellationToken cancellationToken);
}
