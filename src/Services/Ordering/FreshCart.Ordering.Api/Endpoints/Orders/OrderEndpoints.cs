using System.Security.Claims;
using Carter;
using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Ordering.Api.Authentication;
using FreshCart.Ordering.Application.Orders.Commands.CancelOrder;
using FreshCart.Ordering.Application.Orders.Commands.RefundOrder;
using FreshCart.Ordering.Application.Orders.Dtos;
using FreshCart.Ordering.Application.Orders.Queries.GetOrderDetail;
using FreshCart.Ordering.Application.Orders.Queries.GetOrders;
using MediatR;

namespace FreshCart.Ordering.Api.Endpoints.Orders;

/// <summary>
/// Carter module for the order endpoints. Each handler resolves the caller identity from the token,
/// delegates to a MediatR command or query and maps the outcome to an HTTP result. Ownership and
/// administrator checks live in the application handlers so the rules apply regardless of caller.
/// </summary>
public sealed class OrderEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var orders = app.MapGroup("/orders").WithTags("Orders");

        orders.MapGet("/", GetOrdersAsync)
            .RequireAuthorization(OrderingAuthorizationPolicies.Customer)
            .WithSummary("List the caller's orders; administrators may pass customerId to view any customer.")
            .Produces<PaginatedResult<OrderSummaryDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status403Forbidden);

        orders.MapGet("/{orderId:guid}", GetOrderDetailAsync)
            .RequireAuthorization(OrderingAuthorizationPolicies.Customer)
            .WithSummary("Get a single order; the owner or an administrator only.")
            .Produces<OrderDetailDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound);

        orders.MapPost("/{orderId:guid}/cancel", CancelOrderAsync)
            .RequireAuthorization(OrderingAuthorizationPolicies.Customer)
            .WithSummary("Cancel an order the caller owns before it is confirmed.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        orders.MapPost("/{orderId:guid}/refund", RefundOrderAsync)
            .RequireAuthorization(OrderingAuthorizationPolicies.Administrator)
            .WithSummary("Refund a confirmed order (administrators only).")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<IResult> GetOrdersAsync(
        ISender mediator,
        ClaimsPrincipal user,
        int? pageNumber,
        int? pageSize,
        Guid? customerId,
        CancellationToken cancellationToken)
    {
        var pagination = new PaginationRequest(pageNumber ?? 1, pageSize ?? 20);

        var query = new GetOrdersQuery(
            user.GetCustomerId(),
            user.IsAdministrator(),
            customerId,
            pagination);

        var page = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
        return Results.Ok(page);
    }

    private static async Task<IResult> GetOrderDetailAsync(
        Guid orderId,
        ISender mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        var query = new GetOrderDetailQuery(orderId, user.GetCustomerId(), user.IsAdministrator());
        var orderDetail = await mediator.Send(query, cancellationToken).ConfigureAwait(false);
        return Results.Ok(orderDetail);
    }

    private static async Task<IResult> CancelOrderAsync(
        Guid orderId,
        CancelOrderRequest request,
        ISender mediator,
        ClaimsPrincipal user,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new CancelOrderCommand(orderId, user.GetCustomerId(), request.Reason);
        await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return Results.NoContent();
    }

    private static async Task<IResult> RefundOrderAsync(
        Guid orderId,
        RefundOrderRequest request,
        ISender mediator,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var command = new RefundOrderCommand(orderId, request.Reason);
        await mediator.Send(command, cancellationToken).ConfigureAwait(false);
        return Results.NoContent();
    }
}
