using FreshCart.Reporting.Domain.Sales;

namespace FreshCart.Reporting.Application.Common.Abstractions;

public interface IDeliveryReadWarehouse
{
    Task<DeliveryPerformanceSummary> GetPerformanceSummaryAsync(
        ReportingPeriod period,
        CancellationToken cancellationToken);
}
