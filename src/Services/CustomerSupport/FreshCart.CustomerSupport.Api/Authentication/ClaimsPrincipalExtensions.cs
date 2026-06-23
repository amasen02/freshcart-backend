using System.Security.Claims;
using FreshCart.BuildingBlocks.Exceptions;

namespace FreshCart.CustomerSupport.Api.Authentication;

/// <summary>
/// Reads the caller's identity from the validated token. The user id always comes from the token,
/// never from a hub argument or request body, which is what keeps the participant check honest
/// (BOLA prevention in WebSocket form).
/// </summary>
public static class ClaimsPrincipalExtensions
{
    private const string SubjectClaimName = "sub";
    private const string DisplayNameClaimName = "display_name";

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

    public static string GetDisplayName(this ClaimsPrincipal claimsPrincipal)
    {
        ArgumentNullException.ThrowIfNull(claimsPrincipal);

        return claimsPrincipal.FindFirstValue(DisplayNameClaimName)
            ?? claimsPrincipal.FindFirstValue(ClaimTypes.Name)
            ?? throw new ForbiddenException("The access token does not carry a display name claim.");
    }

    public static bool IsCustomer(this ClaimsPrincipal claimsPrincipal)
    {
        ArgumentNullException.ThrowIfNull(claimsPrincipal);

        return claimsPrincipal.IsInRole(AuthorizationPolicies.CustomerRole);
    }

    public static bool IsSupportAgent(this ClaimsPrincipal claimsPrincipal)
    {
        ArgumentNullException.ThrowIfNull(claimsPrincipal);

        return claimsPrincipal.IsInRole(AuthorizationPolicies.SupportAgentRole);
    }

    public static bool IsAdministrator(this ClaimsPrincipal claimsPrincipal)
    {
        ArgumentNullException.ThrowIfNull(claimsPrincipal);

        return claimsPrincipal.IsInRole(AuthorizationPolicies.AdministratorRole);
    }
}
