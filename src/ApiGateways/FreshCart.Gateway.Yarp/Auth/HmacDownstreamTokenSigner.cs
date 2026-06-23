using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace FreshCart.Gateway.Yarp.Auth;

/// <summary>
/// Mints an HS256 bearer token from the shared <c>Jwt:SigningKey</c>. The token carries the same
/// issuer and audience the Identity service uses, so every downstream service validates it with the
/// configuration block it already shares.
/// </summary>
public sealed class HmacDownstreamTokenSigner : IDownstreamTokenSigner
{
    private readonly GatewayJwtOptions jwtOptions;
    private readonly SigningCredentials signingCredentials;
    private readonly JwtSecurityTokenHandler tokenHandler = new();

    public HmacDownstreamTokenSigner(IOptions<GatewayJwtOptions> jwtOptions)
    {
        ArgumentNullException.ThrowIfNull(jwtOptions);

        this.jwtOptions = jwtOptions.Value;
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(this.jwtOptions.SigningKey));
        signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
    }

    public string SignToken(DownstreamPrincipal principal, DateTimeOffset issuedAt)
    {
        ArgumentNullException.ThrowIfNull(principal);

        var claims = new List<Claim>(capacity: 3 + principal.Roles.Count)
        {
            new(DownstreamTokenClaim.Subject, principal.Subject),
        };

        if (!string.IsNullOrWhiteSpace(principal.Email))
        {
            claims.Add(new Claim(DownstreamTokenClaim.Email, principal.Email));
        }

        if (!string.IsNullOrWhiteSpace(principal.DisplayName))
        {
            claims.Add(new Claim(DownstreamTokenClaim.DisplayName, principal.DisplayName));
        }

        foreach (var roleName in principal.Roles)
        {
            claims.Add(new Claim(DownstreamTokenClaim.Role, roleName));
        }

        var expiresAt = issuedAt.Add(jwtOptions.DownstreamTokenLifetime);

        var securityToken = new JwtSecurityToken(
            issuer: jwtOptions.Issuer,
            audience: jwtOptions.Audience,
            claims: claims,
            notBefore: issuedAt.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: signingCredentials);

        return tokenHandler.WriteToken(securityToken);
    }
}
