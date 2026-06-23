using Carter;
using FreshCart.BuildingBlocks.Exceptions;
using FreshCart.BuildingBlocks.Pagination;
using FreshCart.Notification.Api.Hubs;
using FreshCart.Notification.Api.Notifications;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace FreshCart.Notification.Api.Endpoints;

/// <summary>
/// REST surface for a customer's own notification history. The user id is always taken from the
/// bearer token, never from the route or body, so a caller can only ever read or mutate their own
/// notifications.
/// </summary>
public sealed class NotificationEndpoints : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);

        var notificationsGroup = app
            .MapGroup("/notifications")
            .WithTags("Notifications")
            .RequireAuthorization();

        notificationsGroup.MapGet("/", GetOwnNotificationsAsync)
            .WithSummary("The authenticated user's notifications, newest first, paginated.")
            .Produces<PaginatedResult<NotificationDto>>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        notificationsGroup.MapGet("/unread-count", GetUnreadCountAsync)
            .WithSummary("Count of the authenticated user's unread notifications.")
            .Produces<UnreadCountResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

        notificationsGroup.MapPut("/{notificationId:guid}/read", MarkAsReadAsync)
            .WithSummary("Mark one of the authenticated user's notifications as read.")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetOwnNotificationsAsync(
        [AsParameters] PaginationRequest paginationRequest,
        HttpContext httpContext,
        INotificationStore notificationStore,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();

        var page = await notificationStore
            .GetForUserAsync(userId, paginationRequest, cancellationToken)
            .ConfigureAwait(false);

        var response = new PaginatedResult<NotificationDto>(
            page.PageNumber,
            page.PageSize,
            page.TotalItemCount,
            page.Items.Select(NotificationDto.FromDocument).ToList());

        return Results.Ok(response);
    }

    private static async Task<IResult> GetUnreadCountAsync(
        HttpContext httpContext,
        INotificationStore notificationStore,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();

        var unreadCount = await notificationStore
            .CountUnreadAsync(userId, cancellationToken)
            .ConfigureAwait(false);

        return Results.Ok(new UnreadCountResponse(unreadCount));
    }

    private static async Task<IResult> MarkAsReadAsync(
        Guid notificationId,
        HttpContext httpContext,
        INotificationStore notificationStore,
        CancellationToken cancellationToken)
    {
        var userId = httpContext.User.GetUserId();

        var marked = await notificationStore
            .MarkAsReadAsync(userId, notificationId, cancellationToken)
            .ConfigureAwait(false);

        if (!marked)
        {
            throw new NotFoundException(nameof(NotificationDocument), notificationId);
        }

        return Results.NoContent();
    }
}
