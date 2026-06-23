using FreshCart.BuildingBlocks.CQRS;
using FreshCart.Reporting.Application.Common.Abstractions;

namespace FreshCart.Reporting.Application.Products.Queries;

public sealed class GetTopProductsQueryHandler(
    IProductReadWarehouse productWarehouse,
    TimeProvider timeProvider)
    : IQueryHandler<GetTopProductsQuery, TopProductsDto>
{
    public async Task<TopProductsDto> Handle(GetTopProductsQuery query, CancellationToken cancellationToken)
    {
        var period = query.Period.ToPeriod(timeProvider);

        var rows = query.Mode == TopProductsMode.BestSellers
            ? await productWarehouse.GetTopSellingProductsAsync(period, query.Take, cancellationToken).ConfigureAwait(false)
            : await productWarehouse.GetSlowMovingProductsAsync(period, query.Take, cancellationToken).ConfigureAwait(false);

        return new TopProductsDto(period, query.Mode, rows);
    }
}
