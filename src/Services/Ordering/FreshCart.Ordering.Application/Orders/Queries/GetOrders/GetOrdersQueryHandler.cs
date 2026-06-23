using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Ordering.Application.Abstractions;
using FreshCart.Ordering.Application.Orders.Dtos;

namespace FreshCart.Ordering.Application.Orders.Queries.GetOrders;

public sealed class GetOrdersQueryHandler(IOrderReadQueries orderReadQueries)
    : IQueryHandler<GetOrdersQuery, PaginatedResult<OrderSummaryDto>>
{
    public Task<PaginatedResult<OrderSummaryDto>> Handle(GetOrdersQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var effectiveCustomerId = query.CustomerIdFilter ?? query.RequestingCustomerId;

        if (effectiveCustomerId != query.RequestingCustomerId && !query.IsAdministrator)
        {
            throw new ForbiddenException("Only administrators may view another customer's orders.");
        }

        return orderReadQueries.GetOrderSummariesPageAsync(
            effectiveCustomerId,
            query.Pagination.Normalise(),
            cancellationToken);
    }
}
