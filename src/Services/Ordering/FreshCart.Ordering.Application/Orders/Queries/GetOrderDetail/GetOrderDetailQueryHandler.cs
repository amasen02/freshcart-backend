using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.Ordering.Application.Abstractions;
using FreshCart.Ordering.Application.Orders.Dtos;

namespace FreshCart.Ordering.Application.Orders.Queries.GetOrderDetail;

public sealed class GetOrderDetailQueryHandler(IOrderReadQueries orderReadQueries)
    : IQueryHandler<GetOrderDetailQuery, OrderDetailDto>
{
    public async Task<OrderDetailDto> Handle(GetOrderDetailQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        var orderDetail = await orderReadQueries
            .GetOrderDetailAsync(query.OrderId, cancellationToken)
            .ConfigureAwait(false)
            ?? throw new NotFoundException("Order", query.OrderId);

        if (orderDetail.CustomerId != query.RequestingCustomerId && !query.IsAdministrator)
        {
            throw new ForbiddenException("Only the owning customer or an administrator may view this order.");
        }

        return orderDetail;
    }
}
