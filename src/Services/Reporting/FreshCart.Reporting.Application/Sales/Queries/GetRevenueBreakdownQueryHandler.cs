using FreshCart.BuildingBlocks.CQRS;
using FreshCart.Reporting.Application.Common.Abstractions;

namespace FreshCart.Reporting.Application.Sales.Queries;

public sealed class GetRevenueBreakdownQueryHandler(
    ISalesReadWarehouse salesWarehouse,
    TimeProvider timeProvider)
    : IQueryHandler<GetRevenueBreakdownQuery, RevenueBreakdownDto>
{
    public async Task<RevenueBreakdownDto> Handle(GetRevenueBreakdownQuery query, CancellationToken cancellationToken)
    {
        var period = query.Period.ToPeriod(timeProvider);

        var byCategoryTask      = salesWarehouse.GetRevenueByCategoryAsync(period, cancellationToken);
        var byPaymentMethodTask = salesWarehouse.GetRevenueByPaymentMethodAsync(period, cancellationToken);

        await Task.WhenAll(byCategoryTask, byPaymentMethodTask).ConfigureAwait(false);

        return new RevenueBreakdownDto(period, byCategoryTask.Result, byPaymentMethodTask.Result);
    }
}
