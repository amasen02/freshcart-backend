using System.Security.Claims;
using FreshCart.BuildingBlocks.Exceptions;

namespace FreshCart.Basket.Api.Authentication;

/// <summary>
/// Claims access for the basket endpoints. The customer id always comes from the token, never from
/// the request body, which is what keeps every query scoped to its owner (BOLA prevention).
/// </summary>
public static class ClaimsPrincipalExtensions
{
    private const string SubjectClaimName = "sub";
    private const string EmailClaimName = "email";
    private const string DisplayNameClaimName = "display_name";

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

    public static string GetCustomerEmail(this ClaimsPrincipal claimsPrincipal)
    {
        ArgumentNullException.ThrowIfNull(claimsPrincipal);

        return claimsPrincipal.FindFirstValue(ClaimTypes.Email)
            ?? claimsPrincipal.FindFirstValue(EmailClaimName)
            ?? throw new ForbiddenException("The access token does not carry an email claim.");
    }

    public static string GetCustomerDisplayName(this ClaimsPrincipal claimsPrincipal)
    {
        ArgumentNullException.ThrowIfNull(claimsPrincipal);

        return claimsPrincipal.FindFirstValue(DisplayNameClaimName)
            ?? claimsPrincipal.FindFirstValue(ClaimTypes.Name)
            ?? throw new ForbiddenException("The access token does not carry a display name claim.");
    }
}
