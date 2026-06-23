using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace FreshCart.Gateway.Tests.Support;

/// <summary>
/// Builds a cookie-authenticated principal shaped exactly like the one the Identity service writes
/// onto the <c>FreshCart.Session</c> ticket, so the exchanger tests exercise the real claim layout.
/// </summary>
public static class CookiePrincipalFactory
{
    public static ClaimsPrincipal Create(
        Guid userId,
        string email,
        string displayName,
        string authenticationInstant,
        params string[] roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new("sub", userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, displayName),
            new("auth_time", authenticationInstant),
        };

        foreach (var roleName in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, roleName));
        }

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        return new ClaimsPrincipal(identity);
    }
}
