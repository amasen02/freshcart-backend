using FreshCart.Identity.Domain.Users;

namespace FreshCart.Identity.Application.Common.Abstractions;

/// <summary>
/// Issues short-lived JWT access tokens for service-to-service traffic and for mobile clients that
/// cannot use cookies. Browser sessions rely on the HttpOnly cookie and never receive a JWT.
/// </summary>
public interface IAccessTokenIssuer
{
    AccessTokenIssueResult Issue(ApplicationUser user, IReadOnlyCollection<string> roleNames);
}
