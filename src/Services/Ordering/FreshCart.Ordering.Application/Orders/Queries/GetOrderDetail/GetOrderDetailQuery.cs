using FreshCart.BuildingBlocks.CQRS;
using FreshCart.Ordering.Application.Orders.Dtos;

namespace FreshCart.Ordering.Application.Orders.Queries.GetOrderDetail;

public sealed record GetOrderDetailQuery(
    Guid OrderId,
    Guid RequestingCustomerId,
    bool IsAdministrator) : IQuery<OrderDetailDto>;
