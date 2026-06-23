using FreshCart.BuildingBlocks.CQRS;
using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Ordering.Application.Orders.Dtos;

namespace FreshCart.Ordering.Application.Orders.Queries.GetOrders;

public sealed record GetOrdersQuery(
    Guid RequestingCustomerId,
    bool IsAdministrator,
    Guid? CustomerIdFilter,
    PaginationRequest Pagination) : IQuery<PaginatedResult<OrderSummaryDto>>;
