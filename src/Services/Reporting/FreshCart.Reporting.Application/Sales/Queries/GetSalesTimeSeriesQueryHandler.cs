using FreshCart.BuildingBlocks.CQRS;
using FreshCart.Reporting.Application.Common.Abstractions;

namespace FreshCart.Reporting.Application.Sales.Queries;

public sealed class GetSalesTimeSeriesQueryHandler(
    ISalesReadWarehouse salesWarehouse,
    TimeProvider timeProvider)
    : IQueryHandler<GetSalesTimeSeriesQuery, SalesTimeSeriesDto>
{
    public async Task<SalesTimeSeriesDto> Handle(GetSalesTimeSeriesQuery query, CancellationToken cancellationToken)
    {
        var period = query.Period.ToPeriod(timeProvider);

        var points = await salesWarehouse
            .GetTimeSeriesAsync(period, query.Bucket, cancellationToken)
            .ConfigureAwait(false);

        return new SalesTimeSeriesDto(period, query.Bucket, points);
    }
}
