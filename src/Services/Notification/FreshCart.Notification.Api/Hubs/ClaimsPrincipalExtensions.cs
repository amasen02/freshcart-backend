using System.Security.Claims;
using FreshCart.BuildingBlocks.Exceptions;

namespace FreshCart.Notification.Api.Hubs;

/// <summary>
/// Reads the recipient identity from the bearer token. The user id always comes from the token,
/// never from the route or body, so every history query and read receipt stays scoped to its owner
/// (BOLA prevention).
/// </summary>
public static class ClaimsPrincipalExtensions
{
    private const string SubjectClaimName = "sub";

    public static Guid GetUserId(this ClaimsPrincipal claimsPrincipal)
    {
        ArgumentNullException.ThrowIfNull(claimsPrincipal);

        var subject = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? claimsPrincipal.FindFirstValue(SubjectClaimName);

        if (!Guid.TryParse(subject, out var userId))
        {
            throw new ForbiddenException("Authenticated subject is not a valid user identifier.");
        }

        return userId;
    }

    public static IReadOnlyList<string> GetRoles(this ClaimsPrincipal claimsPrincipal)
    {
        ArgumentNullException.ThrowIfNull(claimsPrincipal);

        return claimsPrincipal
            .FindAll(ClaimTypes.Role)
            .Select(roleClaim => roleClaim.Value)
            .ToArray();
    }
}
