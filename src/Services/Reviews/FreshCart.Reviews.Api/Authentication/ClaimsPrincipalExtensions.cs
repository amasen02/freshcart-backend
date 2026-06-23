using System.Security.Claims;
using FreshCart.BuildingBlocks.Exceptions;

namespace FreshCart.Reviews.Api.Authentication;

/// <summary>
/// Claims access for the review endpoints. The author id and display name always come from the token,
/// never from the request body, which is what keeps a customer from posting a review under someone
/// else's name and keeps the my-reviews query scoped to its owner (BOLA prevention).
/// </summary>
public static class ClaimsPrincipalExtensions
{
    private const string SubjectClaimName = "sub";
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

    public static string GetCustomerDisplayName(this ClaimsPrincipal claimsPrincipal)
    {
        ArgumentNullException.ThrowIfNull(claimsPrincipal);

        return claimsPrincipal.FindFirstValue(DisplayNameClaimName)
            ?? claimsPrincipal.FindFirstValue(ClaimTypes.Name)
            ?? throw new ForbiddenException("The access token does not carry a display name claim.");
    }
}
