using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Ordering.Application.Orders.Dtos;

namespace FreshCart.Ordering.Application.Abstractions;

/// <summary>
/// Read-side port served by Dapper projections. Queries bypass the aggregate entirely; they are
/// shaped for the HTTP responses and never participate in change tracking.
/// </summary>
public interface IOrderReadQueries
{
    Task<PaginatedResult<OrderSummaryDto>> GetOrderSummariesPageAsync(
        Guid customerId,
        PaginationRequest paginationRequest,
        CancellationToken cancellationToken);

    Task<OrderDetailDto?> GetOrderDetailAsync(Guid orderId, CancellationToken cancellationToken);
}
