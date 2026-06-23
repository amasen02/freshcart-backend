using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace FreshCart.CustomerSupport.Api.Authentication;

/// <summary>
/// Maps a hub connection to the stable user id so the hub can address a person by their identity
/// rather than the transient connection id. Returning the same subject claim that the REST and
/// gateway layers use keeps the <c>user:{userId}</c> group naming consistent with Notification.
/// </summary>
public sealed class SupportUserIdProvider : IUserIdProvider
{
    private const string SubjectClaimName = "sub";

    public string? GetUserId(HubConnectionContext connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        return connection.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? connection.User?.FindFirstValue(SubjectClaimName);
    }
}
