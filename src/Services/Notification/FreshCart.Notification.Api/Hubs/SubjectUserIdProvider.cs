using System.Security.Claims;
using Microsoft.AspNetCore.SignalR;

namespace FreshCart.Notification.Api.Hubs;

/// <summary>
/// Maps a SignalR connection to the authenticated subject. SignalR's default provider returns the
/// name claim; the platform keys every per-user group on the JWT <c>sub</c> (mapped to
/// <see cref="ClaimTypes.NameIdentifier"/>), so the group a connection joins matches the id the REST
/// endpoints and consumers use.
/// </summary>
public sealed class SubjectUserIdProvider : IUserIdProvider
{
    private const string SubjectClaimName = "sub";

    public string? GetUserId(HubConnectionContext connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        return connection.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? connection.User?.FindFirstValue(SubjectClaimName);
    }
}
