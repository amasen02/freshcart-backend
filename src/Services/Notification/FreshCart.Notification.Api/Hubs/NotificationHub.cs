using FreshCart.Notification.Api.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FreshCart.Notification.Api.Hubs;

/// <summary>
/// Real-time delivery hub at <c>/hubs/notifications</c>. On connect a client joins its own
/// <c>user:{userId}</c> group; back-office roles additionally join <c>backoffice</c> for the live
/// sales tick. The hub carries no business logic: marking a notification read delegates straight to
/// the store, scoped to the connected user.
/// </summary>
[Authorize]
public sealed class NotificationHub(INotificationStore notificationStore) : Hub
{
    public override async Task OnConnectedAsync()
    {
        var user = Context.User ?? throw new HubException("Connection is not authenticated.");
        var userId = user.GetUserId();

        await Groups
            .AddToGroupAsync(Context.ConnectionId, NotificationGroups.ForUser(userId))
            .ConfigureAwait(false);

        foreach (var groupName in NotificationGroups.ForRoles(user.GetRoles()))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName).ConfigureAwait(false);
        }

        await base.OnConnectedAsync().ConfigureAwait(false);
    }

    public Task MarkAsRead(Guid notificationId)
    {
        var userId = (Context.User ?? throw new HubException("Connection is not authenticated.")).GetUserId();

        return notificationStore.MarkAsReadAsync(userId, notificationId, Context.ConnectionAborted);
    }
}
