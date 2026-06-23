using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using FreshCart.Identity.Application.Common.Abstractions;
using FreshCart.Identity.Domain.Users;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FreshCart.Identity.Infrastructure.Tokens;

/// <summary>
/// Concrete <see cref="IAccessTokenIssuer"/>. Produces an HMAC-SHA-256-signed JWT carrying the
/// minimum claim set we expose to downstream services (subject, email, role).
/// </summary>
public sealed class JwtAccessTokenIssuer(IOptions<JwtIssuerOptions> issuerOptions) : IAccessTokenIssuer
{
    private readonly JwtIssuerOptions _options = issuerOptions.Value
        ?? throw new ArgumentException("JwtIssuerOptions missing from configuration.", nameof(issuerOptions));

    public AccessTokenIssueResult Issue(ApplicationUser user, IReadOnlyCollection<string> roleNames)
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(roleNames);

        var signingKeyBytes = Encoding.UTF8.GetBytes(_options.SigningKey);
        if (signingKeyBytes.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey must be at least 32 bytes (256 bits).");
        }

        var signingCredentials = new SigningCredentials(
            new SymmetricSecurityKey(signingKeyBytes),
            SecurityAlgorithms.HmacSha256);

        var nowUtc = DateTimeOffset.UtcNow;
        var expiresOnUtc = nowUtc.Add(_options.AccessTokenLifetime);

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new("display_name", user.DisplayName),
        };

        foreach (var roleName in roleNames)
        {
            claims.Add(new Claim(ClaimTypes.Role, roleName));
        }

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            notBefore: nowUtc.UtcDateTime,
            expires: expiresOnUtc.UtcDateTime,
            signingCredentials: signingCredentials);

        var serializedToken = new JwtSecurityTokenHandler().WriteToken(token);
        return new AccessTokenIssueResult(serializedToken, expiresOnUtc);
    }
}
