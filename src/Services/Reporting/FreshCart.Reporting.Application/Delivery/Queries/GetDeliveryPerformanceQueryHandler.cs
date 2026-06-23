using FreshCart.BuildingBlocks.CQRS;
using FreshCart.Reporting.Application.Common.Abstractions;

namespace FreshCart.Reporting.Application.Delivery.Queries;

public sealed class GetDeliveryPerformanceQueryHandler(
    IDeliveryReadWarehouse deliveryWarehouse,
    TimeProvider timeProvider)
    : IQueryHandler<GetDeliveryPerformanceQuery, DeliveryPerformanceSummary>
{
    public Task<DeliveryPerformanceSummary> Handle(
        GetDeliveryPerformanceQuery query,
        CancellationToken cancellationToken)
    {
        var period = query.Period.ToPeriod(timeProvider);
        return deliveryWarehouse.GetPerformanceSummaryAsync(period, cancellationToken);
    }
}
