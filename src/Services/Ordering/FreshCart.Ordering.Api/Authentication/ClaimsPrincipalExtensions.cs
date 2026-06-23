using System.Security.Claims;
using FreshCart.BuildingBlocks.Exceptions;

namespace FreshCart.Ordering.Api.Authentication;

/// <summary>
/// Claims access for the order endpoints. The customer id always comes from the token, never from
/// the route or body, so every query stays scoped to its owner (BOLA prevention). Administrators are
/// recognised by role so the handlers can grant the documented cross-customer access.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    private const string SubjectClaimName = "sub";

    public static Guid GetCustomerId(this ClaimsPrincipal claimsPrincipal)
    {
        ArgumentNullException.ThrowIfNull(claimsPrincipal);

        var subject = claimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? claimsPrincipal.FindFirstValue(SubjectClaimName);

        if (!Guid.TryParse(subject, out var customerId))
        {
            throw new ForbiddenException("Authenticated subject is not a valid customer identifier.");
        }

        return customerId;
    }

    public static bool IsAdministrator(this ClaimsPrincipal claimsPrincipal)
    {
        ArgumentNullException.ThrowIfNull(claimsPrincipal);

        return claimsPrincipal.IsInRole(OrderingAuthorizationPolicies.AdministratorRoleName);
    }
}
