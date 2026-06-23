using System.Security.Claims;
using FreshCart.BuildingBlocks.Exceptions;

namespace FreshCart.Delivery.Api.Configuration;

/// <summary>
/// Reads the authenticated subject from the JWT. The gateway exchanges the session cookie for a bearer
/// token whose <c>sub</c> claim maps to <see cref="ClaimTypes.NameIdentifier"/> under the default
/// ASP.NET mapping, so both names are checked.
/// </summary>
internal static class ClaimsPrincipalExtensions
{
    public const string AdministratorRole = "Administrator";

    public static Guid GetRequiredCustomerId(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? principal.FindFirstValue("sub")
            ?? throw new ForbiddenException("The request is not associated with an authenticated user.");

        if (!Guid.TryParse(subject, out var customerId))
        {
            throw new ForbiddenException("The authenticated subject is not a valid user identifier.");
        }

        return customerId;
    }

    public static bool IsAdministrator(this ClaimsPrincipal principal)
    {
        ArgumentNullException.ThrowIfNull(principal);
        return principal.IsInRole(AdministratorRole);
    }
}
